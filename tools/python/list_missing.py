"""Zeigt, was generate.py noch fehlt: Namen aus dem Original ohne Synonym und Familien ohne Eintrag in mapping.py.

    python list_missing.py

Ein fehlendes Synonym bricht generate.py ab. Eine Familie ohne Eintrag lässt es bewusst aus, etwa Socketed oder Chaos.
"""

import common
from mapping import CLASSES, FAMILY_NAMES


def main():
    names = common.synonyms()
    missing = {}
    unmapped = {}

    for page, folder, slot, weapon_type, table in CLASSES:
        for family in common.read_families(page):
            if family['template'] not in table:
                unmapped.setdefault(page, []).append(f"{family['type'][:3]} {family['template']} ({len(family['tiers'])})")

                continue

            file_name = table[family['template']][0]

            for tier in family['tiers']:
                if tier['name'] not in names and (file_name, tier['name']) not in FAMILY_NAMES:
                    missing.setdefault(file_name, []).append(tier['name'])

    for file_name, original_names in missing.items():
        print(file_name, '->', ', '.join(dict.fromkeys(original_names)))

    print('Ohne Synonym:', len({name for original_names in missing.values() for name in original_names}))
    print()

    for page, rows in unmapped.items():
        print(page, '| nicht übernommen:', '; '.join(rows))


if __name__ == '__main__':
    main()
