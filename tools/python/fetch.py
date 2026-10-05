"""Lädt die Seiten der Itemklassen von poedb.tw und schreibt ihre Affix-Familien nach data/<Seite>.json.

    python fetch.py                     alle Seiten aus mapping.CLASSES
    python fetch.py Bows Staves         nur diese
    python fetch.py --cached            ohne Download, aus cache/

Die HTML-Seiten landen in cache/ (nicht im Repository, je Seite 1 bis 3 MB). data/ hält den Stand fest,
aus dem generate.py die Affixe baut. Ändert poedb seine Werte, zeigt git diff hier zuerst, was sich ändert.
"""

import json
import sys
import time
import urllib.request

import poedb
from mapping import CLASSES


def download(page):
    request = urllib.request.Request(poedb.URL.format(page=page), headers={'User-Agent': 'Mozilla/5.0'})

    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read().decode('utf-8')


def summary(family):
    best = family['tiers'][-1]

    return (f"{family['type'][:3]} | {family['template']} | {len(family['tiers'])} Stufen | ab {family['tiers'][0]['level']}"
            f" | T1 {best['values']} ab {best['level']} | Gewicht {sum(tier['weight'] for tier in family['tiers'])}")


def main(arguments):
    cached = '--cached' in arguments
    pages = [argument for argument in arguments if not argument.startswith('--')] or [entry[0] for entry in CLASSES]

    poedb.CACHE.mkdir(exist_ok=True)
    poedb.DATA.mkdir(exist_ok=True)

    for index, page in enumerate(pages):
        path = poedb.CACHE / f'{page}.html'

        if not cached:
            if index > 0:
                time.sleep(1)

            path.write_text(download(page), encoding='utf-8')

        families = poedb.families(path.read_text(encoding='utf-8'))

        with open(poedb.DATA / f'{page}.json', 'w', encoding='utf-8', newline='\n') as handle:
            json.dump(families, handle, ensure_ascii=False, indent=1)

        print(f'===== {page}: {sum(len(family["tiers"]) for family in families)} Stufen in {len(families)} Familien')

        for family in families:
            print(summary(family))


if __name__ == '__main__':
    main(sys.argv[1:])
