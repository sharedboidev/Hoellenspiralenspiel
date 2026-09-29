# Höllenspiralenspiel: Analyse und Roadmap

Stand: 29.09.2026. M0 bis M6 liegen auf `master`, dazu die Nachträge zu M2, M3, M5.5 und M6: Bleed stapelt, Schadenswerte im Tooltip, ausgedünnte Kommentare, die Rückmeldungen aus dem ersten Spielen und die Rückmeldungen zu Mauern und Räumen.
Das Spiel läuft seit M5.5 in 3D im Look der PlayStation 1.
Die 2D-Fassung ist abgelöst: Ihr Code und ihre Szenen sind entfallen, die Hauptszene ist das 3D-Testlevel.
Der Feature-Umfang für Leser steht in der [README](../README.md), dieses Dokument enthält Analyse, Befunde und Plan.
Grundlage: Designdokument "Wyldes Gehirnsturmscribble" und der komplette C#-Code samt Szenen. Die Zeilenzahl aus der ersten Analyse, rund 5.500, galt für die 2D-Fassung.
Die Befunde stammen aus Code-Lektüre. Die als behoben markierten Fehler, F19 und die Meilensteine M2 bis M6 wurden zusätzlich im laufenden Spiel geprüft, headless mit Godot 4.6.

## Getroffene Richtungsentscheidungen

| Frage | Entscheidung |
|---|---|
| 2D oder 3D | 3D im Look der PlayStation 1, Vorbild Silent Hill. Entschieden am 29.09.2026 nach dem Vergleich. |
| Sichtbare Ausrüstung | Ja, man soll Ausrüstung am Helden sehen, auch das Amulett. Nur die Ringe bleiben unsichtbar. Gegner zeigen ihre Ausrüstung, sobald es Gegner mit Armen gibt. Entschieden am 29.09.2026. |
| Modelle | Starre Teile ohne Skelett. Entschieden am 29.09.2026. |
| Spielstruktur | Hub (Stadt) plus Abstieg in einen Höllenkreis mit mehreren Ebenen |
| Leveldesign | Etwa 90 % prozedural, dazu handgebaute Räume und Event-Locations, die gezielt eingestreut werden |
| Multiplayer | Singleplayer zuerst, Koop soll später nachrüstbar bleiben |
| Kamera | Perspektivisch. Entschieden am 29.09.2026 vor M6. |
| Grundriss | Handgebaute Räume auf einem Raster, der Generator zieht Gänge dazwischen. Entschieden am 29.09.2026. |
| Wegführung | Verzweigt mit Schleifen: mehrere Wege und Rundläufe, der Ausgang muss gesucht werden. Entschieden am 29.09.2026. |
| Mauern | Jede Mauer hat einen Sockel und Mauerwerk darüber. Steht sie zwischen Held und Kamera, wird das Mauerwerk im Lichtradius durchsichtig. Entschieden am 29.09.2026. |
| Räume | Ein Raum, in dem der Held nicht steht, bleibt verschlossen und dunkel. Licht scheint nicht durch Mauern, Bewegung im Raum zeigt sich erst mit Sichtkontakt. Entschieden am 29.09.2026. |

## 1. Was schon umgesetzt ist

| Bereich | Stand | Abgleich mit dem PDF |
|---|---|---|
| Isometrische Perspektive | Testlevel in 3D mit Boden, Mauern und ummauertem Hof, gesehen von schräg oben. Seit M6 führt die Kellertür in erzeugte Ebenen. | Entspricht dem PDF |
| Attribute | Alle fünf Attribute mit abgeleiteten Werten und Obergrenzen | Obergrenzen stimmen exakt. Wachstum ist logistisch statt beschränkt. Lichtradius fehlt. |
| Leben und Mana | Basisformeln, Regeneration, Orbs mit Shader | Formeln stimmen exakt |
| Modifier-System | Flat, Percentage (increased) und More, sauber getrennt | Trägt das ganze Stat-Konzept |
| Waffen | Schadensspanne, Angriffe pro Sekunde, Swingtimer, Typ, Schadensart, Krit, Anforderungen | Seit M2 bestimmen die Werte den Nahkampf, seit M3 gibt es Fernkampfwaffen mit Projektil. Seit M4 sind Waffen Resources und bringen Parry oder Block mit. Klassen-Anforderung fehlt. |
| Rüstung | Helm, Torso, Handschuhe und Schild mit Rüstungswert | 4 von 16 Slots haben Item-Basen |
| Affixe | 17 Affixe mit Tiers, Gewichten, Itemlevel-Grenze, Prefix/Suffix, lokale und globale Mods | Slot-Tabelle aus dem PDF nur teilweise abgedeckt |
| Loot | Gewichtete Loot-Tabellen, Lootbags, Magic/Rare-Namen. Seit dem Nachtrag zu M5.5 liegt Beute in einem Gitter und trägt Schilder mit dem Namen des Items. | Seit M4 würfelt der Kern mit der gemeinsamen Zufallsquelle. Seit M5 bestimmt das Monsterlevel das Itemlevel. |
| Inventar | Tetris-Inventar, Drag-and-drop, Tauschen, Stapeln, Tooltips | Nicht im PDF. Seit M4 ein Modell im Kern, die Oberfläche zeigt nur an. Seit M5.5 hängt sie an der Schnittstelle `IHero` und läuft auch in 3D. |
| Ausrüstung | 16 Slots inklusive 4 Ringe, Anforderungsprüfung | Entspricht dem PDF. Seit M4 sperrt eine Zweihandwaffe den Schildplatz. Seit M5.5 ist sie in 3D am Helden zu sehen. |
| Speichern | Seit M4: Charakter, Inventar, Ausrüstung und Skill-Leiste, automatisch. Seit M6 auch der Abstieg mit Seed, Tiefe und erkundeter Karte. | Nicht im PDF |
| Leveling | XP-Tabelle bis Level 100, Level-up-Effekt, Attributspunkte, XP-Balken. Seit M5.5 liegt die Regel für XP, Level und Punkte im Kern und treibt den 3D-Helden. | Nicht im PDF, funktioniert |
| Skills | Seit M3 als Daten: Attack, Lightning Strike, Fireball mit Fork, Frost Nova, Thunderbolt. Leiste mit zehn frei belegbaren Plätzen, Tooltip mit DPS | Das PDF kennt keine Skill-Arten. Klassen und Skill-Erwerb sind offen. |
| Kampf | Zentrale Trefferauflösung, Nahkampf, Schadensarten mit Effekten, Statuseffekte, Tod und Respawn | Seit M2. Frost-Effekt war im PDF leer und ist jetzt Verlangsamung. |
| Schadensminderung | Rüstungsformel, Resistenzen, Dodge, Parry und Block für alle Einheiten | Seit M4 bringen Schild, Stab und Schwert Block und Parry mit |
| Gegner | 3 Typen, Spawn-Marker, Gruppen-Aggro, Lebensbalken, Schadenszahlen, eigene Angriffe. Seit M3 setzen sie Skills auf demselben Weg ein wie der Spieler. Seit M5 sind sie Resources mit Level, Ausrüstung und Verhalten, dazu Elite und Rare Elite mit Mods aus Bausteinen. Seit dem Nachtrag zu M5.5 kollidieren sie miteinander und spawnen verstreut. | Nur Blobs und ein Testgegner |
| Wegfindung | Seit M5: Navigationsnetz pro Level, zur Laufzeit gebacken. Gegner und Held laufen um Wände herum | Nicht im PDF |
| Atmosphäre | Der Held trägt sein Licht, die Umgebung ist dunkel und neblig, dazu der PS1-Look und die Overlay-Karte. Seit M6 zeigt unter der Erde eine gezeichnete Karte, was der Held erkundet hat. | Passt zum düsteren Vibe |
| 3D | Seit dem 29.09.2026 läuft das ganze Spiel in 3D auf dem unveränderten Kern. Alle Modelle sind Platzhalter aus Grundkörpern. | Offene Frage aus dem PDF, entschieden für 3D |
| Level | Seit M6: Ebenen aus handgebauten Räumen und erzeugten Gängen, gesteuert über einen Seed. Ein Thema legt Räume, Aussehen und Gegnerpool fest. | Das PDF nennt nur die Kreise. Es gibt ein Testthema, noch keinen Höllenkreis. |

## 2. Was an kritischen Systemen fehlt

Ohne diese Systeme gibt es kein spielbares Spiel, nur eine Testszene.

1. **Spielertod.** Erledigt in M2. Vorher konnte das Leben unter null fallen, ohne dass etwas passierte.
2. **Nahkampf.** Erledigt in M2. Vorher existierten Waffenwerte nur im Tooltip.
3. **Gegnerangriffe.** Erledigt in M2. Vorher schadeten die Blobs nur durch Berührung.
4. **Speichern und Laden.** Erledigt in M4. Vorher gab es keine Persistenz für Charakter, Inventar oder Fortschritt.
5. **Spielstruktur.** Hub, Hauptmenü, Pausenmenü und Freischaltung fehlen. Seit M6 führt die Kellertür hinab, und der Ausgang jeder Ebene führt eine Ebene tiefer. Einen Weg zurück gibt es noch nicht.
6. **Levelgenerierung.** Erledigt in M6. Vorher gab es nur das handgebaute Testlevel.
7. **Wegfindung.** Erledigt in M5. Vorher liefen Gegner in gerader Linie und blieben an Wänden hängen.
8. **Statuseffekte.** Erledigt in M2: Bleed, Burn, Shock und Chill. Bleed und Burn stapeln. Die leeren Schadensart-Klassen sind durch eine Aufzählung im Kern ersetzt.
9. **Parry, Block, Krit aus Stats.** Erledigt in M2. Die Trefferauflösung wertet alle drei aus.
10. **Skill-Erwerb.** Seit M3 sind Skills Daten und die Leiste ist frei belegbar. Der Held kennt vorerst alle Skills. Klassen und Skill-Fortschritt fehlen.
11. **Skalierung.** Seit M5 hat jede Karte ein Bereichslevel, jedes Monster ein Level, und das Monsterlevel bestimmt das Itemlevel. Das Testlevel hat Bereichslevel 1. Seit M6 steigt das Bereichslevel mit jeder Ebene um 1.
12. **Inhalt.** Kein einziger Höllenkreis, kein Boss, kein Intro.
13. **Einstellungen.** Auflösung, Tastenbelegung und Lautstärke sind nicht einstellbar.
14. **Tests.** Vom Testprojekt existiert nur ein `obj`-Ordner ohne Quellcode.

## 3. Was schlecht oder imperformant umgesetzt ist

### 3.1 Fehler

Status vom 28.09.2026. Die Zeilennummern beziehen sich auf den Stand vor den Korrekturen.

