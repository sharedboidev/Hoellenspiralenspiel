"""Baut aus data/<Seite>.json, mapping.py und synonyms.json die Affixe unter Resources/Affixes.

    python generate.py                  schreibt in Resources/Affixes, bestehende Dateien werden überschrieben
    python generate.py <Ordner>         schreibt in einen anderen Ordner, etwa zum Vergleichen mit diff -r

Jede Familie aus mapping.CLASSES wird eine Datei unter Prefixes/<Ordner> oder Suffixes/<Ordner>. Die Stufen stehen von der
niedrigsten zur höchsten, die höchste ist Tier 1. Die Werte kommen 1:1 aus dem Original, Prozente als ganze Zahlen wie 40 für 40 %.
"""

import sys
from pathlib import Path

import common
from mapping import CLASSES, FAMILY_NAMES

TIER_SCRIPT = ('uid://sdx5y8g2wvor', 'res://Resources/Affixes/AffixTier.cs')
SCRIPTS = {
    'Prefix': ('uid://d2himjbu3lvto', 'res://Resources/Affixes/Prefixes/Prefix.cs', 'Prefixes'),
    'Suffix': ('uid://bwwyi50jc5koy', 'res://Resources/Affixes/Suffixes/Suffix.cs', 'Suffixes'),
}

STATS = common.combat_stats()
NAMES = common.synonyms()


def number(value):
    return f'{float(value)}'


def render(kind, stat, modification, is_local, allows_fractions, slot, weapon_type, tiers, hybrid=None):
    script_uid, script_path, _ = SCRIPTS[kind]
    lines = [
        f'[gd_resource type="Resource" script_class="{kind}" format=3]',
        '',
        f'[ext_resource type="Script" uid="{TIER_SCRIPT[0]}" path="{TIER_SCRIPT[1]}" id="1_tier"]',
        f'[ext_resource type="Script" uid="{script_uid}" path="{script_path}" id="2_{kind.lower()}"]',
        '',
    ]

    for tier in tiers:
        lines += [
            f'[sub_resource type="Resource" id="Resource_tier{tier["number"]}"]',
            'script = ExtResource("1_tier")',
            f'Tier = {tier["number"]}',
            f'MinItemLevelToAppearOn = {tier["level"]}',
            f'Weight = {tier["weight"]}',
            f'MinValue = {number(tier["min"])}',
            f'MaxValue = {number(tier["max"])}',
            f'ItemnameAddition = "{tier["name"]}"',
        ]

        if 'min_to' in tier:
            lines += [f'MinValueTo = {number(tier["min_to"])}', f'MaxValueTo = {number(tier["max_to"])}']

        if 'hybrid_min' in tier:
            lines += [f'HybridMinValue = {number(tier["hybrid_min"])}', f'HybridMaxValue = {number(tier["hybrid_max"])}']

        lines += [f'metadata/_custom_type_script = "{TIER_SCRIPT[0]}"', '']

    refs = ', '.join(f'SubResource("Resource_tier{tier["number"]}")' for tier in tiers)
    lines += ['[resource]', f'script = ExtResource("2_{kind.lower()}")']

    # Godot schreibt keine Werte, die dem Standard entsprechen: Life ist 0, Flat ist 0
    if STATS.index(stat):
        lines.append(f'AffectedCombatStat = {STATS.index(stat)}')

    if modification:
        lines.append(f'ModificationType = {modification}')

    lines.append(f'AffectableItemTypes = Array[int]([{slot}])')

    if weapon_type is not None:
        lines.append(f'AffectableWeaponTypes = Array[int]([{weapon_type}])')

    if allows_fractions:
        lines.append('AllowFractions = true')

    if is_local:
        lines.append('IsInherentMod = true')

    if hybrid is not None:
        hybrid_stat, hybrid_modification, hybrid_local = hybrid
        lines.append('IsHybrid = true')

        if STATS.index(hybrid_stat):
            lines.append(f'HybridCombatStat = {STATS.index(hybrid_stat)}')

        if hybrid_modification:
            lines.append(f'HybridModificationType = {hybrid_modification}')

        if hybrid_local:
            lines.append('HybridIsInherentMod = true')

    lines += [f'Tiers = Array[ExtResource("1_tier")]([{refs}])', f'metadata/_custom_type_script = "{script_uid}"', '']

    return '\n'.join(lines)


# Zwei Werte je Stufe sind bei "Adds X to Y" das X und das Y, bei einem hybriden Affix die Werte seiner beiden Stats
def tiers_of(family, file_name, negative, is_hybrid):
    tiers = []
    count = len(family['tiers'])

    for index, tier in enumerate(family['tiers']):
        values = tier['values']
        low, high = values[0]

        if negative:
            low, high = -high, -low

        entry = {'number': count - index, 'level': tier['level'], 'weight': tier['weight'], 'min': low, 'max': high,
                 'name': FAMILY_NAMES.get((file_name, tier['name']), NAMES[tier['name']])}

        if len(values) == 2 and is_hybrid:
            entry['hybrid_min'], entry['hybrid_max'] = values[1]
        elif len(values) == 2:
            entry['min_to'], entry['max_to'] = values[1]
        elif len(values) != 1:
            raise SystemExit(f'Unerwartete Werte in {family["template"]}: {values}')

        tiers.append(entry)

    return tiers


def main(arguments):
    target_root = Path(arguments[0]) if arguments else common.AFFIXES
    written = []

    for page, folder, slot, weapon_type, table in CLASSES:
        for family in common.read_families(page):
            if family['template'] not in table:
                continue

            file_name, stat, modification, is_local, allows_fractions, negative, *rest = table[family['template']]
            hybrid = rest[0] if rest else None
            kind = family['type']
            target = target_root / SCRIPTS[kind][2] / folder
            path = target / f'{file_name}.tres'

            if path in written:
                raise SystemExit(f'Zwei Familien wollen in {path}')

            target.mkdir(parents=True, exist_ok=True)

            with open(path, 'w', encoding='utf-8', newline='\n') as handle:
                handle.write(render(kind, stat, modification, is_local, allows_fractions, slot, weapon_type,
                                    tiers_of(family, file_name, negative, hybrid is not None), hybrid))

            written.append(path)

    print(f'{len(written)} Dateien unter {target_root}')


if __name__ == '__main__':
    main(sys.argv[1:])
