---
name: end-session
description: Beendet eine Arbeitssitzung in einem beliebigen Repo sauber. Bringt README, Pläne, Roadmaps und sonstige Doku auf den Stand des Codes, prüft Build und Tests, committet mit einer kurzen, präzisen Commit-Message, pusht und fasst die Sitzung im Chat zusammen. Immer auslösen, wenn der Nutzer /end-session oder „EndSession" schreibt, wenn er die Session, Sitzung oder Arbeit beenden, abschließen, zumachen oder Feierabend machen will, oder wenn er „Doku aktualisieren, commit und push" in einem Atemzug verlangt. Auch bei knappen Formulierungen wie „fertig machen", „abschließen", „wrap up" oder „zumachen", sobald Änderungen im Arbeitsbaum liegen oder Commits noch nicht gepusht sind.
---

# Session beenden

## Wozu

Am Ende einer Sitzung sollen drei Dinge stimmen: Die Doku beschreibt den Code, wie er jetzt
ist. Die Arbeit liegt gesichert auf `origin`. Und der Nutzer weiß nach einer Minute Lesen,
was passiert ist und wo es weitergeht.

Der Punkt mit der Doku ist der wichtigste und der, der am ehesten unter den Tisch fällt. Die
README ist die Karte zum Code, der Plan das Protokoll der Entscheidungen. Eine Karte, die eine
Route zeigt, die es nicht mehr gibt, ist schlimmer als keine Karte, weil man ihr glaubt. Jede
Sitzung, die Code ändert und die Doku nicht, macht die nächste Sitzung ein Stück blinder.

Der Skill beendet eine Sitzung, er verlängert sie nicht: keine neuen Features, kein
Refactoring „wo wir schon dabei sind", keine Versionssprünge. Was halb fertig ist, wird als
halb fertig dokumentiert, nicht schnell noch fertig gebaut.

## Schritt 0: Regeln des Repos gelten zuerst

Dieser Skill gilt für jedes Repo und kennt deshalb keines genau. Wo ein Repo eigene Regeln
für das Sitzungsende hat, gehen sie vor, weil sie aus Erfahrung mit genau diesem Code stammen:

- Liegt im Repo `.claude/skills/end-session/SKILL.md`, diese Datei lesen und ihr folgen. Sie
  nennt die konkreten Doku-Dateien, Prüfbefehle und Stolperfallen. Dieser Skill füllt dann nur
  die Lücken.
- `CLAUDE.md` im Repo und die Memory-Dateien zum Projekt auf Hinweise zu Commits, Branches,
  Doku und Tests lesen.

Widersprechen sich die Regeln, gewinnt das Repo. Im Zweifel den Nutzer fragen, bevor etwas
gepusht wird.

## Schritt 1: Feststellen, was in dieser Sitzung passiert ist

Zwei Quellen, beide nötig:

- **Git.** `git status --short`, `git diff --stat` und `git log origin/<branch>..HEAD --oneline`
  zeigen, was ungesichert ist und was committet, aber nicht gepusht. Auch `git log` seit dem
  letzten Push lesen, wenn die Sitzung schon zwischendurch committet hat. Hat der Branch noch
  kein Gegenstück auf `origin`, gegen den Hauptbranch vergleichen.
- **Der Gesprächsverlauf.** Welche Features, Fixes und Entscheidungen, welche Erkenntnisse über
  die Maschine, was der Nutzer korrigiert hat, was bewusst offen blieb. Git zeigt das Ergebnis,
  das Gespräch zeigt das Warum. Beides gehört in die Doku, aber an verschiedene Stellen.

Liegen Änderungen im Arbeitsbaum, die nicht aus dieser Sitzung stammen, zum Beispiel weil der
Nutzer nebenher im Editor gearbeitet hat: benennen und fragen, ob sie mit in den Commit sollen.

Ist nichts geändert und nichts ungepusht, gibt es nur die Zusammenfassung. Ein leerer Commit
hilft niemandem.

## Schritt 2: Doku auf den Stand des Codes bringen

Zuerst herausfinden, welche Doku das Repo hat. Üblich sind:

| Doku | Wann anfassen | Was |
|---|---|---|
| `README.md` | bei jeder Änderung an Funktionsumfang, Bedienung, Konfiguration oder Tests | die betroffenen Abschnitte, zum Beispiel Feature-Liste, Steuerung, Konfiguration, Testzahlen, Status von Meilensteinen, Stand-Datum und Badges |
| Pläne und Roadmaps, meist unter `docs/` | wenn ein Schritt umgesetzt oder ein Plan verlassen wurde | Status des Schritts, ein Absatz zu Abweichungen aus der Umsetzung, offene Punkte ergänzen oder streichen |
| übrige Dateien unter `docs/` | nur, wenn die Sitzung ihr Thema berührt hat | den betroffenen Abschnitt |
| Memory | Erkenntnisse über die Entwicklungsmaschine, Nutzerpräferenzen, Korrekturen | gehört nicht ins Repo, sondern in die Memory-Dateien |

Ein erledigter Schritt wandert in der Doku von „offen" nach „umgesetzt", ein neues Feature in
die Feature-Liste, ein neuer Konfigurationsschlüssel in die Tabelle. Ein Abschnitt, den die
Sitzung nicht berührt hat, bleibt unangetastet: kein Umformulieren, kein Auffüllen.

Hat das Repo keine Doku, wird keine erfunden. Dann steht in der Zusammenfassung, dass es keine
gibt, und der Nutzer entscheidet, ob er eine will.