| Nr. | Status | Problem | Stelle |
|---|---|---|---|
| F1 | Behoben | Stärke und Konstitution überschreiben gegenseitig ihren Rüstungsbonus, weil beide dieselbe Herkunfts-ID benutzen | [DerivedStatProvider.cs:65](../Scripts/Core/Stats/DerivedStatProvider.cs), [DerivedStatProvider.cs:98](../Scripts/Core/Stats/DerivedStatProvider.cs) |
| F2 | Behoben | Attribute von Ausrüstung aktualisieren die abgeleiteten Werte nicht. Nur Änderungen am Basiswert lösen die Neuberechnung aus. | [BaseUnit.cs:155](../Scripts/Units/BaseUnit.cs) |
| F3 | Behoben | Zauberschaden ignoriert den More-Multiplikator. Intelligenz erhöht den Schaden dadurch nicht. | [BaseSkill.cs:42](../Scripts/Abilities/BaseSkill.cs) |
| F4 | Behoben | Treffer auf Gegner zeigen manchmal "Dodge" an, ziehen aber trotzdem Leben ab. Heilung konnte ebenfalls "Dodge" anzeigen. | [BaseUnit.cs:48](../Scripts/Units/BaseUnit.cs), [FCTExtensions.cs:17](../Scripts/UI/CombatText.cs) |
| F5 | Behoben | Kontaktschaden trifft in jedem Physik-Frame ohne Abklingzeit | [Player2D.cs:199](../Scripts/Units/Hero.cs) |
| F6 | Behoben | Gegner greifen ohne Cooldown an. Der Testgegner erzeugt pro Frame einen Feuerball. Windup und Recovery werden nicht benutzt. | BaseEnemy.cs:145, heute [Enemy.cs](../Scripts/Units/Enemies/Enemy.cs). TestEnemy.cs:21, seit M5 entfallen |
| F7 | Behoben | Ein Gegner kann mehrere Lootbags fallen lassen, wenn er nach dem Tod noch getroffen wird. Von mehreren gewürfelten Items fällt nur das erste. | [EnemyController.cs:116](../Scripts/Controllers/EnemyController.cs) |
| F8 | Behoben | Einen Trank zu trinken gibt den Slot frei, obwohl der Stapel noch Tränke enthält. Das nächste Item landet darüber. | [Inventory.cs:192](../Scripts/UI/Character/Inventory.cs) |
| F9 | Behoben | Ein fallengelassenes Item geht verloren, wenn man es bei vollem Inventar wieder aufhebt | [MouseObject.cs:72](../Scripts/UI/MouseObject.cs) |
| F10 | Behoben | Ausrüsten per Drag-and-drop umgeht die Anforderungsprüfung | [EquipmentSlot.cs:129](../Scripts/UI/Character/EquipmentSlot.cs) |
| F11 | Behoben | Das Entfernen von Modifikatoren fasst nebenbei wertgleiche Einträge zusammen. Zwei identische Affixe auf einem Item zählen danach nur noch einmal. | [BaseUnit.cs:123](../Scripts/Units/BaseUnit.cs) |
| F12 | Behoben | Der Affix-Wurf kann `null` liefern, wenn ein Slot keine passenden Affixe hat. Das führt zum Absturz. | [Lootsystem.cs:181](../Scripts/Controllers/Lootsystem.cs) |
| F13 | Behoben | Verschachtelte Loot-Tabellen sind als Typ angelegt, aber nicht umgesetzt | [LootTable.cs:42](../Resources/LootTable.cs) |
| F14 | Behoben | Derselbe Affix kann mehrfach auf einem Item landen | [Lootsystem.cs:57](../Scripts/Controllers/Lootsystem.cs) |
| F15 | Behoben | Die Todesanimation ist nie zu sehen, weil der Gegner beim Start der Animation entfernt wird | BaseEnemy.cs:60, heute [Enemy.cs](../Scripts/Units/Enemies/Enemy.cs) |
| F16 | Behoben | Auf Level 100 ist die nächste XP-Schwelle 0. Jeder XP-Gewinn löst dann ein Level-up aus und das Level danach wirft eine Ausnahme. | [XpTable.cs:109](../Scripts/Core/Progression/XpTable.cs) |
| F17 | Behoben | `LifeBase = 75` in den Gegner-Szenen wird ignoriert. Alle Gegner haben 9 Leben, der Feuerball macht 50 bis 75 Schaden. | [yellow_blob.tscn](../Scenes/Units/Enemies/yellow_blob.tscn) |
| F18 | Behoben | Schadenszahlen driften pro Frame statt pro Sekunde und sind damit abhängig von der Bildrate | [FloatingCombatText.cs:58](../Scripts/UI/FloatingCombatText.cs) |
| F19 | Behoben | Ein Slot meldet sich nie vom Stapel-Ereignis eines Tranks ab. Wird ein verschobener Trankstapel leer getrunken, löscht der alte Slot den fremden Trankstapel, der inzwischen dort liegt. | [InventorySlot.cs:62](../Scripts/UI/Character/InventorySlot.cs) |
| F20 | Behoben | Einheiten füllen Leben und Mana, bevor die abgeleiteten Modifier berechnet sind. Sie starten dadurch knapp unter ihrem Maximum. | [BaseUnit.cs:84](../Scripts/Units/BaseUnit.cs), [Player2D.cs:76](../Scripts/Units/Hero.cs) |
| F21 | Behoben | Das Inventar hat einen höheren Z-Index als der Level-up-Dialog und verdeckt ihn | [level_up_dialog.tscn](../Scenes/UI/level_up_dialog.tscn) |
| F22 | Behoben | Das Statdisplay zeichnet sich nach dem Verteilen eines Attributpunkts nicht neu | [CharacterSheet.cs](../Scripts/UI/Character/CharacterSheet.cs), [BaseUnit.cs](../Scripts/Units/BaseUnit.cs) |

Hinweise zu den Korrekturen:

- F2 war pragmatisch behoben und ist seit M1 durch den Stat-Kern ersetzt.
- F4, F5 und F6 waren pragmatisch behoben und sind seit M2 durch die Kampf-Pipeline ersetzt. Der Kontaktschaden aus F5 ist entfallen, Gegner greifen jetzt selbst an.
- F17 führt den exportierten Wert `LifeBaseBonus` ein. Er wird auf das Basisleben aus den Attributen addiert. Seit M5 steht er als `LifeBonus` in der Gegner-Resource.
- F18 ändert die Einheit von `DriftVelocity` auf Pixel pro Sekunde.

### 3.2 Performance

| Nr. | Problem | Stelle |
|---|---|---|
| P1 | Behoben in M1. Jeder Stat-Zugriff durchsuchte die Modifikator-Liste mehrfach mit LINQ. Das passierte pro Einheit und pro Frame mehrfach und erzeugte laufend Müll für den Garbage Collector. | [StatSheet.cs](../Scripts/Core/Stats/StatSheet.cs) |
| P2 | Behoben in M1. Die Orbs bauten jeden Frame Text neu und setzten Shader-Parameter, auch wenn sich nichts änderte. | [ResourceOrb.cs](../Scripts/UI/Character/ResourceOrb.cs) |
| P3 | Behoben in M5. Alle Gegner der Karte wurden jeden Frame simuliert, egal wie weit sie entfernt waren. Jetzt ruhen Denken, Bewegung und Animation fern vom Helden. Die Messung unter M5 zeigt: Bei 200 Gegnern war das kein Engpass. | [EnemyController.cs](../Scripts/Controllers/EnemyController.cs) |
| P4 | Behoben in M3. Ein Feuerball konnte sich auf bis zu 63 Projektile aufspalten, und jeder Treffer sortierte alle Gegner der Karte nach Entfernung. Jetzt sind es höchstens 7, die Zielsuche läuft in einem Durchlauf ohne Sortieren. | [SkillProjectile.cs](../Scripts/Skills/Effects/SkillProjectile.cs), [NearestPicker.cs](../Scripts/Core/Skills/NearestPicker.cs) |
| P5 | Behoben in M4. Das Inventar nutzte Godot-Dictionaries mit Float-Vektoren als Schlüssel. Jeder Zugriff wurde zwischen C# und Engine konvertiert. Jetzt rechnet ein Raster im Kern mit ganzen Feldern. | [InventoryGrid.cs](../Scripts/Core/Items/InventoryGrid.cs) |
| P6 | Behoben in M4. Knoten wurden in Property-Gettern bei jedem Zugriff neu gesucht. Inventar und Ausrüstung holen ihre Knoten jetzt einmal. | [Inventory.cs](../Scripts/UI/Character/Inventory.cs), [EquipmentPanel.cs](../Scripts/UI/Character/EquipmentPanel.cs) |
| P7 | Behoben: für den Kampf in M2, für Loot in M4, für Spawns und Seltenheit in M5. Alle Würfe laufen über eine Zufallsquelle mit Seed. | [GameRandom.cs](../Scripts/Core/Rng/GameRandom.cs), [LootRoller.cs](../Scripts/Core/Items/LootRoller.cs), [EnemyController.cs](../Scripts/Controllers/EnemyController.cs) |
| P8 | Neu seit M2: Jede Schadenszahl ist ein eigener Knoten. Brennen viele Gegner gleichzeitig, entstehen pro Sekunde zwei Zahlen je Gegner. Bisher ohne messbare Folgen, Pooling steht in M9. | [FCTExtensions.cs](../Scripts/UI/CombatText.cs) |
| P9 | Behoben in M5. Flächen, Forks und die Suche nach dem Gegner unter dem Mauszeiger gingen alle Einheiten der Karte durch. Jetzt fragen sie ein Raster und sehen nur die Einheiten in der Nähe. | [SpatialHash.cs](../Scripts/Core/Spatial/SpatialHash.cs), [UnitRegistry.cs](../Scripts/Units/UnitRegistry.cs) |

### 3.3 Architektur

| Nr. | Problem | Folge |
|---|---|---|
| A1 | Behoben in M5.5 und M6. Vorher suchten rund 15 Stellen Spieler, Controller oder Tooltip über feste Namen in der aktuellen Szene. Seit M5.5 kommen Held, Controller und Gegner-Container als Felder aus dem Inspector. Seit M6 ist eine Ebene nur noch Inhalt, der unter den Knoten `Environment` gebaut wird. Held, Oberfläche und Controller bleiben dieselben. Tooltip, Dialoge und Todesbildschirm finden sich weiter über ihren Namen, das schränkt Level nicht mehr ein. | Jedes neue Level musste exakt wie das Testlevel aufgebaut sein |
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

Beobachtungen aus den Laufzeitprüfungen von M2 bis M5. Der Balance-Durchgang steht in M8.

| Nr. | Beobachtung | Stelle |
|---|---|---|
| B1 | Erledigt in M5. Rare und Elite bekamen 25 Stärke und regenerierten dadurch 5 Leben pro Sekunde. Seit M5 kommt die Stärke von Elite und Rare Elite allein aus ihren Mods. | [Resources/MonsterMods/Pool](../Resources/MonsterMods/Pool) |
| B2 | Der Spieler startet mit 9 Leben. Im Testlevel hat er 50 Bonusleben bekommen, damit ein Kampf länger als zwei Treffer dauert. | [test_level.tscn](../Scenes/test_level.tscn) |
| B3 | Jeder Treffer mit Fire, Frost, Lightning oder Slash löst seinen Effekt sicher aus. Eine Chance statt Gewissheit wäre eine Stellschraube. | [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs) |
| B4 | Der Held startet mit 9 Mana und regeneriert 0,5 pro Sekunde. Das reicht für vier Feuerbälle oder zwei Thunderbolts. | [Resources/Skills/Player](../Resources/Skills/Player) |
| B5 | Die Schadenswerte der Zauber stammen von den alten Testzaubern. Ein Thunderbolt mit 50 bis 350 tötet jeden Gegner des Testlevels mit einem Treffer. | [thunderbolt.tres](../Resources/Skills/Player/thunderbolt.tres) |
| B6 | Pierce trifft nur halb so oft. Mit dem Bogen geht deshalb jeder zweite Pfeil daneben, obwohl er sichtbar durch den Gegner fliegt. | [CombatRules.cs](../Scripts/Core/Combat/CombatRules.cs) |
| B7 | Neu seit M4: Die Werte für Parry und Block der ersten Items sind geschätzt. Awareness verstärkt Block und Parry gegen Attacks, nicht gegen Spells. | [Resources/Items](../Resources/Items), [DerivedStatProvider.cs](../Scripts/Core/Stats/DerivedStatProvider.cs) |
| B8 | Neu seit dem 28.09.2026: Bleed stapelt ohne Obergrenze und legt auf Dauer 50 % des Trefferschadens obendrauf. Burn bringt 25 % und endet bei 10 Stapeln. Eine Obergrenze für Bleed ist die Stellschraube. | [StatusEffectRules.cs](../Scripts/Core/Combat/StatusEffects/StatusEffectRules.cs) |
| B9 | Neu seit M5: Attribute wirken bei kleinen Werten kaum. Ein Blob auf Level 10 hat Stärke 10 statt 1 und schlägt damit nur 2,5 % härter zu. Sein Leben steigt dagegen von 9 auf rund 45, und er regeneriert 5 Leben pro Sekunde. Den Schaden hoher Level müssen Ausrüstung und Mods tragen. | [DerivedStatProvider.cs](../Scripts/Core/Stats/DerivedStatProvider.cs), [Resources/Enemies](../Resources/Enemies) |
| B10 | Neu seit M5: Natürliche Waffen mit Frost, Fire oder Lightning wachsen mit keinem Attribut. Stärke verstärkt nur physischen Schaden, Intelligenz nur Spells. | [HitRequests.cs](../Scripts/Core/Combat/HitRequests.cs) |
| B11 | Neu seit M5: Chancen für Elite und Rare Elite, alle Werte der Mods und der Schaden von Meteor, Death Blast und Frost Pulse sind geschätzt. Der Schaden der drei Skills wächst nicht mit dem Level. | [EnemyController.cs](../Scripts/Controllers/EnemyController.cs), [Resources/MonsterMods/Pool](../Resources/MonsterMods/Pool) |
| B13 | Erledigt am 29.09.2026. Reichweiten zählen seit dem Nachtrag zu M5.5 ab dem Rand des Körpers, die Werte stammten aus der Zeit der Körpermitten. Der Held traf unbewaffnet mit 0,95 m Luft zum Gegner, ein Blob mit 0,79 m. Jetzt reicht der unbewaffnete Held 40 Pixel weit und ein Blob 30. Waffen behalten ihre Werte, das Übungsschwert reicht weiter 100 Pixel. | [WeaponProfile.cs](../Scripts/Core/Combat/WeaponProfile.cs), [Resources/Enemies](../Resources/Enemies) |
| B12 | Neu seit M5: Ein Blue Blob läuft 25 Pixel pro Sekunde und gibt auf, sobald der Held 6 Sekunden lang außerhalb des Aggroradius bleibt. Aus der Ferne getroffen, kommt er deshalb nur 150 Pixel weit. | [blue_blob.tres](../Resources/Enemies/blue_blob.tres) |

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
- `Player.cs` und `test_plane_3d.tscn` sind der 3D-Prototyp und bleiben bis zur Entscheidung 2D oder 3D. Mit M5.5 sind beide entfallen.
- `thunder_shader.tres` wird von keiner Szene benutzt und liegt seit M5.5 unter `Shaders/Archive2D`. Dasselbe gilt für ungenutzten Code im `EnemyController` rund um den Spawn-Timer.

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
- Abstände im Kampf wurden zwischen den Körpermitten gemessen. Seit dem Nachtrag zu M5.5 zählen sie vom Rand des Körpers bis zum Rand des Ziels.
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
- Der Held lief in gerader Linie zum Ziel. Seit M5 folgt er dem Pfad um Wände herum. Bleibt er trotzdem hängen, gibt er das Ziel nach 0,4 Sekunden auf.
- Die Angriffsanimation des Spielers benutzt das vorhandene graue Platzhalter-Sprite und spielt für alle Waffen die Einhand-Animation.
- Ein fehlgeschlagener Zauber wurde im Skillbar-Button behandelt. Seit M3 regelt das der Spieler.
- Gegner fanden den Spieler über den festen Namen in der Szene. Seit M5 bekommen sie ihr Ziel vom `EnemyController`.

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
- Gegner zahlen für Skills kein Mana, ihr `AvailableMana` ist unbegrenzt. Seit M5 gilt für sie die Abklingzeit des Skills.

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
- Gegner zahlten für Skills weder Mana noch Abklingzeit. Seit M5 gilt die Abklingzeit, Mana bleibt frei. Ihr Takt kommt weiter aus Windup und Recovery.
- Gegner fanden den Spieler über den festen Namen in der Szene. Seit M5 bekommen sie ihr Ziel vom `EnemyController`.
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
- Itemlevel war immer 1. Seit M5 bestimmt es das Monsterlevel, `Lootsystem.ItemLevel` ist entfallen.
- Rare und Elite würfelten mit eigenen Zufallsquellen. Seit M5 laufen Spawns und Seltenheit über die gemeinsame Zufallsquelle.
- Ringe haben vier einzelne Plätze, aber noch keine Item-Basen. Welcher Ring in welchen Platz geht, ist offen.
- Waffen für die Nebenhand gibt es nicht. Die Wield-Strategien `OffHand` und `OneHand` stehen nur im Tooltip.
- Es gibt einen Charakter. Mehrere Charaktere und ein neues Spiel kommen mit dem Hauptmenü in M7.
- Der Tooltip sucht weiter über einen festen Namen in der Szene. Das gehört zu A1.
- Das Item in der Hand ist wie bisher nur blass zu sehen.

