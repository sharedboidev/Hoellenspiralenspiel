using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Balance;

//Die Matrix rechnet mit den echten Resources. Ihre Bänder sind absichtlich breit: Sie fangen grobe Fehler,
//die Feinarbeit an den Zahlen gehört in Etappe 12 von M8
[TestFixture]
public class BalanceReportTests
{
    private const int Runs = 200;

    private static readonly int[] Levels = [1, 3, 5, 7];

    private static readonly string[] Spells = ["fireball", "frost_nova", "thunderbolt"];

    //Ein Held auf Level N hat N - 1 Punkte verteilt, abwechselnd auf Stärke und Konstitution, und trägt das Trainingsschwert wie ein neuer Charakter
    internal static Fighter HeroAt(int level, params SkillDefinition[] skills)
    {
        var points = level - 1;
        var values = new HeroBaseValues
        {
            Strength      = 1 + (points + 1) / 2,
            Constitution  = 1 + points / 2,
            Movementspeed = 500f
        };

        return Fighter.Hero(values, [GameData.Weapon("training_sword")], skills.Length == 0 ? null : [..skills, Fighter.StandardAttack], $"Hero {level}");
    }

    [Test]
    public void NeuerHeld_ErschlaegtEinSkelettLevel1_In2Bis15Sekunden()
    {
        var summary = FightSimulator.Summarize(HeroAt(1), Fighter.Enemy(GameData.Enemy("skeleton"), 1), 50);

        Assert.Multiple(() =>
        {
            Assert.That(summary.AllKilled, Is.True);
            Assert.That(summary.MeanSeconds, Is.InRange(2.0, 15.0));
        });
    }

    [Test]
    public void SkelettLevel1_BrauchtFuerEinenNeuenHelden8Bis40Sekunden()
    {
        var summary = FightSimulator.Summarize(Fighter.Enemy(GameData.Enemy("skeleton"), 1), HeroAt(1), 50);

        Assert.Multiple(() =>
        {
            Assert.That(summary.AllKilled, Is.True);
            Assert.That(summary.MeanSeconds, Is.InRange(8.0, 40.0));
        });
    }

    [Test]
    public void SkeletonKingLevel7_HaeltDemSchwertDesHeldenAufLevel7_30Bis300SekundenStand()
    {
        var summary = FightSimulator.Summarize(HeroAt(7), Fighter.Enemy(GameData.Enemy("skeleton_king"), 7), 20);

        Assert.Multiple(() =>
        {
            Assert.That(summary.AllKilled, Is.True);
            Assert.That(summary.MeanSeconds, Is.InRange(30.0, 300.0));
        });
    }

    [Test]
    public void JederGegner_FaelltAufLevel1_DurchDasSchwertEinesNeuenHelden()
    {
        foreach (var enemy in GameData.AllEnemies())
            Assert.That(FightSimulator.Summarize(HeroAt(1), Fighter.Enemy(enemy, 1), 10).AllKilled, Is.True, enemy.Id);
    }

    //Eine Tabelle je Level, darin vom schnellsten zum langsamsten Kampf mit dem Schwert. Die Spalten stehen bündig, die Ausgabe bleibt Markdown
    [Test]
    [Explicit("Druckt die Matrix für die Roadmap, rechnet einige Sekunden")]
    public void Matrix_Drucken()
    {
        var spells = Spells.Select(GameData.PlayerSkill).ToList();
        var rows   = new List<MatrixRow>();
        var bosses = new List<string>();

        foreach (var enemy in GameData.AllEnemies())
        {
            if (enemy.IsBoss && enemy.FixedMods.Count > 0)
                bosses.Add($"{enemy.Name} mit {string.Join(", ", enemy.FixedMods.Select(mod => mod.Name))}");

            foreach (var level in Levels)
            {
                var foe  = Fighter.Enemy(enemy, level);
                var hero = HeroAt(level);

                rows.Add(new MatrixRow(level,
                                       enemy.IsBoss ? $"{enemy.Name} (Boss)" : enemy.Name,
                                       foe.CreateStats().GetFinalWhole(CombatStat.Life),
                                       FightSimulator.Summarize(hero, foe, Runs),
                                       spells.Select(spell => FightSimulator.Summarize(HeroAt(level, spell), foe, Runs)).ToList(),
                                       FightSimulator.Summarize(foe, hero, Runs)));
            }
        }

        string[] header = ["Gegner", "Leben", "Schwert", "Schläge", ..spells.Select(spell => spell.Name), "Gegner tötet Helden"];

        var ordered = rows.OrderBy(row => row.Level).ThenBy(row => row.Sword.MeanSeconds).ThenBy(row => row.Name, StringComparer.Ordinal).ToList();
        var table   = new AlignedTable(header, ordered.Select(row => row.ToCells()).ToList(), firstRightAligned: 1, timeColumns: [2, ..Enumerable.Range(4, spells.Count + 1)]);
        var report  = new StringBuilder();

        report.AppendLine("Kampfmatrix aus dem Simulator");
        report.AppendLine();
        report.AppendLine("Der Held hat das Level des Gegners, das Trainingsschwert und seine Punkte abwechselnd auf Stärke und Konstitution verteilt.");
        report.AppendLine($"Zeiten in Sekunden bis zum Tod, Mittel aus {Runs} Seeds, in Klammern der kürzeste und der längste Kampf. Schläge zählt die Schwerthiebe im Mittel.");
        report.AppendLine("Mit einem Zauber wirkt der Held ihn, solange das Mana reicht, sonst schlägt er mit dem Schwert. \"nie\" heißt: nicht binnen 600 Sekunden.");

        if (bosses.Count > 0)
            report.AppendLine($"Bosse kämpfen mit ihren festen Mods, gerechnet werden nur deren Modifier: {string.Join("; ", bosses)}.");

        foreach (var level in Levels)
        {
            report.AppendLine();
            report.AppendLine($"Level {level}");
            report.AppendLine();
            report.Append(table.Render(ordered.Select((row, index) => (row, index)).Where(entry => entry.row.Level == level).Select(entry => entry.index)));
        }

        TestContext.Out.WriteLine(report.ToString());
    }

