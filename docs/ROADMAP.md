# Höllenspiralenspiel: Analyse und Roadmap

Stand: 28.09.2026. M0 bis M3 liegen auf `master`.
Der Feature-Umfang für Leser steht in der [README](../README.md), dieses Dokument enthält Analyse, Befunde und Plan.
Grundlage: Designdokument "Wyldes Gehirnsturmscribble" und der komplette C#-Code (rund 5.500 Zeilen) plus Szenen.
Die Befunde stammen aus Code-Lektüre. Die als behoben markierten Fehler, F19 und die Meilensteine M2 und M3 wurden zusätzlich im laufenden Spiel geprüft, headless mit Godot 4.6.

## Getroffene Richtungsentscheidungen

| Frage | Entscheidung |
|---|---|
| 2D oder 3D | Noch offen. Spiellogik wird zuerst von der Darstellung getrennt. |
| Spielstruktur | Hub (Stadt) plus Abstieg in einen Höllenkreis mit mehreren Ebenen |
| Leveldesign | Etwa 90 % prozedural, dazu handgebaute Räume und Event-Locations, die gezielt eingestreut werden |
| Multiplayer | Singleplayer zuerst, Koop soll später nachrüstbar bleiben |

## 1. Was schon umgesetzt ist

| Bereich | Stand | Abgleich mit dem PDF |
|---|---|---|
| Isometrische Perspektive | Testlevel mit TileMap-Layern für Boden, Wände, Objekte | Entspricht dem PDF, aber nur ein Testlevel |
| Attribute | Alle fünf Attribute mit abgeleiteten Werten und Obergrenzen | Obergrenzen stimmen exakt. Wachstum ist logistisch statt beschränkt. Lichtradius fehlt. |
| Leben und Mana | Basisformeln, Regeneration, Orbs mit Shader | Formeln stimmen exakt |
| Modifier-System | Flat, Percentage (increased) und More, sauber getrennt | Trägt das ganze Stat-Konzept |
| Waffen | Schadensspanne, Angriffe pro Sekunde, Swingtimer, Typ, Schadensart, Krit, Anforderungen | Seit M2 bestimmen die Werte den Nahkampf, seit M3 gibt es Fernkampfwaffen mit Projektil. Klassen-Anforderung fehlt. |
| Rüstung | Helm, Torso, Handschuhe mit Rüstungswert | 3 von 16 Slots haben Item-Basen |
| Affixe | 16 Affixe mit Tiers, Gewichten, Itemlevel-Grenze, Prefix/Suffix, lokale und globale Mods | Slot-Tabelle aus dem PDF nur teilweise abgedeckt |
| Loot | Gewichtete Loot-Tabellen, Lootbags, Magic/Rare-Namen | Funktioniert im Testlevel |
| Inventar | Tetris-Inventar, Drag-and-drop, Tauschen, Stapeln, Tooltips | Nicht im PDF, aber fertig nutzbar |
| Ausrüstung | 16 Slots inklusive 4 Ringe, Anforderungsprüfung | Entspricht dem PDF |
| Leveling | XP-Tabelle bis Level 100, Level-up-Effekt, Attributspunkte, XP-Balken | Nicht im PDF, funktioniert |
| Skills | Seit M3 als Daten: Attack, Lightning Strike, Fireball mit Fork, Frost Nova, Thunderbolt. Leiste mit zehn frei belegbaren Plätzen | Das PDF kennt keine Skill-Arten. Klassen und Skill-Erwerb sind offen. |
| Kampf | Zentrale Trefferauflösung, Nahkampf, Schadensarten mit Effekten, Statuseffekte, Tod und Respawn | Seit M2. Frost-Effekt war im PDF leer und ist jetzt Verlangsamung. |
| Schadensminderung | Rüstungsformel, Resistenzen, Dodge, Parry und Block für alle Einheiten | Parry und Block haben noch keine Quelle, weil Schilde fehlen |
| Gegner | 3 Typen, Spawn-Marker, Gruppen-Aggro, Rare/Elite, Lebensbalken, Schadenszahlen, eigene Angriffe. Seit M3 setzen sie Skills auf demselben Weg ein wie der Spieler | Nur Blobs und ein Testgegner |
| Atmosphäre | Punktlichter mit Schatten, abgedunkelte Szene, Overlay-Karte | Passt zum düsteren Vibe |
| 3D | Ein Prototyp, der nur Bewegung kann | Offene Frage aus dem PDF |

## 2. Was an kritischen Systemen fehlt

Ohne diese Systeme gibt es kein spielbares Spiel, nur eine Testszene.

1. **Spielertod.** Erledigt in M2. Vorher konnte das Leben unter null fallen, ohne dass etwas passierte.
2. **Nahkampf.** Erledigt in M2. Vorher existierten Waffenwerte nur im Tooltip.
3. **Gegnerangriffe.** Erledigt in M2. Vorher schadeten die Blobs nur durch Berührung.
4. **Speichern und Laden.** Es gibt keine Persistenz für Charakter, Inventar oder Fortschritt.
5. **Spielstruktur.** Hub, Levelwechsel, Hauptmenü, Pausenmenü und Freischaltung fehlen. Die Kellertür schreibt nur eine Logzeile.
6. **Levelgenerierung.** Es gibt nur ein handgebautes Testlevel.
7. **Wegfindung.** Gegner laufen in gerader Linie und bleiben an Wänden hängen.
8. **Statuseffekte.** Erledigt in M2: Bleed, stapelnder Burn, Shock und Chill. Die leeren Schadensart-Klassen sind durch eine Aufzählung im Kern ersetzt.
9. **Parry, Block, Krit aus Stats.** Erledigt in M2. Die Trefferauflösung wertet alle drei aus.
10. **Skill-Erwerb.** Seit M3 sind Skills Daten und die Leiste ist frei belegbar. Der Held kennt vorerst alle Skills. Klassen und Skill-Fortschritt fehlen.
11. **Skalierung.** Itemlevel ist immer 1. Monsterlevel und Bereichslevel existieren nicht.
12. **Inhalt.** Kein einziger Höllenkreis, kein Boss, kein Intro.
13. **Einstellungen.** Auflösung, Tastenbelegung und Lautstärke sind nicht einstellbar.
14. **Tests.** Vom Testprojekt existiert nur ein `obj`-Ordner ohne Quellcode.