### M5: Gegner-KI und Skalierung (M, umgesetzt am 28.09.2026 auf `master_EnemyAiAndScaling`)

Ziel: Gegner, die sich durch Level bewegen und mit der Tiefe stärker werden.

- Erledigt: Gegner-Definition als Resource mit Attributen, Wachstum pro Level, Ausrüstung, natürlicher Waffe, Skills, Beute, XP und Verhalten. Die Klassen pro Gegner sind entfallen, die Szene bringt nur noch Aussehen und Kollisionsform mit. F17 bleibt behoben.
- Erledigt: Zustandsmaschine im Kern: Ruhe, Verfolgen, Ausholen, Erholen, Rückweg, Tod. F15 bleibt behoben.
- Erledigt: Wegfindung über Godots Navigation. Auch der Held benutzt sie, wenn er zu einem angeklickten Gegner läuft.
- Erledigt: Gegner geben die Verfolgung auf und gehen langsam in die Nähe ihres Startorts zurück.
- Erledigt: Bereichslevel pro Karte, Anpassung pro Gegner und pro Spawn-Marker. Das Monsterlevel bestimmt das Itemlevel.
- Erledigt: Elite und Rare Elite mit 1 bis 5 Mods. Ein Mod besteht aus Werten, Auslösern und Aktionen, die sich im Inspector frei kombinieren lassen.
- Erledigt: Skills von Gegnern haben Abklingzeiten. Ein Gegner kann mehrere Skills haben.
- Erledigt: Spawns und Seltenheit würfeln über die gemeinsame Zufallsquelle mit Seed. Behebt den Rest von P7.
- Erledigt: Ruhende Gegner fern vom Helden denken und bewegen sich nicht. Behebt P3.
- Erledigt: Flächen, Forks und die Suche unter dem Mauszeiger fragen ein Raster statt alle Einheiten der Karte. Behebt P9.
- Zusätzlich: Stat `ProjectileCount`. Wer ihn erhöht, schießt mehrere Projektile als Fächer. Er gilt für jede Einheit, auch für den Helden.
- Zusätzlich: Jede Einheit meldet erlittene und ausgeteilte Treffer über `DamageTaken` und `HitDealt`, mit Angreifer und Opfer.
- Zusätzlich: Schützen greifen nur an, wenn keine Wand zwischen ihnen und dem Ziel steht.
- Zusätzlich: Namensschild über Elite und Rare Elite mit Namen und Mods.
- Zusätzlich: Der Lebensbalken eines Gegners verschwindet wieder, wenn sein Leben voll ist.

Fertig, wenn Gegner um Wände herum laufen und 200 Gegner auf der Karte die Bildrate nicht senken.

Stand des Fertig-Kriteriums: erfüllt. 122 neue Unit-Tests decken den Kern ab, insgesamt sind es 547. Eine Laufzeitprüfung mit 105 Schritten im Testlevel lief sechsmal hintereinander fehlerfrei. In ihr läuft ein Gegner aus dem ummauerten Hof um die Wand herum zum Helden, und der Held findet denselben Weg zu einem Gegner im Hof. Eine zweite Prüfung mit 5 Schritten deckt die Sichtlinie der Schützen ab. Zusätzlich gab es eine Sichtprüfung mit Bildschirmfotos und eine Messung mit 200 Gegnern, siehe unten.

Getroffene Designentscheidungen vom 28.09.2026:

| Frage | Entscheidung |
|---|---|
| Monsterlevel | Bereichslevel pro Karte. Monster bekommen eine Property für individuelle Anpassungen. |
| Skalierung | Alles über Attribute, Ausrüstung und Monster-Mods. Keine eigene Kurve für Leben oder Schaden. |
| Mods | Sollen einfach und verrückt sein können. Beispiele: 33 % increased Attack Speed, doppelte Projektile, Meteore im Umkreis des Monsters. |
| Anzahl der Mods | 1 bis 5. Mit 1 bis 2 Mods heißt das Monster Elite, mit 3 bis 5 Mods Rare Elite. |
| Kosten für Gegner | Skills haben Abklingzeiten, kosten aber vorerst kein Mana. |
| Verfolgen | Gegner geben nach einer Zeit auf und gehen langsam zurück, nicht genau zum Startort, nur in die Nähe. Verfolgungszeit und Aggroradius sind im Inspector einstellbar. |
| Entfernte Gegner | Statuseffekte und Regeneration laufen normal weiter. |
| Wegfindung | Über Godots Navigation statt über ein eigenes Gitter im Kern. |

Von mir festgelegt, weil es sich aus dem Umbau ergab. Alles lässt sich in den Resources oder im Inspector ändern:

| Punkt | Festlegung |
|---|---|
| Chancen | Elite 10 %, Rare Elite 4 %. Innerhalb der Stufe ist jede Anzahl von Mods gleich wahrscheinlich. |
| Größe, XP und Beute | Elite: 1,25-fache Größe, 1,5-fache XP, Beute zweimal gewürfelt. Rare Elite: 1,5-fache Größe, 3-fache XP, Beute dreimal gewürfelt. |
| Namensschild | Name in Blau für Elite und in Gold für Rare Elite, darunter die Mods. |
| Verfolgungszeit | 6 Sekunden. Sie läuft nur, solange das Ziel außerhalb des Aggroradius ist. Jeder Treffer auf den Gegner setzt sie zurück. |
| Rückweg | Halbes Tempo, Ziel ist ein zufälliger Punkt bis 150 Pixel um den Startort. Der Gegner heilt dabei nicht von selbst. |
| Wecken auf dem Rückweg | Ein Treffer oder ein Ziel im Aggroradius lässt ihn wieder angreifen. |
| Tod des Helden | Gegner geben auf und gehen zurück. |
| Gruppe | Treffer und Tod rufen die Gruppe, bloße Nähe nicht. So war es vor M5 auch. |
| Wachstum der Attribute | Steht pro Attribut in der Gegner-Resource. Die Blobs bekommen pro Level 1 Stärke und 1 Konstitution, der Testgegner 1 Intelligenz und 1 Konstitution. |
| Ausrüstung | Item-Basen ohne Affixe. Die erste Waffe der Liste ersetzt die natürliche Waffe. Den Takt bestimmen weiter Windup und Recovery, nicht das Angriffstempo der Waffe. |
| Wahl des Skills | Der erste Skill der Liste, der nicht abklingt. Die Abklingzeit beginnt beim Ausholen. |
| Reichweite | Der Gegner greift an, sobald das Ziel in `AttackRange` und in der Reichweite des Skills steht. |
| Mehrere Projektile | Teilen sich den Treffer, jede Einheit wird pro Wurf höchstens einmal getroffen. Der Fächer öffnet sich um 12 Grad pro Projektil, höchstens 60 Grad. |
| Passende Mods | Twin Shot bekommen nur Schützen, Blinking nur Nahkämpfer. Die Auswahl steht als `Fit` am Mod. |
| Beschworene Monster | Sind Normal, haben das Level und die Gruppe des Beschwörers und greifen sofort mit an. |
| Simulation | Ruhende Gegner schlafen ab 2500 Pixel Abstand zum Helden. |
| Begehbare Fläche | Das Rechteck um den Boden des Levels, abzüglich aller Wände. |
| Abstand zu Wänden | 40 Pixel für die Mitte jeder Einheit. |

So funktionieren Gegner:

- Eine Gegner-Resource liegt unter `Resources/Enemies`. Ein Spawn-Marker verweist auf die Resource, nicht mehr auf eine Szene.
- Alle Gegner benutzen dasselbe Skript `Enemy`. Die Szene eines Gegners enthält Sprites, Animationen, Kollisionsform und Lebensbalken.
- `EnemyController.Spawn` baut einen Gegner: Szene instanziieren, Definition, Level und Mods übergeben, Ziel setzen, in den Baum hängen.
- Das Level ist `AreaLevel` am `EnemyController` plus `LevelOffset` am Gegner plus `LevelOffset` am Spawn-Marker, begrenzt auf 1 bis 100.
- Ein Attribut ist der Wert aus der Resource plus Wachstum mal gewonnene Level, abgerundet.
- Ausrüstung und Mods legen ihre Modifier ins Stat-Blatt, genau wie beim Helden.
- `EnemyBrain` im Kern entscheidet. Es bekommt pro Schritt eine Wahrnehmung (Abstand zum Ziel, Reichweite, Skill bereit, zu Hause angekommen) und liefert eine Entscheidung (Bewegung, Angriff beginnt, Treffer fällt, aufgeben).
- `Enemy` führt die Entscheidung aus: bewegen, Skill bezahlen, Animation spielen, Skill über `SkillExecutor` auslösen.
- Der `EnemyController` lässt nur wache Gegner denken. Wach ist, wer nicht ruht oder näher als `SimulationRadius` am Helden steht.
- Ein schlafender Gegner hält Animation und Bewegung an. Leben, Statuseffekte, Abklingzeiten und die Auslöser seiner Mods laufen weiter.
- Ein Treffer weckt auch einen schlafenden Gegner, egal wie weit der Angreifer entfernt ist.
- Neue Werte in den Enums `MonsterModFit` und `ModAim` nur am Ende anhängen, weil Resources sie als Zahl speichern.

