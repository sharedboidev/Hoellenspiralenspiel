# Höllenspiralenspiel: Analyse und Roadmap

Stand: 28.09.2026. M0 bis M3 liegen auf `master`, dazu die beiden Nachträge zu M3: Schadenswerte im Tooltip und ausgedünnte Kommentare.
M4 ist auf dem Branch `master_ItemsAndSaving` umgesetzt, dazu der Nachtrag zu M2: Bleed stapelt.
Der Feature-Umfang für Leser steht in der [README](../README.md), dieses Dokument enthält Analyse, Befunde und Plan.
Grundlage: Designdokument "Wyldes Gehirnsturmscribble" und der komplette C#-Code (rund 5.500 Zeilen) plus Szenen.
Die Befunde stammen aus Code-Lektüre. Die als behoben markierten Fehler, F19 und die Meilensteine M2 bis M4 wurden zusätzlich im laufenden Spiel geprüft, headless mit Godot 4.6.

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
| Waffen | Schadensspanne, Angriffe pro Sekunde, Swingtimer, Typ, Schadensart, Krit, Anforderungen | Seit M2 bestimmen die Werte den Nahkampf, seit M3 gibt es Fernkampfwaffen mit Projektil. Seit M4 sind Waffen Resources und bringen Parry oder Block mit. Klassen-Anforderung fehlt. |
| Rüstung | Helm, Torso, Handschuhe und Schild mit Rüstungswert | 4 von 16 Slots haben Item-Basen |
| Affixe | 17 Affixe mit Tiers, Gewichten, Itemlevel-Grenze, Prefix/Suffix, lokale und globale Mods | Slot-Tabelle aus dem PDF nur teilweise abgedeckt |
| Loot | Gewichtete Loot-Tabellen, Lootbags, Magic/Rare-Namen | Seit M4 würfelt der Kern mit der gemeinsamen Zufallsquelle |
| Inventar | Tetris-Inventar, Drag-and-drop, Tauschen, Stapeln, Tooltips | Nicht im PDF. Seit M4 ein Modell im Kern, die Oberfläche zeigt nur an. |
| Ausrüstung | 16 Slots inklusive 4 Ringe, Anforderungsprüfung | Entspricht dem PDF. Seit M4 sperrt eine Zweihandwaffe den Schildplatz. |
| Speichern | Seit M4: Charakter, Inventar, Ausrüstung und Skill-Leiste, automatisch | Nicht im PDF |
| Leveling | XP-Tabelle bis Level 100, Level-up-Effekt, Attributspunkte, XP-Balken | Nicht im PDF, funktioniert |
| Skills | Seit M3 als Daten: Attack, Lightning Strike, Fireball mit Fork, Frost Nova, Thunderbolt. Leiste mit zehn frei belegbaren Plätzen, Tooltip mit DPS | Das PDF kennt keine Skill-Arten. Klassen und Skill-Erwerb sind offen. |
| Kampf | Zentrale Trefferauflösung, Nahkampf, Schadensarten mit Effekten, Statuseffekte, Tod und Respawn | Seit M2. Frost-Effekt war im PDF leer und ist jetzt Verlangsamung. |
| Schadensminderung | Rüstungsformel, Resistenzen, Dodge, Parry und Block für alle Einheiten | Seit M4 bringen Schild, Stab und Schwert Block und Parry mit |
| Gegner | 3 Typen, Spawn-Marker, Gruppen-Aggro, Rare/Elite, Lebensbalken, Schadenszahlen, eigene Angriffe. Seit M3 setzen sie Skills auf demselben Weg ein wie der Spieler | Nur Blobs und ein Testgegner |
| Atmosphäre | Punktlichter mit Schatten, abgedunkelte Szene, Overlay-Karte | Passt zum düsteren Vibe |
| 3D | Ein Prototyp, der nur Bewegung kann | Offene Frage aus dem PDF |

## 2. Was an kritischen Systemen fehlt

Ohne diese Systeme gibt es kein spielbares Spiel, nur eine Testszene.

