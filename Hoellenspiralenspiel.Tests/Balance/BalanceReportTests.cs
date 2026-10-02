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

    [Test]
    [Explicit("Druckt die Matrix für die Roadmap, rechnet einige Sekunden")]
    public void Matrix_Drucken()
    {
        var report = new StringBuilder();
        var spells = Spells.Select(GameData.PlayerSkill).ToList();

        report.AppendLine($"Held auf gleichem Level mit Trainingsschwert, Punkte abwechselnd auf Stärke und Konstitution. Mittel aus {Runs} Seeds, in Klammern kürzester und längster Kampf.");
        report.AppendLine("Zauber: der Held wirkt den Zauber, solange das Mana reicht, sonst schlägt er mit dem Schwert.");
        report.AppendLine();
        report.AppendLine($"| Gegner | Leben | Held tötet mit Schwert | {string.Join(" | ", spells.Select(spell => $"mit {spell.Name}"))} | Gegner tötet Helden |");
        report.AppendLine($"|---|---|---|{string.Concat(spells.Select(_ => "---|"))}---|");

        foreach (var enemy in GameData.AllEnemies())
        {
            foreach (var level in Levels)
            {
                var foe     = Fighter.Enemy(enemy, level);
                var hero    = HeroAt(level);
                var life    = foe.CreateStats().GetFinalWhole(CombatStat.Life);
                var sword   = FightSimulator.Summarize(hero, foe, Runs);
                var magic   = spells.Select(spell => FightSimulator.Summarize(HeroAt(level, spell), foe, Runs));
                var against = FightSimulator.Summarize(foe, hero, Runs);

                report.AppendLine($"| {foe.Name} | {life} | {Describe(sword, true)} | {string.Join(" | ", magic.Select(summary => Describe(summary, false)))} | {Describe(against, false)} |");
            }
        }

        TestContext.Out.WriteLine(report.ToString());
    }

    private static string Describe(FightSummary summary, bool withHits)
    {
        if (summary.Kills == 0)
            return "nie";

        var text = string.Create(CultureInfo.GetCultureInfo("de-DE"), $"{summary.MeanSeconds:0.0} s ({summary.MinSeconds:0.0} bis {summary.MaxSeconds:0.0})");

        if (withHits)
            text += string.Create(CultureInfo.GetCultureInfo("de-DE"), $", {summary.MeanActions:0.0} Schläge");

        return summary.AllKilled ? text : $"{text}, {summary.Kills} von {summary.Runs}";
    }
}