So funktioniert die Wegfindung:

- Jedes Level bekommt einen Knoten `LevelNavigation`. Er hängt neben den Tile-Ebenen und bekommt die Boden-Ebene zugewiesen.
- Beim Start des Levels liest er alle Kollisionsformen der Ebene "Walls" unterhalb seines Elternknotens und backt daraus das Netz, auf einem eigenen Thread. Im Testlevel dauert das rund 30 Millisekunden und ergibt 48 Polygone.
- Nach dem Erzeugen eines Levels zur Laufzeit genügt ein Aufruf von `Rebuild`.
- `PathFollower` kapselt den `NavigationAgent2D`. Gegner und Held fragen ihn nur nach der Richtung zum Ziel.
- Der Agent hängt an der Kollisionsform. Der Pfad gilt damit für die Mitte des Körpers, nicht für die Füße.
- Einen neuen Pfad gibt es höchstens alle 0,4 Sekunden und nur, wenn sich das Ziel um mehr als 48 Pixel bewegt hat. Der Takt ist pro Einheit versetzt. Die Regel steht als `RepathTimer` im Kern.
- Das Ausweichen der Agents untereinander ist aus. Es ist teuer und kennt keine Wände.
- Ohne `LevelNavigation` im Level läuft jede Einheit wie früher in gerader Linie.
- Die Navigation in den Tiles des TileSets bleibt ungenutzt. Die Godot-Doku rät davon ab, weil Einheiten damit an Ecken hängen bleiben.

So funktionieren Monster-Mods:

- Ein Mod ist eine Resource unter `Resources/MonsterMods/Pool`. `MonsterModLibrary` lädt alle Resources aus diesem Ordner, ein neuer Mod braucht dort keinen Eintrag.
- Ein Mod hat Werte und Effekte. Werte sind Modifier fürs Stat-Blatt. Ein Effekt verbindet einen Auslöser mit beliebig vielen Aktionen, dazu Chance, Abklingzeit und eine Schrift über dem Monster.
- `MonsterModRoller` im Kern wählt die Mods: nach Gewicht, keinen doppelt, aus jeder `ExclusiveGroup` höchstens einen, nur passende für Level und Art des Monsters.
- Die Seltenheit folgt aus der Zahl der Mods, die das Monster wirklich bekommen hat. Reicht der Vorrat nicht, sinkt sie.
- Eine Resource gehört allen Monstern mit diesem Mod gemeinsam. Was pro Monster läuft, etwa ein Takt, steht in der Bindung, die der Auslöser pro Monster anlegt.
- Aktionen laufen erst nach dem laufenden Physikschritt. Ein Treffer durch ein Projektil darf so ein Monster beschwören oder ein Projektil zurückschießen.
- "Skill wirken" nimmt jede Skill-Resource. Der Skill kostet dabei weder Mana noch Abklingzeit, den Takt bestimmt der Auslöser.

| Auslöser | Löst aus |
|---|---|
| `OnDeathTrigger` | Beim Tod |
| `OnDamageTakenTrigger` | Bei erlittenem Treffer, wahlweise auch bei abgewehrtem |
| `OnHitDealtTrigger` | Bei eigenem Treffer |
| `IntervalTrigger` | Alle X Sekunden, wahlweise nur im Kampf |
| `LifeBelowTrigger` | Sobald das Leben unter X % fällt, einmal oder bei jedem Unterschreiten |
| `OnEngageTrigger` | Wenn der Kampf beginnt |

| Aktion | Wirkung |
|---|---|
| `CastSkillAction` | Wirkt einen Skill: auf sich, auf das Ziel, auf den Angreifer oder auf einen zufälligen Punkt im Umkreis |
| `SummonAction` | Beschwört Monster neben sich |
| `HealAction` | Heilt einen Anteil des Lebens |
| `TeleportAction` | Springt neben das Ziel |
| `AddModifiersAction` | Legt Modifier ins Stat-Blatt, befristet oder bis zum Tod |

Die ersten zehn Mods, alle in [Resources/MonsterMods/Pool](../Resources/MonsterMods/Pool):

| Mod | Wirkung | Passt zu |
|---|---|---|
| Hasted | 33 % increased Angriffstempo | allen |
| Swift | 40 % increased Bewegungstempo | allen |
| Stalwart | 60 % more Leben | allen |
| Twin Shot | 100 % more Projektile, also doppelte | Schützen |
| Meteor Caller | Im Kampf alle 1,5 Sekunden ein Meteor auf einen zufälligen Punkt bis 400 Pixel um das Monster | allen |
| Volatile | Beim Tod eine Explosion um die Leiche, angekündigt durch einen Kreis | allen |
| Freezing Skin | Bei erlittenem Treffer 35 % Chance auf einen Frost Pulse, höchstens alle 2 Sekunden | allen |
| Broodmother | Unter 50 % Leben einmal drei Blue Blobs | allen |
| Blinking | Im Kampf alle 4 Sekunden ein Sprung neben das Ziel | Nahkämpfern |
| Berserk | Unter 35 % Leben 50 % more Angriffs- und Bewegungstempo bis zum Tod | allen |

Die Skills dazu, alle in [Resources/Skills/Monsters](../Resources/Skills/Monsters):

| Skill | Schaden | Wirkung |
|---|---|---|
| Meteor | 6 bis 12 Fire | Fläche am Zielpunkt, Radius 150, schlägt nach 1,2 Sekunden ein |
| Death Blast | 10 bis 16 Fire | Fläche um den Wirkenden, Radius 200, zündet nach 1 Sekunde |
| Frost Pulse | 3 bis 6 Frost | Fläche um den Wirkenden, Radius 220, wächst in 0,2 Sekunden. Dieselbe Szene wie Frost Nova |

Ein neuer Gegner in drei Schritten:

1. Szene anlegen, Wurzelknoten `CharacterBody2D` mit dem Skript `Enemy`. Sie braucht `CollisionShape2D`, `AnimationPlayer`, `AnimationTree` und einen Lebensbalken mit dem eindeutigen Namen `Healthbar`. Am einfachsten erbt sie von `base_enemy.tscn`.
2. Resource vom Typ `EnemyResource` unter `Resources/Enemies` anlegen und die Szene eintragen.
3. Die Resource an einem Spawn-Marker eintragen.

Ein neuer Mod in zwei Schritten:

1. Resource vom Typ `MonsterModResource` unter `Resources/MonsterMods/Pool` anlegen, Id und Namen vergeben.
2. Werte und Effekte im Inspector zusammenstecken.

Fehlt ein Baustein, ist er eine kleine Klasse: ein Auslöser erbt von `ModTrigger`, eine Aktion von `ModAction`. Danach steht er in jedem Mod zur Auswahl.

Die Werte der Gegner, alle in [Resources/Enemies](../Resources/Enemies):

| Gegner | Leben auf Level 1 | Tempo | Natürliche Waffe | Reichweite | Ausholen und Erholen | Besonderes |
|---|---|---|---|---|---|---|
| Blue Blob | 9 | 25 | 1 bis 3 Frost | 80 | 0,5 s und 0,7 s | 75 % Frostresistenz |
| Yellow Blob | 76 | 100 | 2 bis 4 Lightning | 80 | 0,5 s und 0,7 s | 75 % Blitzresistenz |
| Test Enemy | 263 | 50 | 3 bis 6 Fire | 350 | 0,6 s und 1,0 s | Skill Fire Spit |

Alle drei haben Aggroradius 500, Verfolgungszeit 6 Sekunden und 100 XP. Die Werte sind dieselben wie vor M5.

Messung mit 200 Gegnern im Testlevel, headless und damit ohne Zeichnen. Bei 60 Bildern pro Sekunde hat ein Physik-Frame 16,7 Millisekunden Zeit:

| Lage | Vor M5 | Nach M5 |
|---|---|---|
| Alle ruhen, der Held ist weit weg | 0,7 bis 0,8 ms | 0,8 bis 0,9 ms |
| Alle ruhen, der Held steht im Level, 114 Gegner sind wach | 0,6 bis 0,8 ms | 0,8 bis 0,9 ms |
| Alle 200 verfolgen den Helden und kämpfen | 2,3 ms | 2,0 bis 2,5 ms |

- 200 kämpfende Gegner brauchen ein Siebtel der verfügbaren Zeit. Wegfindung, Zustandsmaschine und Mods kosten gegenüber dem alten Stand nichts Messbares.
- Das Schlafen spart bei 200 Gegnern kaum etwas. P3 war in dieser Größe kein Engpass, der Befund stammte aus der Code-Lektüre.
- Der Code der Gegner macht rund die Hälfte der Zeit aus. Der Rest ist Arbeit der Engine.
- Gemessen wird unter Volllast: Die Engine soll 6000 Physik-Frames pro Sekunde rechnen und schafft nur einen Teil. Aus der Uhr ergibt sich die Dauer pro Frame.
- Der Monitor `TimePhysicsProcess` der Engine taugt dafür nicht. Er meldet den langsamsten Frame der letzten Sekunde, nicht den Mittelwert.

Bewusst offen gelassen:

- Das Bereichslevel ist ein Wert am `EnemyController`. Seit M6 setzt ihn der Abstieg: Bereichslevel der Oberfläche plus Tiefe.
- Ausrüstung von Gegnern bekommt keine Affixe. Gewürfelte Affixe nach Monsterlevel wären ein Weg, den Schaden mit dem Level wachsen zu lassen, siehe B9.
- XP und der Schaden der Mod-Skills wachsen nicht mit dem Level.
- Gegner weichen einander nicht aus und stehen beim Helden übereinander. Das war vor M5 auch so.
- Ein Radius gilt für alle Einheiten. Rare Elite sind größer und streifen an Ecken entlang.
- Begehbar ist das ganze Rechteck um den Boden. Löcher im Boden ohne Kollisionsform gelten als begehbar, so wie für die Physik auch. Räume mit eigenen Umrissen kommen mit M6.
- Der Wechsel auf 3D tauscht `NavigationAgent2D` und `NavigationRegion2D` gegen ihre 3D-Geschwister. Betroffen sind `PathFollower` und `LevelNavigation`, der Kern nicht.
- Die Mods stehen nur auf dem Namensschild. Eine Beschreibung beim Überfahren mit der Maus fehlt.
- Das Namensschild ist nur zu sehen, solange der Gegner wach ist.
- Der `EnemyController` sucht Held und Gegner-Container weiter über feste Namen in der Szene. Das gehört zu A1.
- Die Szenen `lightning_strike.tscn` und `thunderbolt.tscn` verweisen auf veraltete UIDs und melden beim Laden je eine Warnung. Das war vor M5 auch so.

### Entscheidungspunkt: 2D oder 3D (entschieden am 29.09.2026)

Die Entscheidung musste spätestens hier fallen, weil M6 Levelgrafik erzeugt.

Der Vergleich ist am 29.09.2026 auf dem Branch `master_Compare3D` gebaut. Held, drei Gegner und der Feuerball laufen in 3D auf demselben Logik-Kern, mit Grundkörpern als Platzhalter. Messwerte, Aufwand und Empfehlung stehen in [VERGLEICH_2D_3D.md](VERGLEICH_2D_3D.md).

| Frage | Befund |
|---|---|
| Kern und Daten | Laufen in 3D unverändert |
| Neu geschrieben | 2.204 Zeilen Hüllen unter `Scripts/Spike3D` |
| Rechenzeit | Kein Unterschied von Belang |
| Bildrate mit 200 Gegnern | 3D 63, 2D 107. Ohne Schatten liegt 3D gleichauf. |
| Aufwand pro Gegner | In 3D kleiner, sobald ein Modell da ist |

Entscheidung vom 29.09.2026: 3D im Look der PlayStation 1, Vorbild Silent Hill.

| Punkt | Festlegung |
|---|---|
| Grund | Der 2D-Stil gefällt, die Sprites machen aber zu viel Arbeit. Low-Poly-Modelle lassen sich selbst bauen. |
| Look | Wackelnde Eckpunkte, verzogene Texturen, 240 Bildzeilen, 15 Bit Farbtiefe mit Punktmuster, kleine ungefilterte Texturen, Dunkelheit |
| Vorlage | `Scripts/Spike3D`, `Scenes/Spike3D` und `Shaders/Spike3D`, mit dem Abschluss von M5.5 aufgelöst |

Die Kamera ist seit dem 29.09.2026 perspektivisch. F2 schaltet zum Vergleich weiter auf orthogonal um.

### M5.5: Umstellung auf 3D (M, abgeschlossen am 29.09.2026 auf `master_Compare3D`)

