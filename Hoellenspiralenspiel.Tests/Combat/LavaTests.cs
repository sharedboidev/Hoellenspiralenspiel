using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

//Die Regeln hat der User am 08.10.2026 gewählt: Der Lavariss brennt als Schaden über Zeit ohne Trefferwurf, kein Ausweichen, kein Krit, kein Burn.
//Feuerresistenz und die Multiplikatoren für Schaden über Zeit zählen, mehrere Risse unter einem Gegner stapeln nicht
[TestFixture]
public class LavaTests
{
    private const double Frame     = 1.0 / 60.0;
    private const float  Tolerance = 0.01f;

    private static HitRequest PerSecond(float min = 100f, float max = 140f, float addedLightning = 0f)
        => new(min, max, DamageType.Fire, SkillKind.Attack)
        {
            AddedDamage = new PerElement<DamageRange>(default, default, new DamageRange(addedLightning, addedLightning))
        };

    private static StatSheet Defender(float fireResistance = 0f)
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.FireResistance, fireResistance);

        return sheet;
    }

    private static List<StatusTick> Stand(StatusEffectTracker tracker, IEnumerable<StatusEffectApplication> perFrame, double seconds)
    {
        var ticks = new List<StatusTick>();
        var steps = (int)System.Math.Round(seconds / Frame);

        for (var i = 0; i < steps; i++)
        {
            foreach (var application in perFrame)
                tracker.Apply(application);

            tracker.Advance(Frame, ticks);
        }

        return ticks;
    }

    [Test]
    public void Lava_MachtDenMittlerenSchadenJeSekunde_GanzAlsFeuer()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GroundDamage.GetDamagePerSecond(PerSecond(), DamageType.Fire), Is.EqualTo(120f).Within(Tolerance));
            Assert.That(GroundDamage.GetDamagePerSecond(PerSecond(addedLightning: 30f), DamageType.Fire), Is.EqualTo(150f).Within(Tolerance), "der Zusatzschaden brennt mit");
        });
    }

    [Test]
    public void Lava_WaechstMitSchadenUeberZeit()
    {
        var request = PerSecond() with
        {
            DamageOverTimeMultiplier = 1.5f,
            DamageOverTimeByType = new DamageOverTimeByType(3f, 2f, 1f, 1f)
        };

        Assert.That(GroundDamage.GetDamagePerSecond(request, DamageType.Fire), Is.EqualTo(120f * 1.5f * 2f).Within(Tolerance), "der physische Multiplikator zählt nicht");
    }

    [Test]
    public void Lava_WirdVonDerFeuerresistenzGemindert()
    {
        var defender   = Defender(50f);
        var resistance = Defences.GetEffectiveResistance(defender, DamageType.Fire);
        var effect     = GroundDamage.Create(StatusEffectKind.Lava, PerSecond(), defender, "Held");

        Assert.Multiple(() =>
        {
            Assert.That(resistance, Is.GreaterThanOrEqualTo(50f));
            Assert.That(effect.Kind, Is.EqualTo(StatusEffectKind.Lava));
            Assert.That(effect.Magnitude, Is.EqualTo(120f * (1f - resistance / 100f)).Within(Tolerance));
            Assert.That(effect.DurationSec, Is.EqualTo(CombatRules.GroundDamageLingerSec));
            Assert.That(effect.Source, Is.EqualTo("Held"));
        });
    }

    [Test]
    public void Lava_IstKeinAilment_UndMachtFeuerschaden()
    {
        var rule = StatusEffectRules.Get(StatusEffectKind.Lava);

        Assert.Multiple(() =>
        {
            Assert.That(rule.IsAilment, Is.False, "Ailment-Vermeidung schützt nicht vor dem Boden");
            Assert.That(rule.DealsDamage, Is.True);
            Assert.That(rule.Stacking, Is.EqualTo(StackingRule.Strongest));
            Assert.That(StatusEffectRules.GetDamageType(StatusEffectKind.Lava), Is.EqualTo(DamageType.Fire));
            Assert.That(StatusEffectRules.GetDamageType(StatusEffectKind.Burn), Is.EqualTo(DamageType.Fire));
            Assert.That(StatusEffectRules.GetDamageType(StatusEffectKind.Chill), Is.Null);
            Assert.That(StatusEffectRules.Get(StatusEffectKind.Brittle).IsAilment, Is.True);
            Assert.That(StatusEffectRules.FindDamageOverTime(DamageType.Fire)?.Kind, Is.EqualTo(StatusEffectKind.Burn), "ein Feuertreffer legt weiter Burn, keine Lava");
        });
    }

    [Test]
    public void AufDemRiss_BrenntEsJeSekundeDenVollenWert()
    {
        var sheet   = Defender();
        var tracker = new StatusEffectTracker(sheet);
        var lava    = GroundDamage.Create(StatusEffectKind.Lava, PerSecond(), sheet);

        var ticks = Stand(tracker, [lava], 2.0);

        Assert.Multiple(() =>
        {
            Assert.That(ticks.All(tick => tick.Kind == StatusEffectKind.Lava), Is.True);
            Assert.That(ticks.Sum(tick => tick.Damage), Is.EqualTo(240).Within(2));
        });
    }

    [Test]
    public void ZweiRisseUntermGegner_StapelnNicht()
    {
        var sheet   = Defender();
        var tracker = new StatusEffectTracker(sheet);
        var weak    = GroundDamage.Create(StatusEffectKind.Lava, PerSecond(50f, 50f), sheet);
        var strong  = GroundDamage.Create(StatusEffectKind.Lava, PerSecond(), sheet);

        var ticks = Stand(tracker, [strong, weak, strong], 2.0);

        Assert.That(ticks.Sum(tick => tick.Damage), Is.EqualTo(240).Within(2), "nur der stärkste Riss zählt");
    }

    [Test]
    public void VomRissRunter_BrenntEsNurKurzNach()
    {
        var sheet   = Defender();
        var tracker = new StatusEffectTracker(sheet);

        Stand(tracker, [GroundDamage.Create(StatusEffectKind.Lava, PerSecond(), sheet)], 1.0);

        var after = Stand(tracker, [], 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(after.Sum(tick => tick.Damage), Is.LessThanOrEqualTo((int)System.Math.Ceiling(120f * CombatRules.GroundDamageLingerSec) + 1));
            Assert.That(tracker.IsActive(StatusEffectKind.Lava), Is.False);
        });
    }

    [Test]
    public void EinTickLava_LoestBrittle()
    {
        var sheet   = Defender();
        var tracker = new StatusEffectTracker(sheet);

        tracker.Apply(StatusEffectRules.CreateApplication(StatusEffectKind.Brittle));

        Stand(tracker, [GroundDamage.Create(StatusEffectKind.Lava, PerSecond(), sheet)], 0.6);

        Assert.That(tracker.IsActive(StatusEffectKind.Brittle), Is.False);
    }
}