## 3. Was schlecht oder imperformant umgesetzt ist

### 3.1 Fehler

Status vom 28.09.2026. Die Zeilennummern beziehen sich auf den Stand vor den Korrekturen.

| Nr. | Status | Problem | Stelle |
|---|---|---|---|
| F1 | Behoben | Stärke und Konstitution überschreiben gegenseitig ihren Rüstungsbonus, weil beide dieselbe Herkunfts-ID benutzen | [DerivedStatProvider.cs:65](../Scripts/Utils/DerivedStatProvider.cs), [DerivedStatProvider.cs:98](../Scripts/Utils/DerivedStatProvider.cs) |
| F2 | Behoben | Attribute von Ausrüstung aktualisieren die abgeleiteten Werte nicht. Nur Änderungen am Basiswert lösen die Neuberechnung aus. | [BaseUnit.cs:155](../Scripts/Units/BaseUnit.cs) |
| F3 | Behoben | Zauberschaden ignoriert den More-Multiplikator. Intelligenz erhöht den Schaden dadurch nicht. | [BaseSkill.cs:42](../Scripts/Abilities/BaseSkill.cs) |
| F4 | Behoben | Treffer auf Gegner zeigen manchmal "Dodge" an, ziehen aber trotzdem Leben ab. Heilung konnte ebenfalls "Dodge" anzeigen. | [BaseUnit.cs:48](../Scripts/Units/BaseUnit.cs), [FCTExtensions.cs:17](../Scripts/Extensions/FCTExtensions.cs) |
| F5 | Behoben | Kontaktschaden trifft in jedem Physik-Frame ohne Abklingzeit | [Player2D.cs:199](../Scripts/Units/Player2D.cs) |
| F6 | Behoben | Gegner greifen ohne Cooldown an. Der Testgegner erzeugt pro Frame einen Feuerball. Windup und Recovery werden nicht benutzt. | [BaseEnemy.cs:145](../Scripts/Units/Enemies/BaseEnemy.cs), [TestEnemy.cs:21](../Scripts/Units/Enemies/TestEnemy.cs) |
| F7 | Behoben | Ein Gegner kann mehrere Lootbags fallen lassen, wenn er nach dem Tod noch getroffen wird. Von mehreren gewürfelten Items fällt nur das erste. | [EnemyController.cs:116](../Scripts/Controllers/EnemyController.cs) |
| F8 | Behoben | Einen Trank zu trinken gibt den Slot frei, obwohl der Stapel noch Tränke enthält. Das nächste Item landet darüber. | [Inventory.cs:192](../Scripts/UI/Character/Inventory.cs) |
| F9 | Behoben | Ein fallengelassenes Item geht verloren, wenn man es bei vollem Inventar wieder aufhebt | [MouseObject.cs:72](../Scripts/UI/MouseObject.cs) |
| F10 | Behoben | Ausrüsten per Drag-and-drop umgeht die Anforderungsprüfung | [EquipmentSlot.cs:129](../Scripts/UI/Character/EquipmentSlot.cs) |
| F11 | Behoben | Das Entfernen von Modifikatoren fasst nebenbei wertgleiche Einträge zusammen. Zwei identische Affixe auf einem Item zählen danach nur noch einmal. | [BaseUnit.cs:123](../Scripts/Units/BaseUnit.cs) |
| F12 | Behoben | Der Affix-Wurf kann `null` liefern, wenn ein Slot keine passenden Affixe hat. Das führt zum Absturz. | [Lootsystem.cs:181](../Scripts/Controllers/Lootsystem.cs) |
| F13 | Behoben | Verschachtelte Loot-Tabellen sind als Typ angelegt, aber nicht umgesetzt | [LootTable.cs:42](../Resources/LootTable.cs) |
| F14 | Behoben | Derselbe Affix kann mehrfach auf einem Item landen | [Lootsystem.cs:57](../Scripts/Controllers/Lootsystem.cs) |
| F15 | Behoben | Die Todesanimation ist nie zu sehen, weil der Gegner beim Start der Animation entfernt wird | [BaseEnemy.cs:60](../Scripts/Units/Enemies/BaseEnemy.cs) |
| F16 | Behoben | Auf Level 100 ist die nächste XP-Schwelle 0. Jeder XP-Gewinn löst dann ein Level-up aus und das Level danach wirft eine Ausnahme. | [XpTable.cs:109](../Scripts/Utils/XpTable.cs) |
| F17 | Behoben | `LifeBase = 75` in den Gegner-Szenen wird ignoriert. Alle Gegner haben 9 Leben, der Feuerball macht 50 bis 75 Schaden. | [yellow_blob.tscn](../Scenes/Units/Enemies/yellow_blob.tscn) |
| F18 | Behoben | Schadenszahlen driften pro Frame statt pro Sekunde und sind damit abhängig von der Bildrate | [FloatingCombatText.cs:58](../Scripts/UI/FloatingCombatText.cs) |
| F19 | Behoben | Ein Slot meldet sich nie vom Stapel-Ereignis eines Tranks ab. Wird ein verschobener Trankstapel leer getrunken, löscht der alte Slot den fremden Trankstapel, der inzwischen dort liegt. | [InventorySlot.cs:62](../Scripts/UI/Character/InventorySlot.cs) |
| F20 | Behoben | Einheiten füllen Leben und Mana, bevor die abgeleiteten Modifier berechnet sind. Sie starten dadurch knapp unter ihrem Maximum. | [BaseUnit.cs:84](../Scripts/Units/BaseUnit.cs), [Player2D.cs:76](../Scripts/Units/Player2D.cs) |
| F21 | Behoben | Das Inventar hat einen höheren Z-Index als der Level-up-Dialog und verdeckt ihn | [level_up_dialog.tscn](../Scenes/UI/level_up_dialog.tscn) |
| F22 | Behoben | Das Statdisplay zeichnet sich nach dem Verteilen eines Attributpunkts nicht neu | [CharacterSheet.cs](../Scripts/UI/Character/CharacterSheet.cs), [BaseUnit.cs](../Scripts/Units/BaseUnit.cs) |