Regeln, die aus Erfahrung stammen:

- **Jede Aussage am Code prüfen, nicht aus dem Gedächtnis.** Zahlen zählen, Namen greppen,
  Grenzen an den Konstanten ablesen, Tasten an der Eingabekonfiguration. Was die Sitzung
  „eigentlich" bauen wollte, ist nicht immer, was im Code steht.
- **Abweichungen ehrlich benennen.** Der Plan sagte A, gebaut wurde B, weil C. Genau dieser
  Satz ist es, den die nächste Sitzung braucht. Ein Plan, der stillschweigend nicht mehr
  stimmt, ist die teuerste Form von Doku.
- **Daten absolut.** „9. September 2026", nie „heute" oder „gestern". Die Doku wird in Wochen
  gelesen.
- **Sprache wie der Rest des Repos.** Ton, Sprache und Schreibweise der vorhandenen Doku
  übernehmen. Keine Passwörter, keine Secrets, auch keine aus der Entwicklung.

## Schritt 3: Prüfen, bevor es rausgeht

Wenn Code geändert wurde, Build und Tests des Repos laufen lassen. Welche Befehle das sind,
steht meist in der README oder ergibt sich aus dem Projekt:

| Projekt | Build | Tests |
|---|---|---|
| .NET | `dotnet build` | `dotnet test` |
| Node | `npm run build` | `npm test` |
| Rust | `cargo build` | `cargo test` |
| Python | entfällt meist | `pytest` |

Bei Spielen und Anwendungen mit eigener Laufzeit zusätzlich einen kurzen Startversuch machen,
wenn das ohne Bedienung geht, zum Beispiel Godot mit `--headless --quit-after`.

Ein roter Stand auf dem Hauptbranch bedeutet, dass die nächste Sitzung mit einer Fehlersuche
beginnt statt mit dem Plan. Scheitert der Build aus einem Grund, der nichts mit dem Code zu
tun hat, erst die Umgebung prüfen: Ein Vorschau-Server, ein Editor oder ein Testlauf aus einer
alten Sitzung sperrt gern Dateien im Ausgabeordner.

Sind Tests rot: nicht committen, nicht pushen. Den Fehler in der Zusammenfassung mit Ausgabe
nennen und die Entscheidung dem Nutzer lassen. Einzige Ausnahme: der Nutzer hat ausdrücklich
gesagt, dass es trotzdem raus soll.

Danach noch einmal `git status`: keine Fremddateien, also keine Secrets, nichts Temporäres,
keine Ausgaben von Werkzeugen, keine Prüfdateien aus der Sitzung. Was ignoriert sein sollte
und es nicht ist, gehört in die `.gitignore`.

## Schritt 4: Commit

Alles Geänderte stagen, nachdem der Status geprüft ist. Committet wird auf dem Branch, auf dem
die Sitzung gearbeitet hat. Der Skill wechselt keinen Branch und führt nichts zusammen. Will
der Nutzer in den Hauptbranch mergen, sagt er das eigens.

Die Message schreibt man am besten in eine Datei im Scratchpad und übergibt sie mit
`git commit -F`. Umlaute, Anführungszeichen und `$` gehen in der Shell-Quotierung leicht
kaputt.

Die Message sagt, was sich für Produkt oder Repo geändert hat, nicht dass sich etwas geändert
hat. „Update" oder „Doku" allein ist keine Aussage.

- **Betreff:** höchstens 72 Zeichen, ein Satz ohne Punkt, in der Sprache der bisherigen
  Commits. Ein erledigter Planschritt beginnt mit seiner Nummer oder seinem Kürzel.
- **Body:** ein Punkt je wesentlicher Änderung, die Doku-Anpassung in einer Zeile. Weglassen,
  was der Betreff schon sagt.
- **Abschluss:** die `Co-Authored-By`-Zeile, die die Umgebung vorgibt.

Beispiele, die den Ton treffen:

```
Schritt 7: Exposé-Detailseite als static SSR
M1: Stat-Kern in reinem C# mit Tests
Reset-Link trägt das Konto, Mailversand geprüft
```

Keine `--amend` auf Commits, die schon auf `origin` liegen.

## Schritt 5: Push

`git push origin <branch>`, bei einem neuen Branch `git push -u origin <branch>`. Weist der
Server ab, weil er voraus ist: `git pull --rebase`, dann noch einmal pushen. Konflikte werden
nicht still gelöst und nie mit `--force` weggedrückt; dann endet der Skill mit einem Bericht,
und der Nutzer entscheidet.

Zum Schluss `git log origin/<branch> -1` als Beleg, dass der Commit angekommen ist.

## Schritt 6: Zusammenfassung im Chat

Kurz genug für einen Bildschirm. Sie wiederholt nicht die Commit-Message, sondern erzählt die
Sitzung: was gemacht wurde, was die Doku jetzt anders sagt, was offen ist.

```markdown
**Session beendet.** `<hash>` liegt auf origin/<branch>.

**Gemacht**
- zwei bis sechs Punkte: Features, Fixes, Entscheidungen, Erkenntnisse

**Doku**
- je Datei eine Zeile: was sich geändert hat

**Offen**
- was bewusst liegen blieb, und der nächste Schritt laut Plan
```

Gab es nichts zu committen, sagt die Zusammenfassung das im ersten Satz. Sind Tests rot oder
ist der Push gescheitert, steht das im ersten Satz, mit der Ausgabe darunter.