1. **Spielertod.** Erledigt in M2. Vorher konnte das Leben unter null fallen, ohne dass etwas passierte.
2. **Nahkampf.** Erledigt in M2. Vorher existierten Waffenwerte nur im Tooltip.
3. **Gegnerangriffe.** Erledigt in M2. Vorher schadeten die Blobs nur durch Berührung.
4. **Speichern und Laden.** Erledigt in M4. Vorher gab es keine Persistenz für Charakter, Inventar oder Fortschritt.
5. **Spielstruktur.** Hub, Levelwechsel, Hauptmenü, Pausenmenü und Freischaltung fehlen. Die Kellertür schreibt nur eine Logzeile.
6. **Levelgenerierung.** Es gibt nur ein handgebautes Testlevel.
7. **Wegfindung.** Gegner laufen in gerader Linie und bleiben an Wänden hängen.
8. **Statuseffekte.** Erledigt in M2: Bleed, Burn, Shock und Chill. Bleed und Burn stapeln. Die leeren Schadensart-Klassen sind durch eine Aufzählung im Kern ersetzt.
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
| P5 | Behoben in M4. Das Inventar nutzte Godot-Dictionaries mit Float-Vektoren als Schlüssel. Jeder Zugriff wurde zwischen C# und Engine konvertiert. Jetzt rechnet ein Raster im Kern mit ganzen Feldern. | [InventoryGrid.cs](../Scripts/Core/Items/InventoryGrid.cs) |
| P6 | Behoben in M4. Knoten wurden in Property-Gettern bei jedem Zugriff neu gesucht. Inventar und Ausrüstung holen ihre Knoten jetzt einmal. | [Inventory.cs](../Scripts/UI/Character/Inventory.cs), [EquipmentPanel.cs](../Scripts/UI/Character/EquipmentPanel.cs) |
| P7 | Für den Kampf behoben in M2, für Loot in M4: Alle Würfe laufen über eine Zufallsquelle mit Seed. Offen bleiben die eigenen Zufallsquellen der Spawns (M5). | [GameRandom.cs](../Scripts/Core/Rng/GameRandom.cs), [LootRoller.cs](../Scripts/Core/Items/LootRoller.cs), [EnemyController.cs](../Scripts/Controllers/EnemyController.cs) |
| P8 | Neu seit M2: Jede Schadenszahl ist ein eigener Knoten. Brennen viele Gegner gleichzeitig, entstehen pro Sekunde zwei Zahlen je Gegner. Bisher ohne messbare Folgen, Pooling steht in M9. | [FCTExtensions.cs](../Scripts/Extensions/FCTExtensions.cs) |
| P9 | Neu seit M3: Flächen und die Suche nach dem Gegner unter dem Mauszeiger gehen alle Einheiten der Karte durch. Bei rund 100 Gegnern ohne messbare Folgen. Eine räumliche Aufteilung gehört zu M5, zusammen mit P3. | [SkillArea.cs](../Scripts/Skills/Effects/SkillArea.cs), [BaseUnit.cs](../Scripts/Units/BaseUnit.cs) |

### 3.3 Architektur

| Nr. | Problem | Folge |
|---|---|---|
| A1 | Rund 15 Stellen suchen Spieler, Controller oder Tooltip über feste Namen in der aktuellen Szene. Skills gehören seit M3 nicht mehr dazu, Items und Lootbags seit M4. | Jedes neue Level muss exakt wie das Testlevel aufgebaut sein |
| A2 | Behoben in M1. Alle Stats steckten in einer 2D-Physik-Klasse. Die Rechnung liegt jetzt in `Scripts/Core/Stats` ohne Godot. | Blockierte die 2D/3D-Entscheidung, Tests und Koop |
| A3 | Für Skills behoben in M3, für das Inventar in M4. Der Skillbar-Button zauberte und zog Mana ab, das Inventar rechnete in seinen Knoten. Jetzt zeigen Leiste und Inventar nur noch an. | Logik ist ohne UI nicht nutzbar und nicht testbar |
| A4 | Behoben in M4. Items waren Szenen-Knoten, die nie im Baum hingen. Jetzt sind sie reine Daten. | Speicherleck und nicht serialisierbar |
| A5 | Behoben in M3. Es gab zwei parallele Skill-Hierarchien, Skills und Manakosten standen fest im Code. Beide Hierarchien sind durch Skill-Resources ersetzt. | Neue Skills brauchen Codeänderungen an mehreren Stellen |
| A6 | UI wird per Code anhand der Fenstergröße platziert. Das Fenster ist fest 2560x1440 im exklusiven Vollbild. | Bricht bei anderen Auflösungen |
| A7 | Behoben in M4. Lootbag-Code existierte dreimal mit unterschiedlichem Verhalten. Jetzt gibt es einen Aufruf für alle. | Quelle von F9 |
| A8 | Behoben am 28.09.2026. `.idea`, `*.user` und `obj` waren eingecheckt. Shader und Testszenen lagen im Projektwurzelordner. Leere Klassen wie `SceneDispenser` und `StaticMemory` existierten. | Unübersichtlich |
| A9 | Behoben am 28.09.2026. Eingabeaktionen hießen wie Tasten (`F`, `B`, `Tab`) statt nach ihrer Funktion. Die Namen stehen jetzt zentral in `InputActions`. Seit M3 hat jeder Platz der Skill-Leiste eine eigene Aktion. | Tastenbelegung lässt sich nicht sauber ändern |
| A10 | Behoben in M3. Zauber unterschieden Freund und Feind über Typprüfungen auf `Player2D` und `BaseEnemy`, die Gruppe `monsters`, feste Kollisionsebenen und den `EnemyController`. Jetzt entscheidet allein die Fraktion. | Ein Zauber verhält sich nicht gleich für jeden, der ihn wirkt. Begleiter und Koop-Spieler sind nicht abgedeckt. |