Hinweise zu den Korrekturen:

- F2 war pragmatisch behoben und ist seit M1 durch den Stat-Kern ersetzt.
- F4, F5 und F6 waren pragmatisch behoben und sind seit M2 durch die Kampf-Pipeline ersetzt. Der Kontaktschaden aus F5 ist entfallen, Gegner greifen jetzt selbst an.
- F17 führt den exportierten Wert `LifeBaseBonus` ein. Er wird auf das Basisleben aus den Attributen addiert.
- F18 ändert die Einheit von `DriftVelocity` auf Pixel pro Sekunde.

### 3.2 Performance

| Nr. | Problem | Stelle |
|---|---|---|
| P1 | Behoben in M1. Jeder Stat-Zugriff durchsuchte die Modifikator-Liste mehrfach mit LINQ. Das passierte pro Einheit und pro Frame mehrfach und erzeugte laufend Müll für den Garbage Collector. | [StatSheet.cs](../Scripts/Core/Stats/StatSheet.cs) |
| P2 | Behoben in M1. Die Orbs bauten jeden Frame Text neu und setzten Shader-Parameter, auch wenn sich nichts änderte. | [ResourceOrb.cs](../Scripts/UI/Character/ResourceOrb.cs) |
| P3 | Alle Gegner der Karte werden jeden Frame simuliert, egal wie weit sie entfernt sind | [EnemyController.cs:170](../Scripts/Controllers/EnemyController.cs) |
| P4 | Behoben in M3. Ein Feuerball konnte sich auf bis zu 63 Projektile aufspalten, und jeder Treffer sortierte alle Gegner der Karte nach Entfernung. Jetzt sind es höchstens 7, die Zielsuche läuft in einem Durchlauf ohne Sortieren. | [SkillProjectile.cs](../Scripts/Skills/Effects/SkillProjectile.cs), [NearestPicker.cs](../Scripts/Core/Skills/NearestPicker.cs) |
| P5 | Das Inventar nutzt Godot-Dictionaries mit Float-Vektoren als Schlüssel. Jeder Zugriff wird zwischen C# und Engine konvertiert. | [Inventory.cs:17](../Scripts/UI/Character/Inventory.cs) |
| P6 | Knoten werden in Property-Gettern bei jedem Zugriff neu gesucht | [Inventory.cs:21](../Scripts/UI/Character/Inventory.cs), [EquipmentPanel.cs:26](../Scripts/UI/Character/EquipmentPanel.cs) |
| P7 | Für den Kampf behoben in M2: Alle Würfe laufen über eine Zufallsquelle mit Seed. Offen bleiben die eigenen Zufallsquellen von Loot (M4) und Spawns (M5). | [GameRandom.cs](../Scripts/Core/Rng/GameRandom.cs), [Lootsystem.cs](../Scripts/Controllers/Lootsystem.cs), [EnemyController.cs](../Scripts/Controllers/EnemyController.cs) |
| P8 | Neu seit M2: Jede Schadenszahl ist ein eigener Knoten. Brennen viele Gegner gleichzeitig, entstehen pro Sekunde zwei Zahlen je Gegner. Bisher ohne messbare Folgen, Pooling steht in M9. | [FCTExtensions.cs](../Scripts/Extensions/FCTExtensions.cs) |
| P9 | Neu seit M3: Flächen und die Suche nach dem Gegner unter dem Mauszeiger gehen alle Einheiten der Karte durch. Bei rund 100 Gegnern ohne messbare Folgen. Eine räumliche Aufteilung gehört zu M5, zusammen mit P3. | [SkillArea.cs](../Scripts/Skills/Effects/SkillArea.cs), [BaseUnit.cs](../Scripts/Units/BaseUnit.cs) |

### 3.3 Architektur

| Nr. | Problem | Folge |
|---|---|---|
| A1 | Rund 24 Stellen suchen Spieler, Controller oder `Environment` über feste Namen in der aktuellen Szene. Skills gehören seit M3 nicht mehr dazu. | Jedes neue Level muss exakt wie das Testlevel aufgebaut sein |
| A2 | Behoben in M1. Alle Stats steckten in einer 2D-Physik-Klasse. Die Rechnung liegt jetzt in `Scripts/Core/Stats` ohne Godot. | Blockierte die 2D/3D-Entscheidung, Tests und Koop |
| A3 | Für Skills behoben in M3. Der Skillbar-Button zauberte und zog Mana ab. Jetzt zeigt die Leiste nur noch an. Offen bleibt das Inventar, das zu M4 gehört. | Logik ist ohne UI nicht nutzbar und nicht testbar |
| A4 | Items sind Szenen-Knoten, die nie im Baum hängen | Speicherleck und nicht serialisierbar |
| A5 | Behoben in M3. Es gab zwei parallele Skill-Hierarchien, Skills und Manakosten standen fest im Code. Beide Hierarchien sind durch Skill-Resources ersetzt. | Neue Skills brauchen Codeänderungen an mehreren Stellen |
| A6 | UI wird per Code anhand der Fenstergröße platziert. Das Fenster ist fest 2560x1440 im exklusiven Vollbild. | Bricht bei anderen Auflösungen |
| A7 | Lootbag-Code existiert dreimal mit unterschiedlichem Verhalten | Quelle von F9 |
| A8 | Behoben am 28.09.2026. `.idea`, `*.user` und `obj` waren eingecheckt. Shader und Testszenen lagen im Projektwurzelordner. Leere Klassen wie `SceneDispenser` und `StaticMemory` existierten. | Unübersichtlich |
| A9 | Behoben am 28.09.2026. Eingabeaktionen hießen wie Tasten (`F`, `B`, `Tab`) statt nach ihrer Funktion. Die Namen stehen jetzt zentral in `InputActions`. Seit M3 hat jeder Platz der Skill-Leiste eine eigene Aktion. | Tastenbelegung lässt sich nicht sauber ändern |
| A10 | Behoben in M3. Zauber unterschieden Freund und Feind über Typprüfungen auf `Player2D` und `BaseEnemy`, die Gruppe `monsters`, feste Kollisionsebenen und den `EnemyController`. Jetzt entscheidet allein die Fraktion. | Ein Zauber verhält sich nicht gleich für jeden, der ihn wirkt. Begleiter und Koop-Spieler sind nicht abgedeckt. |

