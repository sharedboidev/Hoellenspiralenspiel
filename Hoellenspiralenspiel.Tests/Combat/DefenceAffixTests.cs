using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

//Verteidigung aus den Affixen von Rüstung und Schild: Obergrenze der Resistenzen, Minderung nach der Rüstung, Krits, Ailments und Reflect
[TestFixture]
public class DefenceAffixTests
{
    private const string Shield    = "Item:Shield";
    private const float  Tolerance = 0.001f;

    private static StatSheet Defender(params (CombatStat Stat, ModificationType Modification, float Value)[] modifiers)
    {
        var sheet = new StatSheet();

        foreach (var (stat, modification, value) in modifiers)
            sheet.AddModifier(new CombatStatModifier(stat, modification, value, Shield));

        return sheet;
    }

    private static (CombatStat, ModificationType, float) Flat(CombatStat stat, float value)
        => (stat, ModificationType.Flat, value);

    private static HitRequest Request(DamageType damageType, float criticalHitChance = 0f)
        => new(100, 100, damageType, SkillKind.Attack, CriticalHitChance: criticalHitChance);

    #region Resistenzen

    [TestCase(50f, 0f, 50f)]
    [TestCase(100f, 0f, 75f)]
    [TestCase(100f, 10f, 85f)]
    [TestCase(100f, 30f, 90f)]
    [TestCase(80f, 30f, 80f)]
    public void Resistenz_ZaehltBisZumMaximumUndNieUeber90(float resistance, float maximumBonus, float expected)
    {
        var defender = Defender(Flat(CombatStat.FireResistance, resistance), Flat(CombatStat.MaxFireResistance, maximumBonus));

        Assert.That(Defences.GetEffectiveResistance(defender, DamageType.Fire), Is.EqualTo(expected));
    }

    [Test]
    public void AlleResistenzen_ZaehlenZuJederEinzelnen()
    {
        var defender = Defender(Flat(CombatStat.AllElementalResistances, 20f), Flat(CombatStat.FrostResistance, 10f));

        Assert.Multiple(() =>
        {
            Assert.That(defender.GetFinal(CombatStat.FireResistance), Is.EqualTo(20f));
            Assert.That(defender.GetFinal(CombatStat.FrostResistance), Is.EqualTo(30f));
            Assert.That(defender.GetFinal(CombatStat.LightningResistance), Is.EqualTo(20f));
        });
    }

    [Test]
    public void AlleMaximalenResistenzen_HebenJedesMaximum()
    {
        var defender = Defender(Flat(CombatStat.AllMaximumResistances, 2f), Flat(CombatStat.MaxLightningResistance, 3f));

        Assert.Multiple(() =>
        {
            Assert.That(Defences.GetMaximumResistance(defender, DamageType.Fire), Is.EqualTo(77f));
            Assert.That(Defences.GetMaximumResistance(defender, DamageType.Frost), Is.EqualTo(77f));
            Assert.That(Defences.GetMaximumResistance(defender, DamageType.Lightning), Is.EqualTo(80f));
        });
    }

    [Test]
    public void Treffer_MindertMitDerResistenzUnterIhremMaximum()
    {
        var defender = Defender(Flat(CombatStat.LightningResistance, 95f), Flat(CombatStat.MaxLightningResistance, 5f));

        var result = HitResolver.Resolve(Request(DamageType.Lightning), defender, Rolls.Create());

        Assert.That(result.FinalDamage, Is.EqualTo(20), "80 % zählen, nicht 95 %");
    }

    #endregion

    #region Physischer Schaden

    [Test]
    public void ZusaetzlicheMinderung_WirktNachDerRuestung()
    {
        var plain   = HitResolver.Resolve(Request(DamageType.Slash), Defender(Flat(CombatStat.Armor, 100f)), Rolls.Create());
        var reduced = HitResolver.Resolve(Request(DamageType.Slash), Defender(Flat(CombatStat.Armor, 100f), Flat(CombatStat.Damagereduction, 50f)), Rolls.Create());

        Assert.That(reduced.FinalDamage, Is.EqualTo(plain.FinalDamage / 2).Within(1), "die Hälfte dessen, was durch die Rüstung kommt");
    }