Ziel: Die 3D-Fassung kann alles, was die 2D-Fassung kann. Danach entfällt die 2D-Schicht.

- Erledigt mit dem Vergleich: Einheit, Held, Gegner, Projektil, Wegfindung, Schadenszahlen, Kamera.
- Erledigt: PS1-Look mit zwei Shadern, im Level umschaltbar.
- Erledigt: Der Held trägt Items, und angelegte Ausrüstung ist an ihm zu sehen. Auch das Amulett hat einen Befestigungspunkt.
- Erledigt: Inventar, Charakterbogen, Skill-Leiste, Orbs und XP-Balken hängen an der Schnittstelle `IHero`.
- Erledigt: Alle Skills wirken, als Projektil oder als Fläche auf dem Boden.
- Erledigt: XP von Gegnern, Level-up mit Attributspunkt und Effekt, XP-Verlust beim Tod. Die Regel liegt als `HeroProgress` im Kern.
- Erledigt: Level-up-Dialog und Todesbildschirm. Der Held steht erst auf, wenn der Spieler es verlangt.
- Erledigt: Monster-Mods, Elite und Rare Elite, Namensschild.
- Erledigt: Beutel am Boden und Beute von Gegnern.
- Erledigt: Speichern und Laden. Der Spielstand der 2D-Fassung bleibt gültig.
- Erledigt: Overlay-Karte und Kellertür.
- Erledigt: Aufräumen. Code und Szenen der 2D-Fassung und der alte 3D-Prototyp sind entfallen, die 3D-Klassen tragen die endgültigen Namen, die Hauptszene ist `Scenes/test_level.tscn`.
- Herausgenommen: eigene Modelle für Held, Gegner und Items. Sie entstehen in Blender und sind ein eigener Punkt, siehe unten.

Fertig, wenn die Hauptszene in 3D läuft und die Laufzeitprüfungen aus M2 bis M5 dort bestehen.

Stand des Fertig-Kriteriums: erfüllt. Eine Laufzeitprüfung mit 70 Schritten lief dreimal hintereinander fehlerfrei in der Hauptszene. Sie deckt Ausrüstung, Zweihandregel, Bogen und Trank, Nahkampf, Gegnerangriffe, Feuerball mit Fork, Frost Nova, Fraktionen, Verfolgen, Aufgeben und Heimkehr, den Weg um die Mauer des Hofs, Level 10, Elite, Rare Elite, Namensschild, die Mods Stalwart, Volatile, Broodmother und Meteor Caller, Beute, Beutel, Level-up-Dialog, Karte, Kellertür und Todesbildschirm ab. Ein zweiter Lauf startet das Spiel neu und vergleicht den geladenen Charakter mit dem gespeicherten, in 4 Schritten. Die 560 Unit-Tests sind grün, dazu kam eine Sichtprüfung mit Bildschirmfotos. Eine Nachprüfung mit 22 Schritten lief dreimal fehlerfrei und deckt den Radius der Beutel, das Hinlaufen, das Abbrechen, Aura und Größe von Elite und den Winkel der Karte ab.

Getroffene Designentscheidungen vom 29.09.2026:

| Frage | Entscheidung |
|---|---|
| 2D oder 3D | 3D im Look der PlayStation 1 |
| Sichtbare Ausrüstung | Ja, man soll Ausrüstung am Helden sehen |
| Modelle | Starre Teile: Kopf, Torso, Arme und Beine sind einzelne Netze ohne Gewichte. Kein Skelett. |
| Sichtbare Plätze | 12 von 16: Waffe, Nebenhand, Helm, Amulett, Schultern, Torso, Rücken, Gürtel, Handgelenke, Hände, Beine, Füße. Nur die vier Ringe bleiben unsichtbar. |
| Ausrüstung an Gegnern | Ja, sobald es Gegner mit Armen gibt |
| Kamera | In M5.5 vertagt, vor M6 entschieden: perspektivisch. F2 schaltet zum Vergleich weiter um. |
| Level-up | Derselbe Effekt wie in 2D, als Partikel in 3D |
| Eigene Modelle | Kein Teil von M5.5. M5.5 schließt mit Platzhaltern aus Grundkörpern ab. |
| Aufräumen | Code und Szenen der 2D-Fassung entfallen. Sprites, Tilesets und Texturen der 2D-Effekte bleiben im Repo. |
| Overlay-Karte | Darf die echte Welt zeigen, aber aus dem Winkel der Spielkamera |
| Beutel | Haben einen `PickupRadius`. Steht der Held außerhalb, läuft er erst hin. |
| Elite | Sind größer und haben eine Aura, die leuchtet und glimmt |

Von mir festgelegt, weil es sich aus dem Umbau ergab:

| Punkt | Festlegung |
|---|---|
| Namen der Klassen | Die 3D-Klassen heißen wie ihre Vorgänger in 2D: `BaseUnit`, `Enemy`, `SkillCast`, `SkillAim`, `SkillExecutor`, `SkillProjectile`, `SkillArea`, `UnitRegistry`, `PathFollower`, `LevelNavigation`, `SpawnMarker`. Der Held heißt `Hero`. |
| Ordner | `Scripts/Spike3D` ist aufgelöst in `Scripts/Units`, `Scripts/Skills`, `Scripts/World` und `Scripts/UI`. Szenen liegen unter `Scenes/Units`, `Scenes/Items`, `Scenes/Skills` und `Scenes/Objects`. |
| Shader und Texturen | `Shaders/Ps1` und `Textures/World`. Die Shader der 2D-Effekte liegen unter `Shaders/Archive2D`. |
| Pfade der Szenen | Die 3D-Szenen von Gegnern und Skills liegen auf den Pfaden der 2D-Szenen. Gegner-, Skill- und Waffen-Resources zeigen dadurch ohne Änderung auf 3D. |
| Paarige Plätze | Hände und Handgelenke haben links und rechts je einen Befestigungspunkt und bekommen das Modell an beiden |
| Punkt fürs Amulett | `AttachNeck` sitzt am Halsansatz. Ein Amulett hängt von dort nach vorn auf die Brust. |
| Hieb und Schuss | Beim Hieb hebt sich der rechte Arm um 40 Grad und schwingt 140 Grad quer. Beim Schuss hebt er sich um 80 Grad. |
| Start-Items | Ein Charakter ohne Spielstand startet im Testlevel mit je einem Stück aller neun Item-Basen, Stärke 2 und Geschick 2. Ein geladener Charakter bringt seine eigenen Items und Attribute mit. |
| Größe von Elite | Darstellung und Kollisionsform wachsen um denselben Faktor, 1,25 für Elite und 1,5 für Rare Elite. Klickfläche, Lebensbalken und Namensschild wachsen mit. Die Werte stehen am `EnemyController`. |
| Aura von Elite | Ein Ring am Boden, ein pulsierendes Licht und ein glimmender Körper, alles in der Farbe des Namens: blau für Elite, golden für Rare Elite |
| Radius der Beutel | 150 Pixel, also 1,5 m. Der Wert steht am Beutel im Feld `PickupRadius`. |
| Namensschild | Liegt wie die Schadenszahlen auf der 2D-Ebene und folgt dem Monster. Schrift in der 3D-Welt ginge in den 240 Bildzeilen unter. |
| Klick auf einen Beutel | Der Klick gehört zuerst dem Beutel, dann dem Skill auf der linken Maustaste. Eine Lauftaste oder ein Angriff bricht den Weg zum Beutel ab. |
| Overlay-Karte | Halb durchsichtig über dem Spiel, der Held steht in der Mitte. Sie zeigt 60 m von oben nach unten, gut dreimal so viel wie das Spiel. |
| Kellertür | Eine Falltür im Boden bei (4, 0, 5). Sie leuchtet unter der Maus und schreibt beim Klick weiter nur eine Logzeile. |
| Form der Flächen | Ein Kreis auf dem Boden. Die Stauchung aus 2D ist entfallen, `AreaSettings.Contains` rechnet mit einem Kreis. |
| Aussehen der Effekte | Leuchtende Ringe und Körper aus Grundformen |
| Kollisionsebenen | Ebene 5 ist der Boden, Ebene 6 heißt `Interactive` und trägt Beutel und Kellertür |

So ist die 3D-Fassung aufgebaut:

- `BaseUnit` ist ein `CharacterBody3D` mit Stat-Blatt, Statuseffekten und Abklingzeiten. `Hero` und `Enemy` erben davon.
- Kern und Resources rechnen in Pixeln, die Welt in Metern. `WorldScale` rechnet mit 100 Pixeln pro Meter um.
- Das Testlevel hat drei Steuerknoten: `EnemyController` spawnt und lenkt Gegner, vergibt XP und Beute. `GameController` lädt und speichert und verbindet Held, Dialoge und Todesbildschirm. `Lootsystem` würfelt die Beute.
- Beide Controller bekommen Held und Knoten als Felder im Inspector. Feste Namen in der Szene brauchen nur noch `%ItemTooltip`, `%DeathScreen`, `%LevelUpDialog` und `%OpenLevelUpDialogButton`.
- Schadenszahlen und Namensschilder liegen auf der Ebene `CombatTextLayer` des Levels.

So funktioniert sichtbare Ausrüstung:

- Jede anlegbare Item-Basis hat das Feld `WornModel`. Es zeigt auf eine Szene mit dem Modell. Ohne Modell bleibt das Item unsichtbar.
- Das Modell des Helden hat Befestigungspunkte. Ein Punkt heißt `Attach` plus Platz, etwa `AttachHelmet` oder `AttachPhysicalWeapon`.
- Ein Platz ist sichtbar, wenn es einen solchen Punkt gibt. Es gibt dafür keine Liste im Code.
- `WornItems` hört auf das Anlegen und Ablegen im Kern. Beim Anlegen hängt es das Modell an jeden passenden Punkt, beim Ablegen entfernt es das Modell.
- Der Ursprung eines Modells ist der Befestigungspunkt. Ein Schwert hat seinen Griff im Ursprung, ein Helm die Mitte des Kopfes, ein Amulett den Halsansatz.
- Gezeichnet werden die Modelle mit dem Material des PS1-Looks. `Ps1Look` stellt neue Modelle von selbst ein.

Ein Item sichtbar machen in zwei Schritten:

1. Szene mit dem Modell unter `Scenes/Items` anlegen, Wurzelknoten `Node3D`.
2. Die Szene im Feld `WornModel` der Item-Basis eintragen.

Einen Platz sichtbar machen: im Modell des Helden einen `Marker3D` mit dem Namen `Attach` plus Platz anlegen.

<img src="images/ausruestung_3d_varianten.webp" alt="Der 3D-Held ohne Ausrüstung, mit Schwert und Schild, mit Stab und beim Schuss mit dem Bogen" width="860">

So hängt die Oberfläche am Helden:

- `IHero` nennt alles, was die Oberfläche vom Helden braucht: Werte, Statuseffekte, Items, Waffe, Belegung der Leiste, Abklingzeiten, bekannte Skills, Leben, Mana, Level, XP und Attributspunkte.
- Drei Ereignisse melden Änderungen: `SheetChanged` für Werte und Level, `ResourcesChanged` für Leben und Mana, `XpChanged` für XP und Punkte.
- Orbs, Skill-Leiste, XP-Balken, Charakterbogen und Level-up-Dialog haben das Feld `player`. Steht dort ein Held, binden sie sich beim Start selbst an ihn.
- Beim Aufstieg sprüht `LevelUpEffect` Sterne. Die Szene hat die Werte des 2D-Effekts: 128 Sterne, 2 Sekunden, dieselbe Farbkurve.
- Godot stellt eine Textur auf Kompression und Mipmaps um, sobald sie zum ersten Mal in 3D erscheint. In der `.import`-Datei des Sterns steht deshalb `detect_3d/compress_to=0`, wie bei den Texturen unter `Textures/World`.

So funktionieren Skills in 3D:

- Das Feld `EffectScene` der Skill-Resource zeigt auf die Szene der Wirkung. Eine Fernkampfwaffe bringt ihr Projektil im Feld `ProjectileScene` mit.
- Ein Projektil hat den Wurzelknoten `Area3D` mit `SkillProjectile` und zeigt nach -Z. Seine Kollisionsform ist ein hoher Zylinder.
- Eine Fläche hat den Wurzelknoten `Node3D` mit `SkillArea`. `Visual` zeigt den Radius und wächst in Breite und Tiefe, `VisualRadius` nennt den Radius in Metern ohne Skalierung.
- `Impact` bleibt bis zum Einschlag unsichtbar. Ein Ton unter `Impact` spielt beim Einschlag und überlebt die Fläche.
- Diese Schritte ersetzen die Anleitung unter M3, die für 2D geschrieben ist.

