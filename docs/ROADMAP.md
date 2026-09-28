# Höllenspiralenspiel: Analyse und Roadmap

Stand: 28.09.2026, Branch `master_MeleeCombat`.
Grundlage: Designdokument "Wyldes Gehirnsturmscribble" und der komplette C#-Code (rund 5.500 Zeilen) plus Szenen.
Die Befunde stammen aus Code-Lektüre. Die als behoben markierten Fehler und F19 wurden zusätzlich im laufenden Spiel geprüft, headless mit Godot 4.6.

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
| Waffen | Schadensspanne, Angriffe pro Sekunde, Swingtimer, Typ, Schadensart, Krit, Anforderungen | Werte vorhanden, werden aber im Kampf nicht benutzt. Klassen-Anforderung fehlt. |
| Rüstung | Helm, Torso, Handschuhe mit Rüstungswert | 3 von 16 Slots haben Item-Basen |
| Affixe | 16 Affixe mit Tiers, Gewichten, Itemlevel-Grenze, Prefix/Suffix, lokale und globale Mods | Slot-Tabelle aus dem PDF nur teilweise abgedeckt |
| Loot | Gewichtete Loot-Tabellen, Lootbags, Magic/Rare-Namen | Funktioniert im Testlevel |
| Inventar | Tetris-Inventar, Drag-and-drop, Tauschen, Stapeln, Tooltips | Nicht im PDF, aber fertig nutzbar |
| Ausrüstung | 16 Slots inklusive 4 Ringe, Anforderungsprüfung | Entspricht dem PDF |
| Leveling | XP-Tabelle bis Level 100, Level-up-Effekt, Attributspunkte, XP-Balken | Nicht im PDF, funktioniert |
| Zauber | Fireball mit Fork, Frost Nova, Lightning Strike, Skillbar mit Cooldowns | Elementarschaden wird über Resistenzen gemindert |
| Schadensminderung | Rüstungsformel, Resistenzen und Dodge für alle Einheiten | Parry und Block fehlen |
| Gegner | 3 Typen, Spawn-Marker, Gruppen-Aggro, Rare/Elite, Lebensbalken, Schadenszahlen | Nur Blobs und ein Testgegner |
| Atmosphäre | Punktlichter mit Schatten, abgedunkelte Szene, Overlay-Karte | Passt zum düsteren Vibe |
| 3D | Ein Prototyp, der nur Bewegung kann | Offene Frage aus dem PDF |

## 2. Was an kritischen Systemen fehlt

Ohne diese Systeme gibt es kein spielbares Spiel, nur eine Testszene.

1. **Spielertod.** Das Leben kann unter null fallen, ohne dass etwas passiert. Es gibt kein Game Over und keinen Respawn.
2. **Nahkampf.** Der Spieler hat keinen Angriff. Waffenwerte existieren nur im Tooltip.
3. **Gegnerangriffe.** Die Blobs haben keinen eigenen Angriff und schaden nur durch Berührung. Nur der Testgegner greift an, mit Feuerbällen.
4. **Speichern und Laden.** Es gibt keine Persistenz für Charakter, Inventar oder Fortschritt.
5. **Spielstruktur.** Hub, Levelwechsel, Hauptmenü, Pausenmenü und Freischaltung fehlen. Die Kellertür schreibt nur eine Logzeile.
6. **Levelgenerierung.** Es gibt nur ein handgebautes Testlevel.
7. **Wegfindung.** Gegner laufen in gerader Linie und bleiben an Wänden hängen.
8. **Statuseffekte.** Bleed, stapelnder Feuer-DoT, Action-Failure und Frost-Effekt aus dem PDF fehlen. Die Schadensart-Klassen sind leere Hüllen.
9. **Parry, Block, Krit aus Stats.** Die Werte werden berechnet, aber nirgends im Kampf ausgewertet.
10. **Skill-Erwerb.** Die drei Zauber sind fest im Spieler verdrahtet. Klassen und Skill-Fortschritt fehlen.
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

- F2, F5 und F6 sind pragmatisch behoben. M1 und M2 ersetzen diese Lösungen später durch den Stat-Kern und die Kampf-Pipeline.
- F17 führt den exportierten Wert `LifeBaseBonus` ein. Er wird auf das Basisleben aus den Attributen addiert.
- F18 ändert die Einheit von `DriftVelocity` auf Pixel pro Sekunde.

### 3.2 Performance