Hinweis zu A10: Die alten Testzauber Fireball, Frost Nova und Lightning Strike sind in M3 entfallen und neu gebaut worden. Die Vorarbeit aus M2 trägt das: Jede Einheit hat eine Fraktion, und `UnitRegistry` kennt alle Einheiten im Szenenbaum.

### 3.4 Balance

Beobachtungen aus den Laufzeitprüfungen von M2 bis M4. Der Balance-Durchgang steht in M8.

| Nr. | Beobachtung | Stelle |
|---|---|---|
| B1 | Rare und Elite bekommen 25 Stärke und regenerieren dadurch 5 Leben pro Sekunde. Ein unbewaffneter Spieler macht weniger Schaden und kann sie nicht töten. | [EnemyExtensions.cs](../Scripts/Extensions/EnemyExtensions.cs), [StatFormulas.cs](../Scripts/Core/Stats/StatFormulas.cs) |
| B2 | Der Spieler startet mit 9 Leben. Im Testlevel hat er 50 Bonusleben bekommen, damit ein Kampf länger als zwei Treffer dauert. | [test_plane.tscn](../Scenes/test_plane.tscn) |
| B3 | Jeder Treffer mit Fire, Frost, Lightning oder Slash löst seinen Effekt sicher aus. Eine Chance statt Gewissheit wäre eine Stellschraube. | [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs) |
| B4 | Der Held startet mit 9 Mana und regeneriert 0,5 pro Sekunde. Das reicht für vier Feuerbälle oder zwei Thunderbolts. | [Resources/Skills/Player](../Resources/Skills/Player) |
| B5 | Die Schadenswerte der Zauber stammen von den alten Testzaubern. Ein Thunderbolt mit 50 bis 350 tötet jeden Gegner des Testlevels mit einem Treffer. | [thunderbolt.tres](../Resources/Skills/Player/thunderbolt.tres) |
| B6 | Pierce trifft nur halb so oft. Mit dem Bogen geht deshalb jeder zweite Pfeil daneben, obwohl er sichtbar durch den Gegner fliegt. | [CombatRules.cs](../Scripts/Core/Combat/CombatRules.cs) |
| B7 | Neu seit M4: Die Werte für Parry und Block der ersten Items sind geschätzt. Awareness verstärkt Block und Parry gegen Attacks, nicht gegen Spells. | [Resources/Items](../Resources/Items), [DerivedStatProvider.cs](../Scripts/Core/Stats/DerivedStatProvider.cs) |
| B8 | Neu seit dem 28.09.2026: Bleed stapelt ohne Obergrenze und legt auf Dauer 50 % des Trefferschadens obendrauf. Burn bringt 25 % und endet bei 10 Stapeln. Eine Obergrenze für Bleed ist die Stellschraube. | [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs) |

## 4. Meilensteinplan

Leitlinien, die aus den Richtungsentscheidungen folgen:

- **Logik getrennt von Darstellung.** Stats, Kampf, Items und Levelaufbau werden reines C# ohne Godot-Knoten. Die 2D- oder 3D-Schicht zeigt nur an.
- **Inhalte als Daten.** Gegner, Skills, Items und Räume sind Resources, keine Klassen.
- **Ein Zufallsgenerator mit Seed.** Gleicher Seed ergibt gleiches Level und gleichen Loot. Das ist die Voraussetzung für Koop.
- **Sparsame Kommentare.** Namen sollen für sich sprechen. Ein Kommentar steht nur dort, wo der Code etwas nicht zeigt: eine Falle, ein Warum, eine Eigenheit der Engine. Aufbau und Regeln erklärt dieses Dokument.

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
- Erledigt: Statuseffekt-System mit Bleed, stapelndem Burn, Shock und Chill. Seit dem Nachtrag unten stapelt auch Bleed.
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
- `HitResult` kennt drei Stufen des Schadens. `RolledDamage` ist der gewürfelte Wert. `UnmitigatedDamage` ist der Wert nach Krit, Faktor der Schadensart und Block. `FinalDamage` ist der Wert nach Rüstung oder Resistenz und wird vom Leben abgezogen.
- Einheiten: Chancen, Krit-Schaden und Resistenzen sind Prozent. Die Anteile in `CombatRules` sind Brüche, 0,5 bedeutet 50 %. Nur die drei Werte mit `Base` im Namen sind dort Prozent.
- Die Stärke eines Statuseffekts ist bei Bleed und Burn der Schaden pro Sekunde, bei Shock und Chill ein Anteil von 0 bis 1.
- Ab 100 % Resistenz ist ein Ziel immun. Negative Resistenz erhöht den Schaden.
- Pierce ignoriert die Rüstung. Auch ein abgewehrter Treffer macht einen Gegner aggressiv.

