"""Gemeinsames für die Affixe aus Path of Exile: Pfade, das Lesen der Seiten von poedb.tw und die Stats des Spiels.

Eine Seite wie https://poedb.tw/us/Bows trägt alle Mods ihrer Itemklasse als JSON im Aufruf ``new ModsView({...})``.
Unter ``normal`` steht je Stufe ein Eintrag mit Name, Itemlevel, Prefix oder Suffix, Familie, Gewicht und Text.
"""

import html
import json
import re
from collections import OrderedDict
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
CACHE = HERE / 'cache'
DATA = HERE / 'data'
AFFIXES = REPO / 'Resources' / 'Affixes'
COMBAT_STAT = REPO / 'Enums' / 'CombatStat.cs'
SYNONYMS = HERE / 'synonyms.json'
URL = 'https://poedb.tw/us/{page}'

NUMBER = r'[-+]?\d+(?:\.\d+)?'
VALUE = re.compile(r'\((' + NUMBER + r')\s*[—–-]\s*(' + NUMBER + r')\)|(' + NUMBER + r')')


def load_mods_view(page_html):
    """Das JSON-Argument von ``new ModsView(...)`` aus dem HTML einer Seite."""
    start = page_html.find('new ModsView(')

    if start < 0:
        raise SystemExit('Die Seite enthält kein "new ModsView(".')

    view, _ = json.JSONDecoder().raw_decode(page_html, start + len('new ModsView('))

    return view


def lines_of(raw):
    text = raw.replace('<br>', '\n').replace('<br/>', '\n')
    text = re.sub(r'<[^>]+>', '', text)
    text = html.unescape(text)

    return [line.strip() for line in text.split('\n') if line.strip()]


def parse_line(line):
    """Ersetzt jede Zahl und jede Spanne "(a—b)" durch #, so wird aus "+(16—20) to Accuracy Rating" die Vorlage "+# to Accuracy Rating"."""
    values = []

    def replace(match):
        if match.group(1) is not None:
            values.append((float(match.group(1)), float(match.group(2))))
        else:
            values.append((float(match.group(3)), float(match.group(3))))

        return '#'

    return VALUE.sub(replace, line), values


def families(page_html):
    """Fasst die Basis-Stufen einer Seite nach Prefix oder Suffix, Familie und Vorlage zusammen, je Familie nach Itemlevel sortiert."""
    groups = OrderedDict()

    for mod in load_mods_view(page_html)['normal']:
        lines = lines_of(mod['str'])
        parsed = [parse_line(line) for line in lines]
        template = ' / '.join(template for template, _ in parsed)
        key = (mod['ModGenerationTypeID'], tuple(mod['ModFamilyList']), template)

        groups.setdefault(key, []).append({
            'name': mod['Name'],
            'level': int(mod['Level']),
            'weight': int(mod['DropChance']),
            'values': [value for _, values in parsed for value in values],
            'lines': lines,
            'tags': [re.sub(r'<[^>]+>', '', tag) for tag in mod.get('mod_no', [])]})

    result = []

    for (generation, family, template), tiers in groups.items():
        tiers.sort(key=lambda tier: (tier['level'], tier['values']))
        result.append({'type': 'Prefix' if generation == '1' else 'Suffix', 'family': list(family), 'template': template, 'tiers': tiers})

    return result


def read_families(page):
    """Die zusammengefassten Familien einer Seite aus data/, so wie fetch.py sie zuletzt geschrieben hat."""
    return json.loads((DATA / f'{page}.json').read_text(encoding='utf-8'))


def combat_stats():
    """Die Namen von CombatStat in der Reihenfolge des Enums. Die .tres speichern einen Stat als seine Zahl."""
    text = COMBAT_STAT.read_text(encoding='utf-8')
    body = text[text.index('{') + 1:text.rindex('}')]
    names = []

    for line in body.splitlines():
        line = line.split('//')[0].strip()

        if line and not line.startswith('['):
            names.append(line.rstrip(','))

    return names


def synonyms():
    """PoE-Name einer Stufe → unser Name. Dieselbe Stufe heißt in jeder Itemklasse gleich."""
    return json.loads(SYNONYMS.read_text(encoding='utf-8'))