Hinweis zu A10: Die alten Testzauber Fireball, Frost Nova und Lightning Strike sind in M3 entfallen und neu gebaut worden. Die Vorarbeit aus M2 trägt das: Jede Einheit hat eine Fraktion, und `UnitRegistry` kennt alle Einheiten im Szenenbaum.

### 3.4 Balance

Beobachtungen aus den Laufzeitprüfungen von M2 und M3. Der Balance-Durchgang steht in M8.

| Nr. | Beobachtung | Stelle |
|---|---|---|
| B1 | Rare und Elite bekommen 25 Stärke und regenerieren dadurch 5 Leben pro Sekunde. Ein unbewaffneter Spieler macht weniger Schaden und kann sie nicht töten. | [EnemyExtensions.cs](../Scripts/Extensions/EnemyExtensions.cs), [StatFormulas.cs](../Scripts/Core/Stats/StatFormulas.cs) |
| B2 | Der Spieler startet mit 9 Leben. Im Testlevel hat er 50 Bonusleben bekommen, damit ein Kampf länger als zwei Treffer dauert. | [test_plane.tscn](../Scenes/test_plane.tscn) |
| B3 | Jeder Treffer mit Fire, Frost, Lightning oder Slash löst seinen Effekt sicher aus. Eine Chance statt Gewissheit wäre eine Stellschraube. | [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs) |
| B4 | Der Held startet mit 9 Mana und regeneriert 0,5 pro Sekunde. Das reicht für vier Feuerbälle oder zwei Thunderbolts. | [Resources/Skills/Player](../Resources/Skills/Player) |
| B5 | Die Schadenswerte der Zauber stammen von den alten Testzaubern. Ein Thunderbolt mit 50 bis 350 tötet jeden Gegner des Testlevels mit einem Treffer. | [thunderbolt.tres](../Resources/Skills/Player/thunderbolt.tres) |
| B6 | Pierce trifft nur halb so oft. Mit dem Bogen geht deshalb jeder zweite Pfeil daneben, obwohl er sichtbar durch den Gegner fliegt. | [CombatRules.cs](../Scripts/Core/Combat/CombatRules.cs) |

## 4. Meilensteinplan

Leitlinien, die aus den Richtungsentscheidungen folgen:

- **Logik getrennt von Darstellung.** Stats, Kampf, Items und Levelaufbau werden reines C# ohne Godot-Knoten. Die 2D- oder 3D-Schicht zeigt nur an.
- **Inhalte als Daten.** Gegner, Skills, Items und Räume sind Resources, keine Klassen.
- **Ein Zufallsgenerator mit Seed.** Gleicher Seed ergibt gleiches Level und gleichen Loot. Das ist die Voraussetzung für Koop.

Größen: S bedeutet wenige Abende, M ein bis zwei Wochen Hobbyzeit, L mehrere Wochen.

### M0: Aufräumen und Fehler beheben (S, abgeschlossen)

Ziel: stabile Basis, bevor umgebaut wird.

- Erledigt am 28.09.2026: alle bekannten Fehler F1 bis F22.
- Erledigt am 28.09.2026: `.gitignore` erweitert, eingecheckte IDE- und Build-Dateien aus dem Repo entfernt, Wurzelordner aufgeräumt, tote Klassen gelöscht.
- Erledigt am 28.09.2026: Eingabeaktionen nach Funktion benannt, ungenutzte Aktionen `F` und `+` entfernt.
- Erledigt am 28.09.2026: Branch `master_MeleeCombat` per Fast-Forward nach `master` zusammengeführt.

Bewusst nicht angefasst:

- `Scripts/Skills` ist der begonnene Umbau der Skills und bleibt für M3.
- `Player.cs` und `test_plane_3d.tscn` sind der 3D-Prototyp und bleiben bis zur Entscheidung 2D oder 3D.
- `Scenes/Spells/thunder_shader.tres` wird von keiner Szene benutzt. Dasselbe gilt für ungenutzten Code im `EnemyController` rund um den Spawn-Timer.

Fertig, wenn das Testlevel ohne die genannten Fehler läuft und der Wurzelordner nur noch Projektdateien enthält.

### M1: Stat-Kern in reinem C# (M, umgesetzt am 28.09.2026 auf `master_StatCore`)

Ziel: ein Stat-System, das schnell, testbar und unabhängig von 2D oder 3D ist.

- Erledigt: Klasse für Stat-Blätter mit Basiswerten, Modifikatoren und zwischengespeicherten Endwerten. Neuberechnung nur bei Änderung. Behebt P1, F2, F11.
- Erledigt: Abgeleitete Werte reagieren auf den Endwert eines Attributs, also auch auf Ausrüstung.
- Erledigt: Einheiten halten ein Stat-Blatt und rechnen nicht mehr selbst. Behebt A2.
- Erledigt: Testprojekt `Hoellenspiralenspiel.Tests` mit NUnit für Stat-Berechnung und Wachstumskurven.
- Erledigt: Lichtradius als Stat, gekoppelt an die Lichter des Spielers, mit eigener Zeile im Charakterbogen.
- Erledigt: Orbs aktualisieren sich nur bei Wertänderung. Behebt P2.
- Zusätzlich: Bewegungsgeschwindigkeit, Manaregeneration und Fläche laufen ebenfalls über den Kern.