Stellschrauben, alle in [CombatRules.cs](../Scripts/Core/Combat/CombatRules.cs):

| Wert | Standard |
|---|---|
| Crush, mehr Schaden | 20 % |
| Pierce, weniger Trefferchance | 50 % |
| Bleed | 50 % des ungeminderten Treffers über 4 s, stapelt ohne Obergrenze |
| Burn | 25 % des erlittenen Schadens über 4 s, höchstens 10 Stapel |
| Shock | 25 % Fehlschlag für 4 s |
| Chill | 30 % langsamer für 3 s |
| Krit-Schaden | +50 % |
| Trefferchance | 100 % |
| Abgefangener Anteil beim Block | 50 % |
| Takt der Schadenszahlen | 0,5 s |
| Langsamster Angriff | 0,1 Angriffe pro Sekunde, egal wie stark das Angriffstempo gesenkt ist |
| Kürzeste Abklingzeit eines Zaubers | 0,1 s, auch wenn der Zauber selbst keine hat |
| Unbewaffnet | 1 bis 3 Crush, 1,2 Angriffe pro Sekunde, 5 % Krit, Reichweite 100 |
| XP-Verlust beim Tod | 10 %, in [DeathPenalty.cs](../Scripts/Core/Progression/DeathPenalty.cs) |

Bewusst offen gelassen:

- Parry und Block haben die Grundchance 0. Seit M4 bringen Schild, Stab und Schwert die ersten Werte mit, weitere folgen in M8.
- Parry und Block unterscheiden nur ATTACK und SPELL. Fernkampf-Attacks benutzen die Werte des Nahkampfs. Das gilt auch nach M3.
- Der Held läuft in gerader Linie zum Ziel. Bleibt er hängen, gibt er das Ziel nach 0,4 Sekunden auf. Wegfindung kommt in M5.
- Die Angriffsanimation des Spielers benutzt das vorhandene graue Platzhalter-Sprite und spielt für alle Waffen die Einhand-Animation.
- Ein fehlgeschlagener Zauber wurde im Skillbar-Button behandelt. Seit M3 regelt das der Spieler.
- Gegner finden den Spieler weiter über den festen Namen in der Szene. Das gehört zu A1.

#### Nachtrag vom 28.09.2026: Bleed stapelt

Entscheidung vom 28.09.2026: Bleed stapelt wie Burn, aber ohne Obergrenze. Vorher wirkte nur die stärkste Blutung.

| Punkt | Vorher | Jetzt |
|---|---|---|
| Mehrere Blutungen | Nur die stärkste wirkt | Alle wirken, ihr Schaden addiert sich |
| Obergrenze | Nicht nötig | Keine |
| Ende | Eine stärkere Blutung ersetzt die schwächere | Jede Blutung läuft ihre 4 Sekunden und endet für sich |
| Schaden auf Dauer | Höchstens 12,5 % eines Treffers pro Sekunde | 50 % des Schadens aller Treffer |

- Die Regel ist eine Zeile in [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs).
- Der Tooltip folgt der Regel von selbst. `SkillDamageEstimator` liest Stapelregel und Obergrenze aus `StatusEffectRules`. Eine spätere Obergrenze für Bleed braucht dort keine Änderung.
- Slash legt damit auf Dauer die Hälfte des Trefferschadens obendrauf. Zum Vergleich: Crush bringt 20 %, Burn 25 %.
- Shock und Chill bleiben bei der Regel, dass nur der stärkste Effekt wirkt.

Geprüft: 11 neue Unit-Tests, insgesamt 425. Sechs davon lassen die Effekte 60 Sekunden lang ablaufen und vergleichen ihren Schaden mit der Schätzung, für Bleed und Burn bei langsamem, mittlerem und schnellem Angriffstempo. Im laufenden Spiel ergaben zwei Minuten Nahkampf mit dem Schwert 14,75 DPS, der Tooltip zeigte 14,6. Auf dem Ziel lagen im Mittel 5,7 Blutungen.

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

Stand des Fertig-Kriteriums: erfüllt. 55 neue Unit-Tests decken den Kern ab, insgesamt waren es damit 235. Eine Laufzeitprüfung mit 109 Schritten im Testlevel lief achtmal hintereinander fehlerfrei, zusätzlich gab es eine Sichtprüfung mit Bildschirmfotos. In der Prüfung wirkt der Testgegner den Feuerball des Helden und trifft damit den Helden, aber kein Monster.

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
- Die Abklingzeit gehört zum Skill, nicht zum Platz. Liegt derselbe Skill auf mehreren Plätzen, zeigen alle dieselbe Abklingzeit.
- Eine Skill-Resource baut ihre Definition beim ersten Zugriff und behält sie. Wer Werte der Resource im laufenden Spiel ändert, sieht davon nichts.
- `CollisionLayers` spiegelt die Kollisionsebenen aus den Projekteinstellungen. Ändert sich dort eine Ebene, muss die Klasse folgen.
- Gegner zahlen für Skills weder Mana noch Abklingzeit, ihr `AvailableMana` ist unbegrenzt.

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
- Die Belegung der Leiste wurde nicht gespeichert. Seit M4 steht sie im Spielstand.
- Gegner zahlen für Skills weder Mana noch Abklingzeit. Ihr Takt kommt weiter aus Windup und Recovery. Das gehört zu M5.
- Gegner finden den Spieler weiter über den festen Namen in der Szene. Das gehört zu A1.
- Zauber haben keine Zauberzeit und keine Animation am Helden.
- Der Bogen ist ein Platzhalter mit gezeichnetem Icon und fällt bei Blue Blobs. Die Angriffsanimation bleibt die Einhand-Animation.
- Die Leiste wird weiter per Code platziert. Das gehört zu A6.
- Icons der Skills stammen aus dem vorhandenen Archiv unter `Textures/Spells/Archive/icons`.

