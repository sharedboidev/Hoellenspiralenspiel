using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class StatusEffectTrackerTests
{
    private const double Frame = 1.0 / 60.0;

    private static StatSheet CreateSheet()
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.Movementspeed, 200);
            s.SetBase(CombatStat.Attackspeed, 2);
        });

        return sheet;
    }

    private static List<StatusTick> Run(StatusEffectTracker tracker, double seconds)
    {
        var ticks = new List<StatusTick>();
        var steps = (int)System.Math.Round(seconds / Frame);

        for (var i = 0; i < steps; i++)
            tracker.Advance(Frame, ticks);

        return ticks;
    }

    private static int TotalDamage(IEnumerable<StatusTick> ticks, StatusEffectKind kind)
        => ticks.Where(tick => tick.Kind == kind).Sum(tick => tick.Damage);

    #region Schaden über Zeit

    [Test]
    public void Bleed_VerursachtSeinenSchadenUeberDieDauer()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 4f));

        var firstHalf  = Run(tracker, 2);
        var secondHalf = Run(tracker, 2.1);

        Assert.Multiple(() =>
        {
            Assert.That(TotalDamage(firstHalf, StatusEffectKind.Bleed), Is.EqualTo(10).Within(1));
            Assert.That(TotalDamage(firstHalf, StatusEffectKind.Bleed) + TotalDamage(secondHalf, StatusEffectKind.Bleed), Is.EqualTo(20));
            Assert.That(tracker.IsActive(StatusEffectKind.Bleed), Is.False);
            Assert.That(tracker.HasAny, Is.False);
        });
    }

    [Test]
    public void Schaden_WirdImTaktUndInGanzenZahlenGemeldet()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 0.75f, 4f));

        var ticks = Run(tracker, 4.1);

        Assert.Multiple(() =>
        {
            Assert.That(ticks, Is.Not.Empty);
            Assert.That(ticks.All(tick => tick.Damage >= 1), Is.True, "Takte ohne vollen Schadenspunkt werden nicht gemeldet");
            Assert.That(ticks.Count, Is.LessThanOrEqualTo(8), "höchstens ein Takt je halbe Sekunde");
            Assert.That(TotalDamage(ticks, StatusEffectKind.Bleed), Is.EqualTo(3));
        });
    }

    [Test]
    public void Bleed_NurDerStaerksteWirkt()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 2f, 4f));

        Assert.That(tracker.GetMagnitude(StatusEffectKind.Bleed), Is.EqualTo(5f));
        Assert.That(TotalDamage(Run(tracker, 4.1), StatusEffectKind.Bleed), Is.EqualTo(20));
    }

    [Test]
    public void Bleed_SchwaechererLaeuftWeiterWennDerStaerkereEndet()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 8f, 1f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 2f, 4f));

        var ticks = Run(tracker, 4.1);

        Assert.That(TotalDamage(ticks, StatusEffectKind.Bleed), Is.EqualTo(8 * 1 + 2 * 3));
    }

    [Test]
    public void Bleed_StaerkererErsetztSchwaecheren()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 2f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 4f));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetInstanceCount(StatusEffectKind.Bleed), Is.EqualTo(1));
            Assert.That(tracker.GetMagnitude(StatusEffectKind.Bleed), Is.EqualTo(5f));
        });
    }

    [Test]
    public void Burn_Stapelt()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 3f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 2f, 4f));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetInstanceCount(StatusEffectKind.Burn), Is.EqualTo(2));
            Assert.That(tracker.GetMagnitude(StatusEffectKind.Burn), Is.EqualTo(5f));
        });

        Assert.That(TotalDamage(Run(tracker, 4.1), StatusEffectKind.Burn), Is.EqualTo(20));
    }

    [Test]
    public void Burn_StapelEndenUnabhaengigVoneinander()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 4f, 4f));

        var ticks = Run(tracker, 2);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 4f, 4f));

        ticks.AddRange(Run(tracker, 2));

        Assert.That(tracker.GetInstanceCount(StatusEffectKind.Burn), Is.EqualTo(1), "der erste Stapel ist abgelaufen");

        ticks.AddRange(Run(tracker, 2.1));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.IsActive(StatusEffectKind.Burn), Is.False);
            Assert.That(TotalDamage(ticks, StatusEffectKind.Burn), Is.EqualTo(32));
        });
    }

    [Test]
    public void Burn_HatEineObergrenzeUndBehaeltDieStaerkstenStapel()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 1f, 4f));

        for (var i = 0; i < CombatRules.BurnMaxStacks; i++)
            tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 3f, 4f));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetInstanceCount(StatusEffectKind.Burn), Is.EqualTo(CombatRules.BurnMaxStacks));
            Assert.That(tracker.GetMagnitude(StatusEffectKind.Burn), Is.EqualTo(3f * CombatRules.BurnMaxStacks));
        });
    }

    [Test]
    public void BleedUndBurn_LaufenNebeneinander()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 2f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 3f, 4f));

        var ticks = Run(tracker, 4.1);

        Assert.Multiple(() =>
        {
            Assert.That(TotalDamage(ticks, StatusEffectKind.Bleed), Is.EqualTo(8));
            Assert.That(TotalDamage(ticks, StatusEffectKind.Burn), Is.EqualTo(12));
        });
    }

    #endregion

    #region Chill und Shock

    [Test]
    public void Chill_VerlangsamtBewegungUndAngriff()
    {
        var sheet   = CreateSheet();
        var tracker = new StatusEffectTracker(sheet);
        var speed   = sheet.GetFinal(CombatStat.Movementspeed);
        var attacks = sheet.GetFinal(CombatStat.Attackspeed);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, CombatRules.ChillSlow, CombatRules.ChillDurationSec));

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(speed * 0.7f).Within(0.001f));
            Assert.That(sheet.GetFinal(CombatStat.Attackspeed), Is.EqualTo(attacks * 0.7f).Within(0.001f));
        });

        Run(tracker, CombatRules.ChillDurationSec + 0.1);

        Assert.Multiple(() =>
        {
            Assert.That(tracker.IsActive(StatusEffectKind.Chill), Is.False);
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(speed).Within(0.001f));
            Assert.That(sheet.GetFinal(CombatStat.Attackspeed), Is.EqualTo(attacks).Within(0.001f));
            Assert.That(sheet.Modifiers, Is.Empty);
        });
    }

    [Test]
    public void Chill_ErneuterTrefferVerlaengertDieDauerUndStapeltNicht()
    {
        var sheet   = CreateSheet();
        var tracker = new StatusEffectTracker(sheet);
        var speed   = sheet.GetFinal(CombatStat.Movementspeed);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));
        Run(tracker, 2);
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetInstanceCount(StatusEffectKind.Chill), Is.EqualTo(1));
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(speed * 0.7f).Within(0.001f));
        });

        Run(tracker, 2);

        Assert.That(tracker.IsActive(StatusEffectKind.Chill), Is.True, "die Dauer zählt ab dem zweiten Treffer");

        Run(tracker, 1.1);

        Assert.That(tracker.IsActive(StatusEffectKind.Chill), Is.False);
    }

    [Test]
    public void Chill_RechnetDasBlattNurBeiAenderungNeu()
    {
        var sheet   = CreateSheet();
        var tracker = new StatusEffectTracker(sheet);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));

        var recalculations = sheet.RecalculationCount;

        Run(tracker, 1);
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));
        Run(tracker, 1);

        Assert.That(sheet.RecalculationCount, Is.EqualTo(recalculations));
    }

    [Test]
    public void Shock_LiefertDieChanceAufFehlschlag()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        Assert.That(tracker.ActionFailureChance, Is.Zero);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Shock, CombatRules.ShockActionFailureChance, CombatRules.ShockDurationSec));

        Assert.That(tracker.ActionFailureChance, Is.EqualTo(CombatRules.ShockActionFailureChance));

        var ticks = Run(tracker, CombatRules.ShockDurationSec + 0.1);

        Assert.Multiple(() =>
        {
            Assert.That(tracker.ActionFailureChance, Is.Zero);
            Assert.That(ticks, Is.Empty, "Shock verursacht keinen Schaden");
        });
    }

    #endregion

    #region Verwaltung

    [Test]
    public void Ereignisse_MeldenBeginnUndEndeJeEinmal()
    {
        var tracker = new StatusEffectTracker(CreateSheet());
        var started = new List<StatusEffectKind>();
        var ended   = new List<StatusEffectKind>();

        tracker.Started += started.Add;
        tracker.Ended   += ended.Add;

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 1f, 2f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 1f, 2f));

        Run(tracker, 2.1);

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.EqualTo(new[] { StatusEffectKind.Burn }));
            Assert.That(ended, Is.EqualTo(new[] { StatusEffectKind.Burn }));
        });
    }

    [Test]
    public void Clear_EntferntAlleEffekteUndIhreModifier()
    {
        var sheet   = CreateSheet();
        var tracker = new StatusEffectTracker(sheet);
        var speed   = sheet.GetFinal(CombatStat.Movementspeed);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Shock, 0.25f, 4f));

        tracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.HasAny, Is.False);
            Assert.That(tracker.ActionFailureChance, Is.Zero);
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(speed).Within(0.001f));
            Assert.That(Run(tracker, 4), Is.Empty);
        });
    }

    [Test]
    public void Effekt_OhneStaerkeOderDauerWirdIgnoriert()
    {
        var tracker = new StatusEffectTracker(CreateSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 0f, 4f));
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 0f));

        Assert.That(tracker.HasAny, Is.False);
    }

    [Test]
    public void FremdeModifier_BleibenBeimEndeEinesEffektsErhalten()
    {
        var sheet   = CreateSheet();
        var tracker = new StatusEffectTracker(sheet);

        sheet.AddModifier(new CombatStatModifier(CombatStat.Movementspeed, ModificationType.Flat, 50, "Item:Boots"));

        var speed = sheet.GetFinal(CombatStat.Movementspeed);

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));
        Run(tracker, 3.1);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.Modifiers, Has.Count.EqualTo(1));
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(speed).Within(0.001f));
        });
    }

    #endregion
}