    private sealed record MatrixRow(int Level, string Name, int Life, FightSummary Sword, IReadOnlyList<FightSummary> Spells, FightSummary Against)
    {
        private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

        public string[] ToCells()
            => [Name, Life.ToString(German), Time(Sword), Sword.Kills == 0 ? "" : Sword.MeanActions.ToString("0.0", German), ..Spells.Select(Time), Time(Against)];

        //Mittel und Spanne trennt " (", daran richtet AlignedTable die Zeitspalten aus
        private static string Time(FightSummary summary)
        {
            if (summary.Kills == 0)
                return "nie";

            var text = string.Create(German, $"{summary.MeanSeconds:0.0} ({summary.MinSeconds:0.0}–{summary.MaxSeconds:0.0})");

            return summary.AllKilled ? text : $"{text} {summary.Kills}/{summary.Runs}";
        }
    }

    //Markdown-Tabelle mit gleich breiten Spalten. Ab firstRightAligned stehen die Zahlen rechtsbündig,
    //in den Zeitspalten stehen zusätzlich die Mittelwerte untereinander und die Klammern beginnen in einer Flucht
    private sealed class AlignedTable
    {
        private const string TimeSeparator = " (";

        private readonly string[]   header;
        private readonly string[][] cells;
        private readonly int[]      widths;
        private readonly int        firstRightAligned;

        public AlignedTable(string[] header, IReadOnlyList<string[]> rows, int firstRightAligned, int[] timeColumns)
        {
            this.header            = header;
            this.firstRightAligned = firstRightAligned;

            cells = rows.Select(row => row.ToArray()).ToArray();

            foreach (var column in timeColumns)
                AlignTimes(column);

            widths = Enumerable.Range(0, header.Length)
                               .Select(column => cells.Select(row => row[column].Length).Append(header[column].Length).Max())
                               .ToArray();
        }

        public string Render(IEnumerable<int> rowIndices)
        {
            var text = new StringBuilder();

            text.AppendLine(Line(header));
            text.AppendLine($"|{string.Join("|", widths.Select((width, column) => column >= firstRightAligned ? new string('-', width + 1) + ":" : new string('-', width + 2)))}|");

            foreach (var index in rowIndices)
                text.AppendLine(Line(cells[index]));

            return text.ToString();
        }

        private string Line(string[] values)
            => $"| {string.Join(" | ", values.Select((value, column) => column >= firstRightAligned ? value.PadLeft(widths[column]) : value.PadRight(widths[column])))} |";

        private void AlignTimes(int column)
        {
            var parts = cells.Select(row => Split(row[column])).ToArray();
            var left  = parts.Max(part => part.Mean.Length);
            var right = parts.Max(part => part.Range.Length);

            for (var i = 0; i < cells.Length; i++)
                cells[i][column] = parts[i].Mean.PadLeft(left) + parts[i].Range.PadRight(right);
        }

        private static (string Mean, string Range) Split(string value)
        {
            var at = value.IndexOf(TimeSeparator, StringComparison.Ordinal);

            return at < 0 ? (value, "") : (value[..at], value[at..]);
        }
    }
}