#### Nachtrag vom 28.09.2026: Schadenswerte im Tooltip

Umgesetzt auf dem Branch `master_SkillTooltipDps`. Der Tooltip eines Skills zeigt fünf Werte:

| Zeile | Inhalt |
|---|---|
| DPS | Schaden pro Sekunde gegen ein Ziel |
| Average Hit | Mittlerer Treffer, Krit eingerechnet |
| Crit Chance | Krit-Chance des Skills in der Hand des Helden |
| Attacks oder Casts per Second | Einsätze pro Sekunde. Bei einer ATTACK ist das Angriffstempo die indirekte Abklingzeit, eine längere Abklingzeit des Skills bremst zusätzlich. |
| Cooldown | Nur, wenn der Skill eine Abklingzeit hat |

Die erste Fassung zeigte mehr, unter anderem kleinsten und größten Treffer, Trefferchance, Mana und DPS auf Dauer. Das war zu voll. Der Kern rechnet diese Werte weiter aus, angezeigt werden sie nicht.

- `SkillDamageEstimator` im Kern rechnet mit Erwartungswerten statt zu würfeln und benutzt dieselben Regeln wie die Trefferauflösung.
- Die gemeinsamen Regeln stehen jetzt an einer Stelle: `GetDamageFactor` und `GetHitChanceFactor` an der Schadensart, `GetCriticalFactor` und `GetHitChance` in `CombatFormulas`.
- Der Tooltip entsteht erst beim Anzeigen. Er zeigt deshalb immer die Werte der aktuellen Ausrüstung und der laufenden Statuseffekte.
- Alle Zahlen gelten für ein einzelnes Ziel ohne Verteidigung. Die Anzeige beschreibt den Helden, nicht einen bestimmten Gegner.

Die Formel:

```
Mittlerer Treffer    = (Min + Max) / 2 × (1 + Krit-Chance × Krit-Schaden)
Einsätze pro Sekunde = ATTACK: 1 / max(1 / Angriffstempo, Abklingzeit)
                       SPELL:  1 / max(Abklingzeit, 0,1 s)
Treffer pro Sekunde  = Einsätze pro Sekunde × (1 − Fehlschläge) × Trefferchance
DPS                  = Mittlerer Treffer × Treffer pro Sekunde + Schaden des Statuseffekts
```

Was außer den verlangten Werten in die DPS eingerechnet ist:

| Wert | Wirkung |
|---|---|
| Trefferchance | Stat des Helden, bei Pierce halbiert |
| Faktor der Schadensart | Crush verursacht 20 % mehr Schaden |
| Bleed | 50 % des mittleren Treffers über 4 Sekunden pro Treffer, ohne Obergrenze. Bis zum Nachtrag zu M2 wirkte nur die stärkste Blutung. |
| Burn | 25 % des mittleren Treffers über 4 Sekunden pro Treffer, höchstens 10 Brände gleichzeitig |
| Fehlschläge | Steht der Held unter Shock, schlagen 25 % der Einsätze fehl |
| Chill auf dem Helden | Senkt das Angriffstempo und damit die Einsätze pro Sekunde |

Mana zählt nicht zur DPS. Die Zahl gilt, solange das Mana reicht.

Geprüft: 36 neue Unit-Tests, insgesamt 271. Ein Test würfelt je Schadensart 200.000 Treffer und vergleicht den Mittelwert mit der Schätzung. Im laufenden Spiel ergab eine Minute Nahkampf mit dem Schwert 10,73 DPS, der Tooltip zeigte 10,6. Beide Zahlen stammen aus der Zeit, als nur die stärkste Blutung wirkte. Die Messung mit stapelndem Bleed steht im Nachtrag zu M2.

Bewusst vereinfacht:

- Treffer werden im Spiel auf ganze Zahlen gerundet, die Schätzung rechnet ohne Rundung.
- Forks und mehrere Ziele in einer Fläche zählen nicht, gerechnet wird ein Ziel.
- Die Zeit fürs Hinlaufen und die Flugzeit von Projektilen zählen nicht.