Die Platzhalter, alle in [Scenes/Skills](../Scenes/Skills):

| Skill | Szene | Aussehen |
|---|---|---|
| Fireball, Fire Spit | `fireball.tscn` | Glühende Kugel mit Schweif |
| Short Bow | `arrow.tscn` | Pfeil mit Spitze und Federn |
| Lightning Strike | `lightning_bolt.tscn` | Gezackter Blitz |
| Frost Nova, Frost Pulse | `frost_nova.tscn` | Hellblauer Ring, der nach außen wächst |
| Thunderbolt | `thunderbolt.tscn` | Blauer Kreis, dann ein Blitz von oben mit Licht und Ton |
| Meteor | `meteor.tscn` | Roter Kreis, dann eine flache Kuppel aus Feuer |
| Death Blast | `death_blast.tscn` | Dunkelroter Kreis um den Wirkenden, dann eine flache Kuppel aus Feuer |

<img src="images/hud_und_flaechen_3d.webp" alt="Das 3D-Testlevel mit Orbs, Skill-Leiste und XP-Balken: Frost Nova, Einschlag von Thunderbolt, Einschlag eines Meteors und der Tooltip von Fireball" width="860">

So funktionieren Beute, Karte und Kellertür:

- `Lootbag` ist ein `Area3D` auf der Ebene `Interactive`. `Lootbag.Drop` legt einen Beutel in die Welt, `Collect` hebt ihn auf. Ist das Inventar voll, hüpft der Beutel und bleibt liegen.
- Der Held prüft bei einem Linksklick zuerst mit einem Strahl, ob ein Beutel unter der Maus liegt. In Reichweite hebt er ihn sofort auf, sonst läuft er über das Navigationsnetz hin. Seit dem Nachtrag unten gilt das nur, solange die Schilder der Beute aus sind.
- Die Karte ist ein `SubViewport` mit eigener Kamera und eigener Umgebung ohne Nebel. Die Kamera übernimmt die Ausrichtung der Spielkamera. Gerechnet wird die Karte nur, solange sie zu sehen ist.
- `EliteAura` baut Ring und Licht eines Elite aus Farbe und Radius. Der Körper glimmt in der Farbe des Namens. Das Aufleuchten unter der Maus ist mit dem Nachtrag unten entfallen.
- Die Kellertür ist ein `Area3D`. Ihr Knoten `Glow` ist das Gegenstück zu den Lichtstrahlen der 2D-Tür.

<img src="images/elite_aura_und_karte_3d.webp" alt="Links ein Elite und ein Rare Elite mit Aura, rechts die Overlay-Karte aus dem Winkel der Spielkamera" width="860">

Das folgende Bild zeigt den Stand vor den Änderungswünschen, mit der Karte von oben und Elite ohne Aura.

<img src="images/abschluss_m55_3d.webp" alt="Elite mit Namensschild, Beutel und leuchtende Kellertür, die Overlay-Karte, der Level-up-Dialog und der Todesbildschirm" width="860">

Frühere Prüfungen aus M5.5, alle vom 29.09.2026: 70 Schritte für Ausrüstung und Kampf, 78 Schritte für Oberfläche, XP und Flächen, 8 Schritte für den Level-up-Effekt, dazu zwei Gegenproben im 2D-Testlevel mit 14 und 24 Schritten, solange es die 2D-Fassung gab.

Die offene Frage aus M5.5 ist entschieden:

| Frage | Stand |
|---|---|
| Kamera orthogonal oder perspektivisch? | Perspektivisch, entschieden am 29.09.2026 vor M6. Verzogene Texturen gibt es nur perspektivisch. |

Bewusst offen gelassen:

- Gegner zeigen ihre Ausrüstung noch nicht.
- Es gibt noch keine Item-Basis für Amulette. Der Platz wird mit dem ersten Amulett sichtbar, das ein Modell hat.
- Die Karte der Oberfläche zeigt die Welt, wie sie ist, mit Boden, Mauern und Nebel der Ferne. Die gezeichnete Karte aus M6 liest den Grundriss einer erzeugten Ebene.
- Beutel hatten keinen Tooltip und kein Schimmern wie in 2D. Seit dem Nachtrag unten tragen sie einen Stern und ein Schild mit dem Namen des Items.
- Tunika und Gugel decken den Körper nur zu. Ein Modell, das Körperteile ersetzt, gibt es noch nicht.
- Die Sterne des Level-up-Effekts benutzen ein Standardmaterial, weil der Shader des PS1-Looks weder Partikelfarben noch additives Mischen kennt. Ihre Eckpunkte rasten deshalb nicht ein.
- Die Beschreibungen unter M2 bis M5 sind für 2D geschrieben. Regeln und Kern gelten unverändert, Szenen und Knoten sind jetzt die aus diesem Abschnitt.
- Sprites, Tilesets, die Texturen der 2D-Effekte und die Addons für 2D liegen ungenutzt im Repo.

#### Nachtrag vom 29.09.2026: Rückmeldung aus dem ersten Spielen

Umgesetzt auf dem Branch `master_PlaytestFeedback`. Der User hat die 3D-Fassung zum ersten Mal selbst gespielt. Aura und Karte bleiben, wie sie sind.

Vorher lief die Nachprüfung, die nach M5.5 offen war. Beim Umbenennen hatten die Knoten `CollisionShape3D` ihr "3D" verloren, die große Prüfung lief vor der Korrektur. Auf dem Stand von M5.5 bestanden 56 Schritte für Kampf, Schützen hinter Mauern und Tod ohne Fehler.

Getroffene Designentscheidungen vom 29.09.2026:

| Frage | Entscheidung |
|---|---|
| Gegner untereinander | Gegner dürfen sich nie überlappen |
| Spawn | Locker verstreut um den Marker, mindestens 1 m Luft zwischen den Körpern. Umkreis und Mindestabstand sind Felder am Marker. |
| Reichweiten | Zählen ab dem Rand des Körpers, nicht ab der Mitte. Die Werte bleiben bis zum Balance-Durchgang in M8, siehe B13. |
| Beutel | Dunkles Braun wie altes Leder. Ein kleiner weißer Stern glimmt rechts oben am Beutel und wird langsam größer und wieder kleiner. |
| Maus über Gegnern | Kein Aufleuchten mehr |
| Maus über Beuteln | Der Beutel wird etwas heller |
| Abwerfen | Items landen in einem Gitter im Kreis um den Helden. Liegt in der Nähe eines Punkts schon ein Beutel, kommt der nächste Punkt dran. Beute von Gegnern benutzt dasselbe Gitter. |
| Schilder der Beute | Name des Items in der Farbe der Seltenheit auf dunklem Kasten, wie in den großen ARPGs. `Alt` schaltet sie an und aus, beim Start sind sie an. |
| Aufheben | Sind die Schilder an, hebt man nur über das Schild auf. Sind sie aus, über den Beutel, und der Beutel unter der Maus zeigt sein Schild. |
| Ordnung der Schilder | Schilder überlappen nie und stapeln sich nach oben. Beim Aufheben verschiebt sich kein anderes Schild. Nach Aus und An dürfen sie sich neu ausrichten. |

Von mir festgelegt, weil es sich aus dem Umbau ergab:

| Punkt | Festlegung |
|---|---|
| Kollisionsformen | Kapseln mit dem Radius des Modells plus 3 cm: Blue Blob 0,48 m, Yellow Blob 0,58 m, Testgegner 0,53 m. Der Held hat 0,3 m statt 0,2 m. Gegner tragen die Maske 11 und stoßen damit auch aneinander. |
| Abstand zum Helden | Eine Marker-Gruppe spawnt nicht näher am Helden als ihre Aggro-Reichweite plus 100 Pixel. Mit 16 und 12 Blobs griff die Gruppe sonst schon beim Laden an. |
| Marker im Hof | Liegt in der Hofmitte bei (14, 0, 0). In der Ecke war für fünf Gegner mit Abstand kein Platz, einer landete vor dem Tor. |
| Navigationsnetz | Wege halten 0,75 m Abstand zu Mauern statt 0,5 m |
| Flächen | Treffen ein Ziel ab seinem Rand. Eine Fläche um den Wirkenden beginnt an dessen Rand. |
| Gitter der Beutel | Punkte im Abstand von 100 Pixeln, fest in der Welt. Die nächsten Punkte zuerst, bis 800 Pixel weit. Das Abwerfen in Richtung Maus ist entfallen. |
| Stern | Vier Zacken, vom Shader gezeichnet, 0,3 m groß. Ein Atemzug dauert 2,6 Sekunden, die Größe schwankt um 15 %, die Helligkeit sinkt um bis zu 35 %. Der Stern mit fünf Zacken aus dem Level-up war bei 240 Bildzeilen nur ein Fleck. |
| Aufhellen | Die Farbe des Leders mal 1,5 und das Eigenleuchten mal 3. Die Farbe allein reichte nicht, weil die dunkle Seite des Beutels kaum Licht bekommt. |
| Text der Schilder | Magic zeigt den Namen mit Affixen, Rare den erzeugten Namen und die Basis, Stapel die Anzahl in Klammern |
| Schild ohne Schilder | Das Schild des Beutels unter der Maus steht direkt über ihm und fängt die Maus nicht ab, sonst flackerte es |
| Kollisionsformen im Editor | Boden und Mauern des Testlevels haben `debug_fill = false`. Die Füllung lag genau auf den Flächen und flimmerte als welliges Muster, im Editor immer und im Spiel mit "Visible Collision Shapes". |

So funktionieren Körper und Plätze:

- `BaseUnit.BodyRadius` kommt aus der Kollisionsform. `DistancePxTo` misst von Rand zu Rand, für eine Einheit und für einen Punkt.
- `UnitRegistry` merkt sich den größten Körper und sucht um so viel weiter, sonst entginge der Suche ein großer Gegner am Rand.
- `SpotSearch` im Kern würfelt Punkte in einem Kreis, bis einer frei ist. Nach jeder erfolglosen Runde wächst der Kreis.
- `EnemyController.FindFreeSpot` entscheidet, was frei heißt: kein anderer Körper im Abstand, keine Mauer, und von der Mitte aus zu sehen. Jeder Spawn läuft darüber, auch Beschwören und Teleport.
- `SpawnArea` beschreibt den Wunsch: Mitte, Umkreis, Mindestabstand und Abstand zum Helden. Ohne Umkreis ist die Mitte der Wunschplatz.
- Am `SpawnMarker` stehen `ScatterRadius` mit 400 und `MinGap` mit 100, beide in Pixeln.
- `WallMinSlideAngle` ist 0. Godot hält einen Körper sonst an, der steiler als 15 Grad auf eine Mauer läuft, und große Körper blieben an jeder Ecke hängen.
- Auf dem Rückweg gilt als angekommen, wer 0,6 Sekunden lang nicht vorankommt oder näher als 300 Pixel am Ziel nur noch um einen anderen herumrutscht.

So funktionieren Beutel und Schilder:

- `GridSearch` im Kern geht die Punkte eines festen Gitters im Kreis um eine Mitte durch. `Lootbag.DropAround` sperrt Punkte, an denen ein Beutel näher als 75 Pixel liegt oder eine Mauer die Sicht zur Mitte versperrt.
- Wer abwirft, steht noch da. Der Beutel hält deshalb Abstand zu seinem Körper. Bei der Beute eines Gegners ist der tote Gegner die Mitte, ohne Abstand.
- `Lootbag` meldet `Appeared` und `Vanished` und kennt die Oberfläche nicht.
- `LabelStacker` im Kern hebt ein Schild über alle, die es berührt. Die liegenden Schilder bleiben, wo sie sind. `PlaceAll` ordnet alle auf einmal, von unten nach oben.
- `LootLabels` liegt im Level unter `CombatTextLayer` und braucht den Helden im Feld `Hero`. Jedes Schild merkt sich seinen Abstand zum Beutel auf dem Bildschirm und folgt damit der Kamera.
- Der Stern ist der Shader `Shaders/Ps1/glimmer.gdshader`. Er dreht sich zur Kamera, rückt 0,4 m auf sie zu und lässt sich über `view_offset` auf dem Bildschirm verschieben.
- Die Taste ist die Aktion `toggle_loot_labels` in den Projekteinstellungen.

<img src="images/beute_schilder_3d.webp" alt="Oben links 16 abgeworfene Items mit Schildern, oben rechts dieselben nach dem Aufheben eines Bogens, unten links nach Aus und An neu ausgerichtet, unten rechts die Beute von vier Gegnern im Gitter" width="860">