| Nr. | Problem | Stelle |
|---|---|---|
| P1 | Jeder Stat-Zugriff durchsucht die Modifikator-Liste mehrfach mit LINQ. Das passiert pro Einheit und pro Frame mehrfach und erzeugt laufend Müll für den Garbage Collector. | [BaseUnit.cs:66](../Scripts/Units/BaseUnit.cs) |
| P2 | Die Orbs bauen jeden Frame Text neu und setzen Shader-Parameter, auch wenn sich nichts ändert | [Player2D.cs:182](../Scripts/Units/Player2D.cs), [ResourceOrb.cs:128](../Scripts/UI/Character/ResourceOrb.cs) |
| P3 | Alle Gegner der Karte werden jeden Frame simuliert, egal wie weit sie entfernt sind | [EnemyController.cs:170](../Scripts/Controllers/EnemyController.cs) |
| P4 | Ein Feuerball kann sich auf bis zu 63 Projektile aufspalten. Jeder Treffer sortiert alle Gegner der Karte nach Entfernung. | [Fireball.cs:55](../Scripts/Abilities/Spells/Fireball.cs) |
| P5 | Das Inventar nutzt Godot-Dictionaries mit Float-Vektoren als Schlüssel. Jeder Zugriff wird zwischen C# und Engine konvertiert. | [Inventory.cs:17](../Scripts/UI/Character/Inventory.cs) |
| P6 | Knoten werden in Property-Gettern bei jedem Zugriff neu gesucht | [Inventory.cs:21](../Scripts/UI/Character/Inventory.cs), [EquipmentPanel.cs:26](../Scripts/UI/Character/EquipmentPanel.cs) |
| P7 | Der Zufallsgenerator wird bei jedem Dodge-Wurf neu initialisiert. Viele getrennte Zufallsquellen verhindern später Seeds und Koop. | [HitResult.cs:25](../Scripts/Models/HitResult.cs) |

### 3.3 Architektur

| Nr. | Problem | Folge |
|---|---|---|
| A1 | Rund 27 Stellen suchen Spieler, Controller oder `Environment` über feste Namen in der aktuellen Szene | Jedes neue Level muss exakt wie das Testlevel aufgebaut sein |
| A2 | Alle Stats stecken in einer 2D-Physik-Klasse | Blockiert die 2D/3D-Entscheidung, Tests und Koop |
| A3 | Spiellogik steckt in UI-Klassen. Der Skillbar-Button zaubert und zieht Mana ab. | Logik ist ohne UI nicht nutzbar und nicht testbar |
| A4 | Items sind Szenen-Knoten, die nie im Baum hängen | Speicherleck und nicht serialisierbar |
| A5 | Zwei parallele Skill-Hierarchien, Skills und Manakosten fest im Code | Neue Skills brauchen Codeänderungen an mehreren Stellen |
| A6 | UI wird per Code anhand der Fenstergröße platziert. Das Fenster ist fest 2560x1440 im exklusiven Vollbild. | Bricht bei anderen Auflösungen |
| A7 | Lootbag-Code existiert dreimal mit unterschiedlichem Verhalten | Quelle von F9 |
| A8 | Behoben am 28.09.2026. `.idea`, `*.user` und `obj` waren eingecheckt. Shader und Testszenen lagen im Projektwurzelordner. Leere Klassen wie `SceneDispenser` und `StaticMemory` existierten. | Unübersichtlich |
| A9 | Behoben am 28.09.2026. Eingabeaktionen hießen wie Tasten (`F`, `B`, `Tab`) statt nach ihrer Funktion. Die Namen stehen jetzt zentral in `InputActions`. Die Skill-Tasten sind weiter fest im Code und gehören zu M3. | Tastenbelegung lässt sich nicht sauber ändern |
| A10 | Zauber unterscheiden Freund und Feind über Typprüfungen auf `Player2D` und `BaseEnemy`, die Gruppe `monsters`, feste Kollisionsebenen und den `EnemyController` | Ein Zauber verhält sich nicht gleich für jeden, der ihn wirkt. Begleiter und Koop-Spieler sind nicht abgedeckt. |

Hinweis zu A10: Fireball, Frost Nova und Lightning Strike sind Testzauber und werden in M3 neu gebaut. Sie werden bis dahin nicht umgebaut. Der Feuerball kennt seit F6 zwei Seiten, entscheidet aber weiter über den Typ des Besitzers.

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

### M1: Stat-Kern in reinem C# (M)

Ziel: ein Stat-System, das schnell, testbar und unabhängig von 2D oder 3D ist.

- Klasse für Stat-Blätter mit Basiswerten, Modifikatoren und zwischengespeicherten Endwerten. Neuberechnung nur bei Änderung. Behebt P1, F2, F11.
- Abgeleitete Werte reagieren auf den Endwert eines Attributs, also auch auf Ausrüstung.
- Einheiten halten ein Stat-Blatt, statt selbst 350 Zeilen Stat-Code zu enthalten. Behebt A2.
- Echtes Testprojekt mit Tests für Stat-Berechnung und Wachstumskurven.
- Lichtradius als Stat ergänzen und an das Spielerlicht koppeln.
- Orbs nur bei Wertänderung aktualisieren. Behebt P2.