#### Nachtrag vom 28.09.2026: Kommentare im Code

Der Code aus M1 bis M3 war zu dicht kommentiert, oft stand über einer Methode nur ihr Name in anderen Worten.

- Von 311 Kommentarzeilen, die seit M1 dazugekommen waren, sind 47 geblieben.
- Geblieben sind Fallen ("Neue Werte nur am Ende anhängen"), Begründungen, Eigenheiten von Godot und Reihenfolgen, die eingehalten werden müssen.
- Erklärungen zu Aufbau, Einheiten und Regeln stehen jetzt hier: unter M1 "So funktioniert der Kern", unter M2 "So funktioniert die Pipeline" und unter M3 "So funktionieren Skills".
- Kommentare aus der Zeit vor M1 sind unverändert.

### M4: Items als Daten und Speichern (M, umgesetzt am 28.09.2026 auf `master_ItemsAndSaving`)

Ziel: Charakter und Fortschritt überleben einen Neustart.

- Erledigt: Item-Basen als Resource, Item-Instanzen als reine Daten. Die Klassen und Szenen pro Item sind entfallen. Behebt A4.
- Erledigt: Inventar-Modell getrennt von der Inventar-Oberfläche, mit Ganzzahl-Koordinaten. Behebt P5, P6 und A3 für das Inventar. F8 bleibt behoben.
- Erledigt: Aufgehobene Tränke landen automatisch auf vorhandenen Stapeln.
- Erledigt: Eine einzige Lootbag-Logik. Alle gewürfelten Items fallen. Behebt A7, F7 und F9 bleiben behoben.
- Erledigt: Keine doppelten Affixe, verschachtelte Loot-Tabellen. Beides liegt jetzt im Kern und ist getestet, F13 und F14 bleiben behoben.
- Erledigt: Schilde und Waffen als Quelle für Parry und Block. Affix für den abgefangenen Anteil beim Block (`BlockReduction`).
- Erledigt: Loot würfelt über die gemeinsame Zufallsquelle mit Seed. Behebt den Loot-Anteil von P7.
- Erledigt: Speichern und Laden von Charakter, Inventar, Ausrüstung und Fortschritt, dazu die Belegung der Skill-Leiste.
- Zusätzlich: Regel für Zweihandwaffen und Schild, mit Schalter für ein späteres Talent.
- Zusätzlich: Holzschild als erste Item-Basis für den Schildplatz, mit gezeichnetem Platzhalter-Icon.
- Zusätzlich: Der Charakterbogen zeigt Block und Parry, je gegen Attacks und gegen Spells.

Fertig, wenn ein Charakter mit Ausrüstung nach Neustart identisch geladen wird.

Stand des Fertig-Kriteriums: erfüllt. 143 neue Unit-Tests decken den Kern ab, insgesamt sind es 414. Eine Laufzeitprüfung mit 187 Schritten im Testlevel lief fünfmal hintereinander fehlerfrei. Sie spielt, speichert, beendet das Spiel, startet neu und vergleicht den geladenen Charakter mit dem gespeicherten, Wert für Wert. Eine zweite Prüfung mit 17 Schritten deckt die neuen Zeilen im Charakterbogen ab. Zusätzlich gab es eine Sichtprüfung mit Bildschirmfotos.

Getroffene Designentscheidungen vom 28.09.2026:

| Frage | Entscheidung |
|---|---|
| Wann wird gespeichert | Automatisch: beim Beenden und nach jeder Änderung an Items, Ausrüstung, Skill-Leiste, Attributen und XP, außerdem nach Level-up und Tod. Beim Start lädt der Charakter von selbst. |
| Block | Jeder Schild blockt. Zweihand-Stäbe blocken ebenfalls. |
| Parry | Einige Nahkampfwaffen parieren, unter anderem Schwerter und Dolche. Später kommen Schilde dazu, die auch parieren, zum Beispiel Buckler. |
| Konfiguration | Parry und Block sind Felder jeder Item-Basis und im Inspector einstellbar. Kein Item-Typ blockt oder pariert per Code. |
| Zweihandwaffen | Sperren den Schildplatz. Beim Anlegen wandert der Schild ins Inventar, ohne Platz dafür schlägt das Anlegen fehl. |
| Späteres Talent | Soll erlauben, Zweihandwaffen einhändig zu führen und dazu einen Schild zu tragen. Der Schalter dafür ist `CharacterItems.AllowsOffhandWithTwoHander`. |
| Anzeige | Der Charakterbogen zeigt Parry und Block. |

Von mir festgelegt, weil es sich aus dem Umbau ergab. Die Werte lassen sich in den Resources ändern:

| Punkt | Festlegung |
|---|---|
| Speicherort | Eine Datei `user://saves/character.json`, unter Windows `%APPDATA%\Godot\app_userdata\Hoellenspiralenspiel\saves`. Die Auswahl zwischen mehreren Charakteren kommt mit dem Hauptmenü in M7. |
| Was gespeichert wird | Level, XP, offene Attributpunkte, die fünf Attribute, Belegung der Leiste, Inventar mit Positionen, Ausrüstung. |
| Was nicht gespeichert wird | Leben, Mana, Position, Gegner und Beutel am Boden. Der Held startet am Startpunkt mit vollem Leben und Mana. |
| Item in der Hand | Steht als Item ohne Platz im Spielstand und landet beim Laden im Inventar. Passt es nicht, fällt es zu Boden. |
| Takt des Speicherns | Eine Sekunde nach der ersten Änderung. Mehrere Änderungen in dieser Zeit ergeben einen Schreibvorgang. |
| Kaputter Spielstand | Wird als `character.json.broken` zur Seite gelegt, das Spiel beginnt mit einem neuen Charakter. |
| Ablegen auf belegte Felder | Das Item in der Hand landet mit seiner linken oberen Ecke auf dem angeklickten Feld. Liegt dort genau ein Item, tauschen beide. Am Rand rückt das Item ins Raster. |
| Tauschen beim Anlegen | Das alte Item nimmt den Platz des neuen im Inventar ein, wenn es dort passt. |
| Trank bei vollem Inventar | Vorhandene Stapel füllen sich, der Rest bleibt im Beutel. |
| Werte für Parry und Block | Siehe Tabelle unten, alle geschätzt. |
| Affixe für Schilde | Der neue Affix für `BlockReduction`, dazu die beiden vorhandenen Rüstungs-Affixe. |
| Dolch | Neuer Waffentyp `Dagger` mit Schadensart Pierce. Eine Item-Basis dazu gibt es noch nicht. |
| Zeilen im Charakterbogen | Vier Zeilen unter Dodge: Block, Spell Block, Parry, Spell Parry. Die Werte sind wie in der Trefferauflösung auf 0 bis 100 % begrenzt. |
| Abstand im Charakterbogen | Der Abstand zwischen den Gruppen ist von 12 auf 7 Pixel gesunken. So passen die vier Zeilen ohne Rollbalken, und die Werteliste schließt weiter bündig mit dem Inventar ab. |

So funktionieren Items:

- Eine Item-Basis ist eine Resource unter `Resources/Items`. `ItemLibrary` lädt alle Resources aus diesem Ordner samt Unterordnern, eine neue Basis braucht dort keinen Eintrag.
- Es gibt drei Arten: `WeaponBaseResource`, `ArmorBaseResource` und `ConsumableBaseResource`. Ein Schild ist eine Rüstung im Platz `Offhand`.
- Der Kern kennt eine Basis als `ItemDefinition` und ein einzelnes Item als `ItemInstance`. Die Instanz hält Basis, Itemlevel, Affixe, Namen und Stapelgröße.
- Ein lokaler Affix verändert die Werte des Items selbst, zum Beispiel Schaden oder Rüstung. Ein globaler Affix geht beim Anlegen ins Stat-Blatt. Im Inspector heißt das Feld weiter `IsInherentMod`.
- `ItemInstance.GetEquipModifiers` liefert alles, was ein angelegtes Item dem Helden gibt: Rüstung, Parry, Block und die globalen Affixe.
- `CharacterItems` ist das Modell für alles, was der Held bei sich trägt: Inventar, Ausrüstung und das Item in der Hand. Jeder Klick in der Oberfläche ruft dort genau eine Methode auf.
- `InventoryGrid` rechnet mit ganzen Feldern. Gesucht wird Zeile für Zeile von links oben.
- Die Oberfläche hört auf `CharacterItems.Changed` und gleicht ihre Ansichten mit dem Modell ab. Sie enthält keine Regeln mehr.
- Der Held hört auf `Equipment.Equipped` und `Unequipped` und legt die Modifier ins Stat-Blatt oder nimmt sie heraus.
- `Lootbag.Drop` legt einen Beutel in die Welt. Gegner, Held und das Laden benutzen denselben Aufruf. Der Beutel hängt am Elternknoten des Helden.
- `LootRoller` und `AffixRoller` würfeln im Kern mit `GameRandom.Shared`. Derselbe Seed ergibt dieselbe Beute.
- Eine Item-Resource baut ihre Definition beim ersten Zugriff und behält sie. Wer Werte der Resource im laufenden Spiel ändert, sieht davon nichts.
- Die Herkunft der Modifier im Stat-Blatt ist die `InstanceId` des Items. Sie gilt nur für die laufende Sitzung und steht nicht im Spielstand.
- Neue Werte in den Enums `Requirement`, `WeaponType`, `ConsumableEffectKind` und `ItemSlot` nur am Ende anhängen, weil Resources sie als Zahl speichern.

So funktioniert das Speichern:

- `SaveGame` ist das Format, `SaveGameSerializer` schreibt und liest es als JSON, `SaveGameMapper` übersetzt zwischen Spiel und Format. Alle drei liegen im Kern und sind getestet.
- Enums stehen als Namen in der Datei. Ein Spielstand übersteht deshalb neue Enum-Werte.
- Items stehen mit der Id ihrer Basis im Spielstand. Gibt es die Basis nicht mehr, fehlt das Item nach dem Laden, und das Spiel meldet eine Warnung.
- `SaveGameStore` schreibt erst in eine zweite Datei und benennt sie dann um. Ein Absturz mitten im Schreiben zerstört den alten Spielstand nicht.
- `GameController` lädt nach dem Aufbau der Szene und beobachtet danach den Helden. Der Schalter `SavingEnabled` im Inspector schaltet Laden und Speichern ab.
- Ein Spielstand kann mehr XP enthalten, als das Level verlangt. Der Aufstieg folgt wie im Spiel erst mit dem nächsten XP-Gewinn.
- Die Datei trägt eine Versionsnummer. Spielstände einer neueren Version lehnt das Spiel ab.
- Mit einem anderen Spielstand starten: Godot mit `-- --save-file=user://saves/test.json` aufrufen.
- Neu anfangen: die Datei `character.json` löschen.

Eine neue Item-Basis in zwei Schritten:

1. Resource vom Typ `WeaponBaseResource`, `ArmorBaseResource` oder `ConsumableBaseResource` unter `Resources/Items` anlegen. Id, Name, Icon, Größe und Anforderungen eintragen, bei Bedarf Parry und Block.
2. Die Resource in eine Loot-Tabelle unter `Resources/LootTables` eintragen.

Vorläufige Werte der Item-Basen, alle in [Resources/Items](../Resources/Items):

| Item | Art | Größe | Werte | Parry und Block | Anforderung |
|---|---|---|---|---|---|
| Training Sword | Einhand, Slash | 1x3 | 4 bis 9, 1,4 Angriffe pro Sekunde, 5 % Krit | 5 % Parry | Stärke 2 |
| Wooden Staff | Zweihand, Crush | 1x4 | 10 bis 14, 0,33 Angriffe pro Sekunde, 3 % Krit | 10 % Block, 5 % Block gegen Spells | Intelligenz 1 |
| Short Bow | Zweihand, Pierce | 1x3 | 5 bis 11, 1,2 Angriffe pro Sekunde, 6 % Krit, Reichweite 700 | keine | Geschick 2 |
| Wooden Shield | Schild | 2x2 | 8 Rüstung | 15 % Block, 8 % Block gegen Spells | Stärke 2 |
| Gugel | Helm | 2x2 | 10 Rüstung | keine | Stärke 1 |
| Peasant Tunic | Torso | 2x3 | 25 Rüstung | keine | keine |
| Wool Gloves | Handschuhe | 2x2 | 10 Rüstung | keine | keine |
| Health Potion | Trank | 1x1 | 20 % Leben, Stapel bis 5 | | |
| Mana Potion | Trank | 1x1 | 20 % Mana, Stapel bis 5 | | |

Der neue Affix für Schilde:

| Stufe | Ab Itemlevel | Wert | Name |
|---|---|---|---|
| 4 | 1 | 3 bis 5 | of Bracing |
| 3 | 20 | 6 bis 9 | of Deflection |
| 2 | 45 | 10 bis 13 | of the Bulwark |
| 1 | 70 | 14 bis 18 | of the Bastion |

Der Wert erhöht den abgefangenen Anteil beim Block, der Grundwert ist 50 %.

Bewusst offen gelassen:

- Der abgefangene Anteil beim Block steht nicht im Charakterbogen. Den Grundwert von 50 % nennt die Roadmap, den Affix der Tooltip des Schilds.
- Die Werteliste im Charakterbogen ist voll. Eine weitere Zeile braucht mehr Höhe oder eine kleinere Schrift.
- Itemlevel ist weiter 1. Das Monsterlevel bestimmt es ab M5. `Lootsystem.ItemLevel` ist bis dahin die Stellschraube.
- Rare und Elite würfeln weiter mit eigenen Zufallsquellen. Das gehört zu M5.
- Ringe haben vier einzelne Plätze, aber noch keine Item-Basen. Welcher Ring in welchen Platz geht, ist offen.
- Waffen für die Nebenhand gibt es nicht. Die Wield-Strategien `OffHand` und `OneHand` stehen nur im Tooltip.
- Es gibt einen Charakter. Mehrere Charaktere und ein neues Spiel kommen mit dem Hauptmenü in M7.
- Der Tooltip sucht weiter über einen festen Namen in der Szene. Das gehört zu A1.
- Das Item in der Hand ist wie bisher nur blass zu sehen.

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
- Balance-Durchgang für Leben, Schaden, XP und Loot. Dazu gehören B1 bis B8 und die Stellschrauben aus M2.
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