Geprüft, alles im laufenden Spiel und fehlerfrei:

| Prüfung | Schritte |
|---|---|
| Kampf, Schützen, Tod, Spawn, Gedränge, Rückweg, Elite durch das Tor, Tod des Helden | 80, mit sechs verschiedenen Seeds |
| Aufhellen der Beutel und Gegner ohne Aufleuchten | 13 |
| Abwerfen im Gitter, auch von zwei Orten und an einer Mauer | 15 |
| Beute von Gegnern im Gitter und Schilder | 36 |

Im Gedränge von 18 Gegnern blieben zwischen den sichtbaren Körpern mindestens 3,5 cm Luft. 39 neue Unit-Tests decken die Platzsuche, das Gitter und das Stapeln ab, insgesamt sind es 599.

Bewusst offen gelassen:

- Nahkampf sah nach einem Schlag in die Luft aus. Erledigt mit B13.
- Bei der perspektivischen Kamera ändern sich die Abstände der Beutel auf dem Bildschirm beim Laufen. Das gilt weiter: Ein Neuausrichten bei jeder Bewegung der Kamera ließ die Schilder zappeln und ist wieder entfallen, siehe Nachtrag zu M6.
- Die Taste schaltet beim Drücken. Wer mit Alt+Tab das Fenster wechselt, schaltet die Schilder dabei um.
- Ob die Schilder an oder aus sind, steht nicht im Spielstand.
- Der Text eines Schilds ändert sich nicht, wenn ein Stapel Tränke nur zum Teil ins Inventar passt.
- Der Todeseffekt zieht den Körper flach und 1,4-mal breiter. Er ragt dabei kurz unter die Nachbarn.
- Ein Gegner mit Blinking springt dem Helden alle vier Sekunden hinterher, auch außer Reichweite. Das Verhalten stammt aus M5.
- Bei 0,3 m Größe ist der Stern bei 240 Bildzeilen zwei bis drei Pixel groß. Auf dem dunklen Leder ist er als Kreuz zu erkennen.

### Eigene Modelle (läuft neben den Meilensteinen)

Ziel: Held, Gegner und Items bekommen eigene Low-Poly-Modelle aus Blender statt der Grundkörper.

- Die Modelle bestehen aus starren Teilen ohne Skelett.
- Der Held braucht die Knoten `Visual`, `Visual/Body`, `Visual/WeaponPivot` und die Befestigungspunkte mit dem Namen `Attach` plus Platz.
- Ein Gegner braucht `Visual` und `Visual/Body`. An `Body` hängen Klickfläche und das Färben beim Ausholen.
- Die Kollisionsform eines Gegners ist eine Kapsel mit dem Radius des Modells plus 3 cm, Maske 11. Sie ist so hoch, dass ein gerader Teil bleibt, sonst schieben sich Körper verschiedener Größe nach oben und unten weg.
- Material ist der Shader `Shaders/Ps1/ps1_surface.gdshader`, Texturen sind klein und ungefiltert.

### M6: Prozedurale Level mit handgebauten Räumen (L, umgesetzt am 29.09.2026 auf `master_ProceduralLevels`)

Ziel: jeder Abstieg sieht anders aus, und eigene Räume lassen sich einstreuen.

- Erledigt: Generator erzeugt einen logischen Grundriss aus Räumen und Gängen, gesteuert über einen Seed.
- Erledigt: Raumvorlagen sind handgebaute Szenen mit Anschlusspunkten, Spawn-Markern und Gewicht.
- Erledigt: Regeln pro Vorlage: Häufigkeit, frühestes Level, Höchstzahl pro Ebene und Pflichtraum für Event-Locations.
- Erledigt: Thema als Resource mit Räumen, Texturen, Licht, Gegnerpool und Musik. Es gibt ein Testthema, das Thema eines Höllenkreises kommt mit M8.
- Erledigt: Overlay-Karte liest den logischen Grundriss.
- Anders gelöst: Feste Szenenpfade sind nicht durch globale Dienste ersetzt. Eine Ebene ist nur noch Inhalt, der in die laufende Szene gebaut wird. Das behebt A1.
- Erledigt: Gänge und Tore sind breiter als der größte Körper. Die Ebene `CombatTextLayer` mit `LootLabels` bleibt beim Wechsel der Ebene bestehen.

Fertig, wenn derselbe Seed zweimal dasselbe Level ergibt und ein handgebauter Event-Raum garantiert erscheint. Beides ist geprüft, im Kern und im laufenden Spiel.

Getroffene Designentscheidungen vom 29.09.2026:

| Frage | Entscheidung |
|---|---|
| Kamera | Perspektivisch |
| Grundriss | Handgebaute Räume auf einem Raster, der Generator zieht Gänge dazwischen |
| Wegführung | Verzweigt mit Schleifen. Mehrere Wege und Rundläufe, der Ausgang muss gesucht werden. |
| Nahkampf | 40 Pixel für den unbewaffneten Helden, 30 für Blobs, siehe B13 |
| Mauern | Alle Mauern haben einen Sockel und das restliche Mauerwerk darüber. Steht der Held vor der Mauer, sieht sie aus wie bisher. Steht die Mauer zwischen Held und Kamera, wird der obere Teil durchsichtig, und Spielobjekte dahinter werden klickbar. Der durchsichtige Bereich reicht so weit wie der Lichtradius. |
| Karte | Deckt sich beim Erkunden auf. Ein Schalter zum Testen zeigt die ganze Karte und führt zurück zum Erkundeten. Der Stand der Erkundung steht im Spielstand. |
| Einstieg bis M7 | Die Kellertür im Testlevel führt in Ebene 1. Der Ausgang jeder Ebene führt eine Ebene tiefer, mit neuem Seed und Bereichslevel plus 1. Held, Inventar und Oberfläche bleiben bestehen. |
| Gegner | Räume bringen ihre Spawn-Marker mit. In Gängen stehen vereinzelt kleine Gruppen aus dem Gegnerpool des Themas. |

Von mir festgelegt, weil es sich aus dem Bau ergab:

| Punkt | Festlegung |
|---|---|
| Zelle | 4 m. Eine Zelle ist zugleich die Breite eines Gangs. Raumvorlagen sind für dieses Maß gebaut. |
| Mauern | 0,5 m dick und 2,5 m hoch, davon 0,6 m Sockel. Der Sockel ist dunkler und steht 6 cm vor. Ein Gang ist zwischen den Mauern 3,5 m breit, der größte Körper misst 1,74 m. |
| Rand der Sicht | Das Mauerwerk schließt sich auf den letzten 1,5 m des Lichtradius mit einem Punktmuster. Beim Schritt durch eine Tür öffnet es sich über 0,5 m. |
| Oberfläche | Die neun Mauern des Testlevels sind dieselben Mauerstücke mit Sockel. Maße und Orte sind geblieben. |
| Drehung | Der Generator dreht Räume in Vierteldrehungen. Eine Vorlage kann das mit `CanRotate` abschalten. |
| Ausgang | Liegt unter 30 gewürfelten Plätzen am weitesten vom Start. Start und Ausgang sind nie direkt verbunden. |
| Rundwege | Zum kürzesten Baum über alle Räume kommen Verbindungen für 35 % der Räume dazu, gewählt unter den kürzesten |
| Gänge | Laufen lieber gerade, teilen sich vorhandene Strecken und meiden die Mauern fremder Räume |
| Größe | Ebene 1 hat 10 Räume, jede weitere einen mehr, höchstens 24 |
| Gruppen in Gängen | Eine Gruppe je 14 Zellen Gang, nicht näher als 2 Zellen an einer Tür und 3 Zellen am Startraum |
| Gegnerpool | Blue Blob mit Gewicht 3 in Gruppen von 3 bis 5, Yellow Blob mit Gewicht 2 in Gruppen von 2 bis 4, der Zauberer ab Bereichslevel 3 allein oder zu zweit |
| Erkunden | Aufgedeckt wird, was vom Standort aus zu Fuß erreichbar ist, ohne den Lichtradius zu verlassen. Hinter einer Mauer bleibt die Karte dunkel. |
| Schalter der Karte | `F5`, wie die übrigen Tasten zum Testen |
| Speichern der Karte | Beim Betreten einer Ebene nach einer Sekunde, beim Erkunden nach zehn Sekunden und beim Beenden |
| Laden | War der Held beim Speichern unter der Erde, startet er am Start derselben Ebene. Gegner stehen wieder da. |
| Neuer Abstieg | Wer von der Oberfläche hinabsteigt, beginnt einen neuen Abstieg mit neuem Seed. Das Feld `Seed` am Knoten `Descent` legt ihn fest, 0 würfelt. |
| Benutzen | Die Kellertür öffnet sich erst, wenn der Held bei ihr steht. Aus der Ferne läuft er hin, wie zu einem Beutel. |
| Räume | Siehe den zweiten Nachtrag unten: Die Mauern eines Raums öffnen sich nur für den, der drin steht |
| Anzeige | Die Zeile oben links nennt unter der Erde Ebene, Seed des Abstiegs und Bereichslevel |

So entsteht eine Ebene:

1. `RoomPicker` wählt die Vorlagen: einen Start, einen Ausgang, jeden Pflichtraum und dazwischen gewürfelte Räume nach Gewicht, bis die Zahl der Räume erreicht ist.
2. `LevelGenerator` setzt den Start und legt jeden weiteren Raum neben einen schon gesetzten, mit 2 bis 4 Zellen Abstand.
3. Aus den Abständen der Räume entsteht der kürzeste Baum über alle Räume, dazu kommen die Verbindungen für die Rundwege.
4. Für jede Verbindung wählt er das Paar Türen mit dem kürzesten Weg. `CorridorRouter` sucht den Gang von Tür zu Tür.
5. Ist nicht jeder Raum vom Start aus erreichbar, beginnt ein neuer Versuch mit einem abgeleiteten Seed, höchstens 20.
6. Zuletzt kommen die Plätze für die Gruppen in den Gängen.

Das Ergebnis ist `LevelLayout`: ein Raster aus Fels, Raum und Gang, dazu Räume, Verbindungen und Türen. Aus ihm folgt, wo Mauern stehen: zum Fels immer, zwischen Raum und Gang überall außer an einer Tür.

So wird daraus Welt:

- `Descent` hängt in der Szene des Spiels und führt den Helden hinab. Er räumt Gegner, Beutel, Wirkungen und die alte Ebene ab und baut die neue unter den Knoten `Environment`.
- `LevelBuilder` legt Böden, stellt Mauern auf und hängt die Szenen der Räume ein. Aufeinanderfolgende Mauerkanten werden ein Stück, Gänge zerfallen in Rechtecke.
- `LevelGrid` rechnet Zellen in Meter um. Die Mitte des Startraums liegt im Ursprung der Welt.
- Danach backt `LevelNavigation` das Navigationsnetz neu, und der `EnemyController` spawnt aus den Markern der Räume und Gänge.
- Die Zufallsquelle `GameRandom` bekommt den Seed der Ebene. Dadurch stehen bei gleichem Seed auch dieselben Gegner am selben Ort.

So funktionieren Mauern:

- `WallSegment` ist ein gerades Stück Mauer längs seiner X-Achse, der Ursprung liegt am Boden. Es baut Sockel, Mauerwerk und Kollisionsform selbst. Als Tool zeigt es sich auch im Editor.
- Das Mauerwerk trägt den Shader `Shaders/Ps1/ps1_wall.gdshader`. Er kennt den Ort des Helden, seine Größe und seinen Lichtradius.
- Der Held steht hinter einem Stück, wenn er und die Kamera auf verschiedenen Seiten seiner Ebene stehen. Nur dann öffnet sich Mauerwerk.
- `RoomZone` ist die Fläche eines Raums, ein `Area3D` mit einem Quader. Der Aufbau der Ebene legt für jeden Raum eine an, im Testlevel liegt eine über dem Hof.
- Jedes Stück fragt, welcher Raum kurz vor und kurz hinter seiner Mitte liegt. Der Grundriss teilt eine Mauer dort in zwei Stücke, wo sich ändert, was auf einer ihrer Seiten liegt.
- `WallOpeningRule` im Kern entscheidet daraus und aus dem Raum des Helden, wie weit sich ein Stück öffnet: ganz, halb oder gar nicht. Neu entschieden wird nur, wenn der Held den Raum wechselt.
- Wo Mauerwerk den Helden selbst verdeckt, bleibt es nie ganz zu, gleich welche Regel sonst gilt.
- Halb offen ist ein Schachbrett aus Mauer und Durchblick im Raster der Bildzeilen.
- Jedes Stück hat einen unsichtbaren Körper, der nur Schatten wirft. Die Mauer steht auch dort, wo ihr Mauerwerk die Sicht freigibt.
- Dieselbe Rechnung steht im Kern als `WallFadeRule`. `WallFade.IsHidden` entscheidet damit, ob sich ein Gegner, ein Beutel oder die Kellertür anklicken lässt.
- Die Kollisionsform reicht über die ganze Höhe. Für Bewegung, Projektile und Sichtlinien der Gegner ändert sich nichts.
- Der Vertex-Teil des PS1-Looks liegt in `Shaders/Ps1/ps1_common.gdshaderinc`, damit Flächen und Mauerwerk denselben benutzen.