Fertig, wenn alle Werte im Charakterbogen aus dem neuen Kern kommen und die Tests grün sind.

### M2: Kampf-Pipeline, Tod und Nahkampf (M)

Ziel: eine vollständige Kampfschleife. Das ist der erste Meilenstein, der sich wie ein Spiel anfühlt.

- Eine zentrale Trefferauflösung: Ausweichen, Parry, Block, Krit, Minderung, Anwenden. Einmal würfeln, Ergebnis unveränderlich. Behebt F4, F5, P7.
- Nahkampfangriff des Spielers mit Waffenwerten: Swingtimer, Schadensspanne, Krit-Chance.
- Gegnerangriffe mit Windup und Recovery. Gegnerprojektile treffen den Spieler. Behebt F6.
- Spielertod mit XP-Verlust und Respawn.
- Schadensarten aus dem PDF: Crush, Pierce, Slash mit ihren Effekten.
- Statuseffekt-System: Bleed, stapelnder Feuer-DoT, Action-Failure, Frost-Verlangsamung.

Fertig, wenn Spieler und Gegner sich gegenseitig töten können und jede Schadensart ihren Effekt auslöst.

Offene Designfrage: Der Frost-Effekt ist im PDF leer. Vorschlag ist Verlangsamung von Bewegung und Angriff.

### M3: Skills als Daten (M)

Ziel: neue Skills ohne Codeänderung am Spieler.

- Skill-Definition als Resource: Kosten, Cooldown, Schaden, Schadensart, Szene, Icon.
- Eine Skill-Hierarchie statt zwei. Zauberlogik raus aus dem Skillbar-Button. Behebt A3, A5.
- Skillbar frei belegbar.
- Die drei Testzauber werden neu gebaut, nicht umgebaut. Behebt P4.
- Zauber sind unabhängig davon, wer sie wirkt. Behebt A10.
  - Jede Einheit bekommt eine Fraktion. Ein Zauber trifft Einheiten, deren Fraktion sich von der des Wirkenden unterscheidet.
  - Trefferlogik arbeitet nur mit `BaseUnit`, ohne Typprüfung auf Spieler oder Gegner.
  - Zielsuche für Fork und Flächenzauber fragt "feindliche Einheiten in der Nähe" ab, nicht den `EnemyController`.
  - Die Kollisionsmaske ergibt sich aus der Fraktion. Wände werden über ihre Kollisionsebene erkannt, nicht über den Knotennamen.
  - Geschwindigkeit, Lebenszeit und Anzahl der Forks kommen aus der Skill-Definition.
  - Erzeugte Projektile hängen am Elternknoten des Auslösers, ohne festen Pfad durch die Szene.

Fertig, wenn ein neuer Zauber nur aus einer Resource und einer Szene besteht und derselbe Zauber von Spieler und Gegner gewirkt werden kann.

Offene Designfrage: Gibt es Klassen, und wie bekommt man Skills? Das PDF nennt Klassen nur bei den Item-Anforderungen. Die Entscheidung wird erst hier gebraucht.

### M4: Items als Daten und Speichern (M)

Ziel: Charakter und Fortschritt überleben einen Neustart.

- Item-Basen als Resource, Item-Instanzen als reine Daten. Behebt A4.
- Inventar-Modell getrennt von der Inventar-Oberfläche, mit Ganzzahl-Koordinaten. Behebt F8, P5, P6.
- Aufgehobene Tränke landen automatisch auf vorhandenen Stapeln.
- Eine einzige Lootbag-Logik. Alle gewürfelten Items fallen. Behebt F7, F9, A7.
- Keine doppelten Affixe, verschachtelte Loot-Tabellen. Behebt F13, F14.
- Speichern und Laden von Charakter, Inventar, Ausrüstung und Fortschritt.

Fertig, wenn ein Charakter mit Ausrüstung nach Neustart identisch geladen wird.

### M5: Gegner-KI und Skalierung (M)

Ziel: Gegner, die sich durch Level bewegen und mit der Tiefe stärker werden.

- Gegner-Definition als Resource: Attribute, Level, Loot-Tabelle, XP, Angriffe. Behebt F17.
- Zustandsmaschine: Idle, Verfolgen, Windup, Angriff, Recovery, Tod. Todesanimation läuft zu Ende. Behebt F15.
- Wegfindung auf einem logischen Gitter, das für 2D und 3D gleich funktioniert.
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
- Balance-Durchgang für Leben, Schaden, XP und Loot.
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