    [Test]
    public void ZusaetzlicheMinderung_TrifftAuchPierceUndHoertBei90Auf()
    {
        var pierce = HitResolver.Resolve(Request(DamageType.Pierce), Defender(Flat(CombatStat.Damagereduction, 20f)), Rolls.Create());
        var capped = HitResolver.Resolve(Request(DamageType.Pierce), Defender(Flat(CombatStat.Damagereduction, 150f)), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(pierce.FinalDamage, Is.EqualTo(80));
            Assert.That(capped.FinalDamage, Is.EqualTo(10));
        });
    }

    [Test]
    public void ZusaetzlicheMinderung_LaesstElementeInRuhe()
        => Assert.That(HitResolver.Resolve(Request(DamageType.Fire), Defender(Flat(CombatStat.Damagereduction, 50f)), Rolls.Create()).FinalDamage, Is.EqualTo(100));

    #endregion

    #region Krits, Ailments, Reflect

    [Test]
    public void WenigerKritSchaden_KuerztNurDenZusatzschaden()
    {
        var plain   = HitResolver.Resolve(Request(DamageType.Pierce, 100f), Defender(), Rolls.Create(crit: 0f));
        var reduced = HitResolver.Resolve(Request(DamageType.Pierce, 100f), Defender(Flat(CombatStat.ReducedCriticalDamageTaken, 60f)), Rolls.Create(crit: 0f));

        Assert.Multiple(() =>
        {
            Assert.That(plain.FinalDamage, Is.EqualTo(150));
            Assert.That(reduced.FinalDamage, Is.EqualTo(120), "60 % weniger von den 50 % Zusatzschaden");
        });
    }

    [Test]
    public void Ailments_KoennenAbgewehrtWerden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatusEffectRules.IsAvoided(Defender(Flat(CombatStat.AilmentAvoidance, 100f)), Rolls.Create()), Is.True);
            Assert.That(StatusEffectRules.IsAvoided(Defender(Flat(CombatStat.AilmentAvoidance, 30f)), new FixedRandom(0.5f)), Is.False, "Wurf 50 über 30");
            Assert.That(StatusEffectRules.IsAvoided(Defender(Flat(CombatStat.AilmentAvoidance, 30f)), new FixedRandom(0.1f)), Is.True, "Wurf 10 unter 30");
        });
    }

    [Test]
    public void OhneAbwehr_FaelltKeinWurf()
    {
        var random = new FixedRandom(0.5f);

        StatusEffectRules.IsAvoided(Defender(), random);

        Assert.That(random.Draws, Is.Zero);
    }

    [Test]
    public void Reflect_GibtEinenAnteilDesUngemindertenPhysischenSchadens()
    {
        var defender = Defender(Flat(CombatStat.ReflectPhysical, 25f), Flat(CombatStat.Armor, 500f));
        var physical = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());
        var fire     = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());
        var missed   = physical with { Avoidance = HitAvoidance.Dodged };

        Assert.Multiple(() =>
        {
            Assert.That(HitGains.GetReflectedDamage(defender, physical), Is.EqualTo(25f).Within(Tolerance), "aus 100 vor der Rüstung");
            Assert.That(HitGains.GetReflectedDamage(defender, fire), Is.Zero, "nur physisch");
            Assert.That(HitGains.GetReflectedDamage(defender, missed), Is.Zero);
            Assert.That(HitGains.GetReflectedDamage(Defender(), physical), Is.Zero);
        });
    }

    #endregion

    #region Schaden über Zeit je Schadensart

    [Test]
    public void MultiplikatorDerSchadensart_WirktAufIhrenEffekt()
    {
        var attacker = Defender((CombatStat.FireDamageOverTime, ModificationType.More, 0.5f),
                                (CombatStat.DamageOverTime, ModificationType.More, 0.2f));

        var byType = StatusEffectRules.GetDamageOverTimeByType(attacker);
        var plain  = new HitRequest(100, 100, DamageType.Slash, SkillKind.Attack) { AddedDamage = new PerElement<DamageRange>(new DamageRange(100, 100), default, default) };
        var scaled = plain with
        {
            DamageOverTimeMultiplier = StatusEffectRules.GetDamageOverTimeMultiplier(attacker),
            DamageOverTimeByType     = byType
        };

        var plainResult  = HitResolver.Resolve(plain, new StatSheet(), Rolls.Create());
        var scaledResult = HitResolver.Resolve(scaled, new StatSheet(), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(byType, Is.EqualTo(new DamageOverTimeByType(1f, 1.5f, 1f, 1f)));
            Assert.That(scaledResult.AddedEffects.Fire.Magnitude, Is.EqualTo(plainResult.AddedEffects.Fire.Magnitude * 1.2f * 1.5f).Within(Tolerance), "Burn: beide More");
            Assert.That(scaledResult.InflictedEffect.Magnitude, Is.EqualTo(plainResult.InflictedEffect.Magnitude * 1.2f).Within(Tolerance), "Bleed: nur der allgemeine");
        });
    }

    [Test]
    public void PhysischerMultiplikator_WirktAufBleed()
    {
        var byType = StatusEffectRules.GetDamageOverTimeByType(Defender((CombatStat.PhysicalDamageOverTime, ModificationType.More, 0.66f)));

        Assert.Multiple(() =>
        {
            Assert.That(byType.For(DamageType.Slash), Is.EqualTo(1.66f).Within(Tolerance));
            Assert.That(byType.For(DamageType.Fire), Is.EqualTo(1f));
        });
    }

    #endregion
}
