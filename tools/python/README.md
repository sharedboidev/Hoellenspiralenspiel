# Affixe nach einem gängigen aRPG

Diese Skripte bauen die Affixe unter `Resources/Affixes/{Prefixes,Suffixes}/{Weapons,Armors}/<Klasse>` aus den Basis-Affixen eines gängigen aRPG, im Folgenden die Vorlage. Die Daten kommen aus einer öffentlichen Datenbank im Netz, ihre Adresse steht in `poedb.py`. Die Skripte übernehmen alle Stufen mit Itemlevel und Gewicht 1:1 und geben jeder Stufe einen eigenen Namen. Godot übergeht den Ordner `tools`, er hat eine `.gdignore`.

Gebraucht wird Python ab 3.10, ohne weitere Pakete. Alle Befehle laufen in diesem Ordner.

| Datei | Aufgabe |
|---|---|
| `fetch.py` | Lädt die Seiten nach `cache/` und schreibt ihre Affix-Familien nach `data/<Seite>.json`. `--cached` arbeitet ohne Download |
| `poedb.py` | Gemeinsames: Pfade, Lesen der Seiten, die Stats aus `Enums/CombatStat.cs`, die Synonyme |
| `mapping.py` | Welche Familie welche Datei wird, mit Stat, lokal oder global, Brüchen und dem zweiten Stat hybrider Affixe. Dazu die Itemklassen mit Slot und Waffentyp |
| `synonyms.json` | Name einer Stufe in der Vorlage → unser Name. Eine Stufe heißt in jeder Klasse gleich, wie in der Vorlage |
| `list_missing.py` | Zeigt Namen der Vorlage ohne Synonym und Familien, die `mapping.py` nicht übernimmt |
| `check_names.py` | Prüft die Synonyme: kein Name der Vorlage, keiner doppelt, keiner eines alten Affixes |
| `generate.py` | Schreibt die `.tres` nach `Resources/Affixes` oder in einen anderen Ordner |
| `data/` | Der Stand der Datenbank vom 05.10.2026, aus dem die Affixe im Spiel entstanden sind |

## Neu bauen

```bash
python generate.py
```

Danach die Tests laufen lassen. `PoeAffixDataTests` prüft jede Datei.

## Werte neu holen

```bash
python fetch.py
git diff --stat data
python generate.py
```

`git diff` auf `data/` zeigt, was die Datenbank seit dem letzten Stand geändert hat.

## Eine Itemklasse dazunehmen

1. In `mapping.py` die Klasse unter `CLASSES` eintragen: Seite der Datenbank, etwa `Rings` oder `Boots_str`, Ordner, `ItemSlot` und `WeaponType` als Zahl, Tabelle. Rüstungsteile gibt es je Verteidigungsart, das Spiel kennt nur Armour (`_str`).
2. `python fetch.py <Seite>` lädt sie.
3. `python list_missing.py` zeigt Familien ohne Eintrag und Namen ohne Synonym. Neue Familien kommen in `WEAPON` oder `ARMOUR`. Braucht eine Familie einen Stat, den es nicht gibt, wird sie erst abgesprochen. Neue Namen kommen nach `synonyms.json`.
4. `python check_names.py` muss ohne Befund enden.
5. `python generate.py`, dann die Klasse in `PoeAffixDataTests` eintragen und die Tests laufen lassen.