So entsteht ein neuer Raum:

1. Eine Szene unter `Scenes/Rooms` anlegen, an der Wurzel das Skript `RoomTemplate`. Der Ursprung ist die Mitte des Raums.
2. `WidthCells` und `HeightCells` setzen, dazu Rolle, Gewicht, frühestes Bereichslevel, Höchstzahl und Pflichtraum.
3. Für jede mögliche Tür einen `Marker3D` mit dem Skript `RoomDoor` auf den Rand setzen, in die Mitte einer Zelle.
4. Spawn-Marker setzen. Ein Marker ohne Gegner bekommt einen aus dem Gegnerpool des Themas.
5. Einrichtung bauen. Was im Weg stehen soll, ist ein `StaticBody3D` auf der Ebene der Mauern. Ein `WallSegment` ohne Textur übernimmt Textur und Höhen des Themas.
6. Der Startraum braucht einen `HeroStart`, der Ausgang eine Kellertür.
7. Die Szene im Thema unter `Rooms` eintragen.

Boden und Außenmauern baut der Aufbau der Ebene. Der Knoten `EditorPreview` zeigt im Editor die Fläche des Raums und wird beim Aufbau entfernt.

| Raum | Größe in Zellen | Rolle |
|---|---|---|
| `start_room` | 3 x 3 | Start |
| `exit_room` | 3 x 3 | Ausgang mit Kellertür und Wachen |
| `chamber` | 3 x 3 | Kammer |
| `hall` | 5 x 4 | Halle mit vier Pfeilern, doppeltes Gewicht |
| `gallery` | 6 x 3 | Galerie mit zwei Sichtblenden |
| `shrine` | 4 x 4 | Schrein mit Altar und Hüter, Pflichtraum |

Stellschrauben am Knoten `Descent`:

| Feld | Wert | Bedeutung |
|---|---|---|
| `Seed` | 0 | Seed des Abstiegs, 0 würfelt bei jedem Abstieg neu |
| `RoomCount` | 10 | Räume in Ebene 1 |
| `RoomsMorePerDepth` | 1 | So viele Räume kommen pro Ebene dazu |
| `MaxRoomCount` | 24 | Obergrenze |
| `MinGap`, `MaxGap` | 2 und 4 | Freie Zellen zwischen zwei Räumen |
| `LoopShare` | 0,35 | Zusätzliche Verbindungen als Anteil der Räume |
| `CellsPerCorridorPack` | 14 | Zellen Gang je Gruppe, 0 lässt die Gänge leer |

<img src="images/ebene_m6_3d.webp" alt="Oben links der Held hinter einer Mauer, deren Mauerwerk die Sicht freigibt, oben rechts vor einer Mauer, unten links der Schrein, unten rechts die ganze Karte einer Ebene" width="860">

Geprüft, alles fehlerfrei:

| Prüfung | Umfang |
|---|---|
| Unit-Tests | 93 neue für Generator, Grundriss, Drehung der Räume, Erkundung, Regeln der Mauern und Abstieg im Spielstand. Insgesamt 692. |
| Laufendes Spiel, headless, vor dem Nachtrag unten | 435 Schritte: Mauern der Oberfläche, Abstieg über die Kellertür, Abbau der alten Ebene, gleicher Seed, Wege zu allen Räumen, Türen nach der Drehung, freie Sicht und Anklicken, Karte, Spielstand, Ebene 2, zwölf Ebenen in vier Tiefen |
| Neustart mit Spielstand, vor dem Nachtrag unten | 9 Schritte: dieselbe Ebene, dieselbe Karte |
| Nachprüfung mit Fenster, nach dem ersten Nachtrag | 41 Schritte mit dem Seed aus dem Spiel des Users: Mauern an drei Türen, Anklicken eines Gegners hinter offenem Mauerwerk, starre Schilder, Hof der Oberfläche |
| Nachprüfung mit Fenster, nach dem zweiten Nachtrag | 45 Schritte: die vier Lagen aus den Bildern des Users im Hof des Testlevels und in der Ebene mit seinem Seed, dazu Schatten, verborgene Gegner und ihre Wirkungen |

Zwölf Ebenen entstehen samt Aufbau in unter einer Sekunde. Bei 1600 x 900 lief eine Ebene mit 120 Bildern pro Sekunde, mit den Schatten der Mauern aus dem zweiten Nachtrag mit 115. Beide Werte sind von der Bildwiederholrate des Monitors gedeckelt.

Ohne diesen Deckel, gemessen mit 37 Gegnern an der Oberfläche und 83 Gegnern in Ebene 3, davon 13 Elite mit Aura:

| Ort | Nur das Licht des Helden wirft Schatten | Alle 17 Lichter werfen Schatten |
|---|---|---|
| Oberfläche bei den Blobs | 259 | 191 |
| Oberfläche im Hof | 248 | 171 |
| Ebene 3 am Start | 286 | 233 |
| Ebene 3 im Schrein | 274 | 208 |
| Ebene 3 am Ausgang | 378 | 289 |

#### Nachtrag vom 29.09.2026: Rückmeldung aus dem Spielen der Ebenen

Der User hat die erzeugten Ebenen gespielt und zwei Dinge beobachtet.

| Beobachtung | Änderung |
|---|---|
| Schilder der Beute zappeln beim Laufen. "Die sollen einfach starr bleiben." | Die Schilder richten sich beim Laufen nicht mehr neu aus. Jedes hält seinen Abstand zum Beutel, wie vor M6. |
| Die Mauern sind uneinheitlich: Eine Mauer ist noch undurchsichtig, die daneben ist weg. | Der Held stand vor der Tür in der Ostmauer eines Raums. Meine Änderung öffnete beide Mauern, sobald vor ihnen Fels lag. Das war falsch herum und ist mit dem zweiten Nachtrag ersetzt. |

#### Zweiter Nachtrag vom 29.09.2026: Räume bleiben verschlossen

Der User hat vier Bilder geschickt. Das erste zeigt, wie es sein soll: Der Held steht im Hof, die nördlichen Mauern stehen, die südlichen sind durchsichtig.

| Beobachtung | Entscheidung des Users |
|---|---|
| Der Held steht außerhalb des Hofs, trotzdem wird dessen südliche Mauer durchsichtig | Südliche Mauern eines Raums werden nur durchsichtig, wenn der Held im Raum ist. Dafür braucht es eine Erkennung. |
| Der Held steht nördlich des Hofs und kann voll hineinsehen | Das Innere von Räumen muss dunkel und nicht einsehbar sein. Licht darf nicht durch Mauern scheinen, und im Raum darf keine Bewegung zu sehen sein, bis der Held direkten Sichtkontakt bekommt oder drin ist. |
| Hinter einer Mauer muss man trotzdem mit Objekten umgehen können | Nördliche Mauern haben 50 % Deckkraft, wenn der Held außerhalb und nördlich des Raums steht |
| Der Held steht hinter einer Mauer, die nächste ist nicht durchsichtig | Auch sie muss sich öffnen, der Held steht auch hinter ihr |

Die Regel seitdem. Sie gilt für Mauerwerk im Lichtradius, und nur, wenn der Held hinter der Mauer steht:

| Lage | Mauerwerk |
|---|---|
| Hinter der Mauer liegt ein Raum, in dem der Held nicht steht | Bleibt zu. Das sind die südlichen und östlichen Mauern eines fremden Raums. |
| Vor der Mauer liegt ein Raum, in dem der Held nicht steht | Halb durchsichtig. Das sind die nördlichen und westlichen Mauern eines fremden Raums. |
| Sonst | Durchsichtig. Das sind die südlichen Mauern des eigenen Raums, die Mauern der Gänge und Mauern auf freiem Feld. |
| Das Mauerwerk verdeckt den Helden selbst | Mindestens halb durchsichtig, im Umkreis von 0,9 m um die Sichtlinie |

Von mir festgelegt, weil es sich aus dem Bau ergab:

| Punkt | Festlegung |
|---|---|
| Erkennung | Jeder Raum hat eine Fläche. Der Held steht im Raum, wenn er auf ihr steht. Ein Flag an der Tür braucht es nicht. |
| Halb durchsichtig | Ein Schachbrett im Raster der Bildzeilen statt echter Durchsicht. Es passt zum Punktmuster des PS1-Looks und braucht kein Sortieren. |
| Schatten | Auf Ansage des Users werfen alle Lichter die Schatten der Mauern, auch im PS1-Look: das Licht des Helden, Mondlicht, Altar, Kellertür, Auren und die Lichter der Skills. Figuren stehen weiter auf dunklen Scheiben. F4 schaltet wie bisher die übrigen Schatten ein. |
| Sichtkontakt | Der Held sieht, wer mit ihm im selben Raum steht und wen keine Mauer verdeckt. Geprüft wird von 1,5 m Höhe zur Mitte des Körpers und zu seinen beiden Rändern. |
| Verborgen | Ein Gegner ohne Sichtkontakt zeigt weder Körper noch Aura, Lebensbalken, Namensschild oder Schadenszahlen und lässt sich nicht anklicken. Treffen kann er und kann man ihn trotzdem. |
| Wirkungen | Projektile und Flächen der Gegner zeigen sich nur mit Sichtkontakt |
| Takt | Acht Gegner pro Schritt der Physik, reihum. Bei 40 Gegnern vergeht bis zum nächsten Blick ein Zwölftel einer Sekunde. |
| Beute | Beutel und ihre Schilder bleiben sichtbar, auch in einem Raum, den der Held verlassen hat |
| Ersetzt | Die Unterscheidung nach Boden und Fels aus dem ersten Nachtrag ist entfallen |

<img src="images/raeume_und_mauern_3d.webp" alt="Oben links der Held im Hof mit durchsichtigen südlichen Mauern, oben rechts vor dem Tor mit verschlossenem Hof, unten links nördlich des Hofs hinter der halb durchsichtigen Mauer, unten rechts an einer Gangecke hinter offenem Mauerwerk" width="860">

Bewusst offen gelassen:

- Der Wechsel beim Betreten eines Raums geschieht in einem Schritt, ohne Überblenden.
- Das Umgebungslicht bleibt. Der Boden eines verschlossenen Raums ist über die Mauern hinweg schwach zu erkennen, was darin steht, nicht.
- Starre Schilder können sich bei perspektivischer Kamera überlappen. Rückt eine Gruppe von Beuteln beim Laufen an den oberen Bildrand, schrumpfen ihre Abstände auf dem Bildschirm, die Schilder bleiben gleich groß. `Alt` zweimal richtet sie neu aus.
- Zwischen Räumen und Gängen ist nichts. Der Fels hat keine Oberseite, man blickt ins Schwarze.
- Türen sind Lücken in der Mauer, ohne Rahmen und ohne Türblatt.
- Es gibt keinen Weg zurück nach oben. Treppen, Checkpoints und Town-Portal kommen mit M7.
- Räume sind Rechtecke. Eine Vorlage mit anderem Umriss gibt es nicht.
- Das Testthema hat keine Musik. Das Feld am Thema ist da, und `Descent` spielt, was dort steht.
- F2 schaltet die Kamera weiter um. Seit der Entscheidung für die Perspektive dient das nur noch dem Vergleich.
- Die Karte zeigt weder Gegner noch Beute.
- Ein Mauerstück in einer Raumvorlage zeigt im Editor die Textur aus dem Testthema, bis das Thema ihm seine gibt.
- Mit "einmal pro Kreis" ist vorerst einmal pro Ebene gemeint. Kreise mit mehreren Ebenen kommen mit M7.

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
             +-> M4 -> M5 -> [2D/3D] -> M5.5 -> M6 -> M7 -> M8 -> M9 -> M10
```

M3 und M4 sind voneinander unabhängig und können getauscht werden.