Fertig, wenn alle Werte im Charakterbogen aus dem neuen Kern kommen und die Tests grün sind.

Stand des Fertig-Kriteriums: Die Tests sind grün. Krit-Chance, Krit-Schaden und Angriffstempo kamen nach M1 noch aus der Ausrüstung. Seit M2 kommen auch sie aus dem Kern, das Kriterium ist damit erfüllt.

So funktioniert der Kern:

- `StatSheet` rechnet bei jeder Änderung einmal alles neu und speichert die Endwerte. Lesen ist nur ein Zugriff auf ein Feld.
- Reihenfolge der Rechnung: erst Attribute, dann die daraus abgeleiteten Modifier, dann alle übrigen Stats.
- Bei Leben, Mana und Lebensregeneration ist der gesetzte Grundwert ein Zuschlag auf die Formel aus den Attributen.
- Mehrere Änderungen lassen sich mit `Update` zusammenfassen, zum Beispiel beim Anlegen eines Items.
- Neue Werte im Enum `CombatStat` nur am Ende anhängen, weil Affixe den Stat als Zahl speichern.

Messung mit 200.000 Lesezugriffen auf drei Stats:

| Stand | Dauer | Angeforderter Speicher |
|---|---|---|
| Vorher | rund 480 ms | rund 640 MB |
| Nachher | rund 2,5 ms | 0 |

Tests ausführen:

```bash
dotnet test Hoellenspiralenspiel.Tests
```

Das Testprojekt bindet `Scripts/Core` als Quelltext ein. Hängt eine Datei dort von Godot ab, schlägt der Test-Build fehl.

### M2: Kampf-Pipeline, Tod und Nahkampf (M, umgesetzt am 28.09.2026 auf `master_CombatPipeline`)

Ziel: eine vollständige Kampfschleife. Das ist der erste Meilenstein, der sich wie ein Spiel anfühlt.

- Erledigt: Eine zentrale Trefferauflösung: Treffen, Ausweichen, Parry, Block, Krit, Minderung. Einmal würfeln, Ergebnis unveränderlich. Behebt F4, F5 und den Kampf-Anteil von P7.
- Erledigt: Nahkampfangriff des Spielers mit Waffenwerten: Swingtimer, Schadensspanne, Krit-Chance.
- Erledigt: Gegnerangriffe mit Windup und Recovery. Gegnerprojektile treffen den Spieler. Behebt F6.
- Erledigt: Spielertod mit XP-Verlust und Respawn.
- Erledigt: Schadensarten aus dem PDF mit ihren Effekten, physisch und elementar.
- Erledigt: Statuseffekt-System mit Bleed, stapelndem Burn, Shock und Chill.
- Zusätzlich: Krit-Chance, Krit-Schaden und Angriffstempo im Charakterbogen kommen aus dem Kern.
- Zusätzlich: Fraktionen und `UnitRegistry` als Vorarbeit für A10.

Fertig, wenn Spieler und Gegner sich gegenseitig töten können und jede Schadensart ihren Effekt auslöst.

Stand des Fertig-Kriteriums: erfüllt. 89 neue Unit-Tests decken den Kern ab. Eine Laufzeitprüfung mit 72 Schritten im Testlevel lief fünfmal hintereinander fehlerfrei, zusätzlich gab es eine Sichtprüfung mit Bildschirmfotos. Crush, Slash, Fire, Frost und Lightning sind im Testlevel erreichbar. Pierce hat noch keine Waffe und keinen Gegner und wurde mit einem umgestellten Gegner geprüft.

Getroffene Designentscheidungen vom 28.09.2026:

| Frage | Entscheidung |
|---|---|
| Frost-Effekt | Chill: Bewegung und Angriffe 30 % langsamer für 3 Sekunden, jeder Frosttreffer erneuert die Dauer |
| Parry und Block | Parry wehrt den Treffer ganz ab. Block fängt einen Anteil ab, Standard 50 %. Der Anteil ist der Stat `BlockReduction` und durch Items und Skills veränderbar. |
| Skills | Trennung in ATTACK und SPELL. Attacks sind Nah- und Fernkampfskills mit Waffen und skalieren mit dem Waffenschaden. Spells haben eigenen Grundschaden, den Affixe und Skills verändern. |
| Standardangriff | Eine ATTACK mit 100 % Waffenschaden. Vorerst fest auf der linken Maustaste. |
| Auslösen des Angriffs | Nur bei Klick auf einen Gegner. Außer Reichweite läuft der Held zuerst hin. WASD oder ein Klick auf einen anderen Gegner übersteuert das. |
| Tod | Verlust von 10 % der XP-Spanne des aktuellen Levels, kein Levelverlust. Respawn am Startpunkt mit vollem Leben und Mana. Gegner bleiben, wie sie sind. |

So funktioniert die Pipeline:

- `HitRequests` baut aus den Stats des Angreifers einen `HitRequest`. `ForAttack` nimmt Waffe und Attack, `ForSpell` nimmt den Zauber.
- `HitResolver.Resolve` würfelt sechs Werte vorab und in fester Reihenfolge: Treffen, Ausweichen, Parry, Block, Krit, Schaden. Jeder Treffer verbraucht dadurch gleich viele Würfe, und derselbe Seed ergibt denselben Kampf.
- Das Ergebnis ist ein unveränderliches `HitResult`. Es enthält auch den Statuseffekt, den der Treffer auslöst.
- `BaseUnit.ReceiveDamage` wendet das Ergebnis an: Leben abziehen, Effekt auflegen, Zahl anzeigen.
- `StatusEffectTracker` lässt die Effekte ablaufen. Chill legt seine Modifier selbst auf das Stat-Blatt und nimmt sie wieder herunter.
- `AttackCycle` ist der Takt aus Ausholen, Treffer und Erholen. Spieler und Gegner benutzen denselben.
- Die Waffe legt Angriffstempo und Krit-Chance als Grundwerte ins Stat-Blatt. Ohne Waffe gilt `WeaponProfile.Unarmed`.
- Abstände im Kampf werden zwischen den Körpermitten gemessen, also zwischen den Kollisionsformen.

Stellschrauben, alle in [CombatRules.cs](../Scripts/Core/Combat/CombatRules.cs):

| Wert | Standard |
|---|---|
| Crush, mehr Schaden | 20 % |
| Pierce, weniger Trefferchance | 50 % |
| Bleed | 50 % des ungeminderten Treffers über 4 s, nur der stärkste wirkt |
| Burn | 25 % des erlittenen Schadens über 4 s, höchstens 10 Stapel |
| Shock | 25 % Fehlschlag für 4 s |
| Chill | 30 % langsamer für 3 s |
| Krit-Schaden | +50 % |
| Trefferchance | 100 % |
| Abgefangener Anteil beim Block | 50 % |
| Takt der Schadenszahlen | 0,5 s |
| Unbewaffnet | 1 bis 3 Crush, 1,2 Angriffe pro Sekunde, 5 % Krit, Reichweite 100 |
| XP-Verlust beim Tod | 10 %, in [DeathPenalty.cs](../Scripts/Core/Progression/DeathPenalty.cs) |

Bewusst offen gelassen:

- Parry und Block haben die Grundchance 0. Eine Quelle kommt erst mit Schilden und passenden Waffen in M4 und M8.
- Parry und Block unterscheiden nur ATTACK und SPELL. Fernkampf-Attacks benutzen die Werte des Nahkampfs. Das gilt auch nach M3.
- Der Held läuft in gerader Linie zum Ziel. Bleibt er hängen, gibt er das Ziel nach 0,4 Sekunden auf. Wegfindung kommt in M5.
- Die Angriffsanimation des Spielers benutzt das vorhandene graue Platzhalter-Sprite und spielt für alle Waffen die Einhand-Animation.
- Ein fehlgeschlagener Zauber wurde im Skillbar-Button behandelt. Seit M3 regelt das der Spieler.
- Gegner finden den Spieler weiter über den festen Namen in der Szene. Das gehört zu A1.

### M3: Skills als Daten (M, umgesetzt am 28.09.2026 auf `master_SkillsAsData`)

Ziel: neue Skills ohne Codeänderung am Spieler.

- Erledigt: Skill-Definition als Resource mit Kosten, Cooldown, Schaden, Schadensart, Szene und Icon.
- Erledigt: Zwei Arten von Skills als Resource, `AttackSkillResource` und `SpellSkillResource`. Der Kern kennt beide als `SkillDefinition`.
- Erledigt: Der Standardangriff ist eine ATTACK wie jede andere und liegt auf einem Platz der Leiste. Die feste Bindung an die linke Maustaste ist entfallen.
- Erledigt: Fernkampf-Attacks mit Projektilen. Ein Bogen als Platzhalter macht Pierce spielbar.
- Erledigt: Eine Skill-Hierarchie statt zwei. Die Leiste zeigt nur noch an. Behebt A3 für Skills und A5.
- Erledigt: Skillbar frei belegbar, mit zehn Plätzen.
- Erledigt: Die drei Testzauber sind neu gebaut. Behebt P4.
- Erledigt: Skills sind unabhängig davon, wer sie einsetzt. Behebt A10.
  - Ein Skill trifft Einheiten, deren Fraktion sich von der des Wirkenden unterscheidet.
  - Die Trefferlogik arbeitet nur mit `BaseUnit`, ohne Typprüfung auf Spieler oder Gegner.
  - Die Zielsuche für Forks und Flächen fragt `UnitRegistry`, nicht den `EnemyController`.
  - Die Kollisionsmaske eines Projektils ergibt sich aus der Fraktion. Alles andere auf der Maske ist eine Wand.
  - Geschwindigkeit, Lebenszeit und Anzahl der Forks kommen aus der Skill-Resource.
  - Projektile und Flächen hängen am Elternknoten des Wirkenden, ohne festen Pfad durch die Szene.
- Zusätzlich: Flächen als zweite allgemeine Wirkung neben dem Projektil, um den Wirkenden oder am gezielten Punkt.
- Zusätzlich: Gegner bekommen ihren Skill in der Szene zugewiesen. Der Testgegner hat keinen eigenen Angriffscode mehr.

Fertig, wenn ein neuer Zauber nur aus einer Resource und einer Szene besteht und derselbe Zauber von Spieler und Gegner gewirkt werden kann.

Stand des Fertig-Kriteriums: erfüllt. 55 neue Unit-Tests decken den Kern ab, insgesamt sind es 235. Eine Laufzeitprüfung mit 109 Schritten im Testlevel lief achtmal hintereinander fehlerfrei, zusätzlich gab es eine Sichtprüfung mit Bildschirmfotos. In der Prüfung wirkt der Testgegner den Feuerball des Helden und trifft damit den Helden, aber kein Monster.

Getroffene Designentscheidungen vom 28.09.2026:

| Frage | Entscheidung |
|---|---|
| Klassen und Skill-Erwerb | Vertagt. Der Held kennt alle Skills im Ordner `Resources/Skills/Player`. Die Skill-Resource hat ein Feld für Anforderungen, das leer bleibt. |
| Plätze der Leiste | Zehn: linke und rechte Maustaste, Q, E, R, F, 1, 2, 3, 4 |
| Zuweisen | Rechtsklick auf einen Platz öffnet die Liste aller bekannten Skills, ein Klick legt den Skill dort ab |
| Fernkampf | Klick auf einen Gegner: Der Held läuft in Reichweite und schießt. Ohne Gegner unter dem Mauszeiger schießt er aus dem Stand in Richtung Maus. |

