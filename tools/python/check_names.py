"""Prüft die Synonyme, bevor generate.py sie in die Affixe schreibt.

    python check_names.py

Ein Synonym soll kein Name aus Path of Exile sein, nicht zweimal vergeben werden und keinem alten Affix des Spiels
gehören. Ob jeder Name genau eine Familie trägt, prüft danach der Test PoeAffixDataTests.
"""

import re

import poedb
from mapping import CLASSES, FAMILY_NAMES


def main():
    names = poedb.synonyms()
    ours = list(names.values()) + list(FAMILY_NAMES.values())

    poe_names = {tier['name'] for page, *_ in CLASSES for family in poedb.read_families(page) for tier in family['tiers']}

    # Alte Affixe sind alle, die nicht in einem der Ordner aus mapping.CLASSES liegen
    ported = [(poedb.AFFIXES / kind / folder).resolve() for _, folder, *_ in CLASSES for kind in ('Prefixes', 'Suffixes')]
    old = {}

    for path in poedb.AFFIXES.rglob('*.tres'):
        if any(path.resolve().is_relative_to(folder) for folder in ported):
            continue

        for name in re.findall(r'ItemnameAddition = "([^"]+)"', path.read_text(encoding='utf-8')):
            old[name.strip()] = path.relative_to(poedb.AFFIXES).as_posix()

    problems = 0

    for title, found in (('Synonyme, die selbst ein PoE-Name sind', sorted(set(ours) & poe_names)),
                         ('Synonyme, die ein altes Affix schon trägt', sorted(f'{name} ({old[name]})' for name in set(ours) & set(old))),
                         ('Doppelt vergebene Synonyme', sorted({name for name in ours if ours.count(name) > 1}))):
        print(f'{title}: {", ".join(found) if found else "keine"}')
        problems += len(found)

    raise SystemExit(1 if problems else 0)


if __name__ == '__main__':
    main()