Von mir festgelegt, weil es sich aus dem Umbau ergab. Alles lässt sich in den Resources ändern:

| Punkt | Festlegung |
|---|---|
| Name des Zaubers mit Zielkreis | Thunderbolt. Lightning Strike heißt jetzt die ATTACK aus dem Beispiel vom 28.09.2026. |
| Auslösen des Thunderbolt | Sofort am Mauszeiger, der Blitz schlägt nach 0,5 Sekunden ein. Der zweite Klick zum Platzieren ist entfallen, weil die linke Maustaste jetzt selbst ein Platz der Leiste ist. |
| Forks | Jeder Wurf trifft jede Einheit höchstens einmal. Zwei Forks pro Treffer über zwei Generationen ergeben höchstens 7 Projektile. |
| Reichweite des Feuerballs | 2000 statt vorher 12000 |
| Gehaltene Taste | Wiederholt den Skill, sobald er wieder bereit ist. Ein Zauber ohne Abklingzeit ist nach 0,1 Sekunden wieder bereit. |
| Startbelegung | Attack, Lightning Strike, leer, Frost Nova, Thunderbolt, Fireball, danach vier leere Plätze |

So funktionieren Skills:

- Eine Skill-Resource liegt unter `Resources/Skills`. `SkillLibrary` lädt alle Resources aus `Resources/Skills/Player`, ein neuer Skill braucht dort keinen Eintrag.
- `Delivery` bestimmt, wie der Skill ins Ziel kommt: `Weapon`, `Projectile`, `AreaAroundCaster` oder `AreaAtPoint`.
- Bei `Weapon` entscheidet die Waffe. Nahkampfwaffen treffen direkt, Fernkampfwaffen schießen die Szene, die an der Waffe hängt.
- `SkillExecutor.Execute` bringt die Wirkung in die Welt. Spieler und Gegner rufen dieselbe Methode auf.
- `SkillCast` hält Fraktion und Treffer fest. Beides steht beim Auslösen fest, ein Projektil wirkt also weiter, wenn sein Wirkender inzwischen tot ist.
- Für die Wirkung gibt es zwei allgemeine Skripte: `SkillProjectile` und `SkillArea`. Eine neue Szene hängt eines davon an ihren Wurzelknoten und bringt nur Aussehen und Kollisionsform mit.
- Flächen suchen ihre Ziele über den Abstand, nicht über die Physik. Der Boden ist isometrisch gestaucht, der Abstand nach oben und unten zählt deshalb doppelt.
- Kosten und Abklingzeit regelt, wer den Skill einsetzt. `BaseUnit.TryPayFor` prüft beides über `SkillGate` und zahlt.
- Eine ATTACK läuft über den Takt der Waffe und zahlt beim Ausholen. Ein SPELL wirkt sofort.
- Die Belegung der Leiste ist ein `SkillLoadout` im Kern und speichert nur die Ids der Skills. Die Startbelegung steht in `Resources/Skills/starting_loadout.tres`.
- Jeder Platz hat eine Eingabeaktion `skill_slot_1` bis `skill_slot_10` in den Projekteinstellungen.

Ein neuer Skill in drei Schritten:

1. Szene anlegen, Wurzelknoten `Area2D` mit `SkillProjectile` oder `Node2D` mit `SkillArea`. Projektile zeigen nach rechts.
2. Resource vom Typ `AttackSkillResource` oder `SpellSkillResource` unter `Resources/Skills/Player` anlegen, Id vergeben und die Szene eintragen.
3. Im Spiel per Rechtsklick auf einen Platz legen.

Vorläufige Werte der Skills, alle in [Resources/Skills](../Resources/Skills):

| Skill | Art | Schaden | Mana | Abklingzeit | Wirkung |
|---|---|---|---|---|---|
| Attack | ATTACK | 100 % Waffenschaden | 0 | keine | Treffer der Waffe |
| Lightning Strike | ATTACK | 180 % Waffenschaden als Lightning | 3 | keine | Projektil, Reichweite 770 |
| Fireball | SPELL | 50 bis 75 Fire | 2 | 0,25 s | Projektil, Reichweite 2000, Forks bis 600 |
| Frost Nova | SPELL | 10 bis 50 Frost | 2 | 0,5 s | Fläche um den Helden, Radius 355, wächst in 0,2 s |
| Thunderbolt | SPELL | 50 bis 350 Lightning | 4 | 1 s | Fläche am Mauszeiger, Radius 243, nach 0,5 s |
| Fire Spit | SPELL | 3 bis 6 Fire | 0 | keine | Skill des Testgegners, dieselbe Szene wie Fireball |
| Short Bow | Waffe | 5 bis 11 Pierce | | | 1,2 Angriffe pro Sekunde, Reichweite 700 |

Bewusst offen gelassen:

- Klassen und Skill-Erwerb. Die Frage muss vor dem Meilenstein beantwortet sein, der Skills freischaltet.
- Die Tasten lassen sich nur in den Projekteinstellungen ändern. Eine Einstellung im Spiel kommt in M7.
- Die Belegung der Leiste wird nicht gespeichert. Das gehört zum Speichern in M4.
- Gegner zahlen für Skills weder Mana noch Abklingzeit. Ihr Takt kommt weiter aus Windup und Recovery. Das gehört zu M5.
- Gegner finden den Spieler weiter über den festen Namen in der Szene. Das gehört zu A1.
- Zauber haben keine Zauberzeit und keine Animation am Helden.
- Der Bogen ist ein Platzhalter mit gezeichnetem Icon und fällt bei Blue Blobs. Die Angriffsanimation bleibt die Einhand-Animation.
- Die Leiste wird weiter per Code platziert. Das gehört zu A6.
- Icons der Skills stammen aus dem vorhandenen Archiv unter `Textures/Spells/Archive/icons`.

### M4: Items als Daten und Speichern (M)

Ziel: Charakter und Fortschritt überleben einen Neustart.

- Item-Basen als Resource, Item-Instanzen als reine Daten. Behebt A4.
- Inventar-Modell getrennt von der Inventar-Oberfläche, mit Ganzzahl-Koordinaten. Behebt F8, P5, P6.
- Aufgehobene Tränke landen automatisch auf vorhandenen Stapeln.
- Eine einzige Lootbag-Logik. Alle gewürfelten Items fallen. Behebt F7, F9, A7.
- Keine doppelten Affixe, verschachtelte Loot-Tabellen. Behebt F13, F14.
- Schilde und Waffen als Quelle für Parry und Block. Affix für den abgefangenen Anteil beim Block (`BlockReduction`).
- Loot würfelt über die gemeinsame Zufallsquelle mit Seed. Behebt den Loot-Anteil von P7.
- Speichern und Laden von Charakter, Inventar, Ausrüstung und Fortschritt.

Fertig, wenn ein Charakter mit Ausrüstung nach Neustart identisch geladen wird.

### M5: Gegner-KI und Skalierung (M)

Ziel: Gegner, die sich durch Level bewegen und mit der Tiefe stärker werden.

- Gegner-Definition als Resource: Attribute, Level, Loot-Tabelle, XP, Angriffe. Behebt F17.
- Zustandsmaschine: Idle, Verfolgen, Windup, Angriff, Recovery, Tod. Todesanimation läuft zu Ende. Behebt F15.
- Wegfindung auf einem logischen Gitter, das für 2D und 3D gleich funktioniert. Auch der Held benutzt sie, wenn er zu einem angeklickten Gegner läuft.
- Spawns und Rare/Elite würfeln über die gemeinsame Zufallsquelle mit Seed. Behebt den Spawn-Anteil von P7.
- Nur Gegner in Spielernähe werden simuliert. Behebt P3.
- Monsterlevel bestimmt Itemlevel. Rare und Elite bekommen eigene Modifikatoren.

Fertig, wenn Gegner um Wände herum laufen und 200 Gegner auf der Karte die Bildrate nicht senken.

### Entscheidungspunkt: 2D oder 3D

Spätestens hier muss die Entscheidung fallen, weil M6 Levelgrafik erzeugt.
Vorschlag: ein zeitlich begrenzter Vergleich. Spieler, ein Gegner und ein Zauber laufen einmal in 3D auf demselben Logik-Kern. Danach wird der Aufwand pro neuem Gegner in beiden Varianten verglichen.

### M6: Prozedurale Level mit handgebauten Räumen (L)

Ziel: jeder Abstieg sieht anders aus, und eigene Räume lassen sich einstreuen.

- Generator erzeugt einen logischen Grundriss aus Räumen und Gängen, gesteuert über einen Seed.
- Raumvorlagen sind handgebaute Szenen mit Anschlusspunkten, Spawn-Markern und Gewicht.
- Regeln pro Vorlage: Häufigkeit, frühestes Level, "muss einmal pro Kreis vorkommen" für Event-Locations.
- Thema pro Höllenkreis als Resource: Tiles oder Meshes, Licht, Gegnerpool, Musik.
- Overlay-Karte liest den logischen Grundriss.
- Feste Szenenpfade durch globale Dienste ersetzen. Behebt A1.

Fertig, wenn derselbe Seed zweimal dasselbe Level ergibt und ein handgebauter Event-Raum garantiert erscheint.

### M7: Hub und Abstieg (M)

Ziel: die Spielstruktur steht.

- Hauptmenü, Pausenmenü, Todesbildschirm, Ladebildschirm.
- Hub-Szene mit Portal in die freigeschalteten Höllenkreise.
- Mehrere Ebenen pro Kreis, Treppen, Checkpoints, Town-Portal.
- Hub-Funktionen: Truhe und Händler. Dafür braucht es eine Währung.
- Freischaltung des nächsten Kreises nach dem Boss.
- Einstellungen für Auflösung, Tasten, Lautstärke. UI über Anker statt Code. Behebt A6.

Fertig, wenn man vom Hauptmenü in den Hub, in einen Kreis, zurück und wieder hinein kommt.

### M8: Vertikaler Schnitt, ein kompletter Höllenkreis (L)

Ziel: ein Kreis in Endqualität als Vorlage für alle weiteren.

- Vorschlag ist der zweite Kreis aus dem PDF: Wollust, ewiger Sturm. Wind als Levelmechanik.
- Vier bis fünf Gegnertypen, ein Boss, zwei Event-Räume.
- Item-Basen für alle 16 Slots und Affixe nach der Slot-Tabelle des PDF.
- Balance-Durchgang für Leben, Schaden, XP und Loot. Dazu gehören B1 bis B3 und die Stellschrauben aus M2.
- Ton und Musik.

Fertig, wenn ein Durchlauf des Kreises 30 bis 60 Minuten dauert und Spaß macht.

### M9: Inhalt und Politur (L, fortlaufend)

- Die übrigen acht Kreise nach dem Muster aus M8.
- Humoristisches Intro.
- Objekt-Pooling für Schadenszahlen und Projektile, falls Messungen es nötig machen.

### M10: Koop (L, optional)

- Host-autoritatives Netzwerk auf Basis des Logik-Kerns und der Seeds.
- Möglich ohne Umbau, wenn M1 bis M6 die Leitlinien einhalten.

## Reihenfolge und Abhängigkeiten

```
M0 -> M1 -> M2 -> M3
             |
             +-> M4 -> M5 -> [2D/3D] -> M6 -> M7 -> M8 -> M9 -> M10
```

M3 und M4 sind voneinander unabhängig und können getauscht werden.
