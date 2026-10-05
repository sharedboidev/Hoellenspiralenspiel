using System;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

//Erhöhter Schaden über Zeit addiert sich, der Multiplikator auf ihn ist ein More-Modifier
[TestFixture]
public class DamageOverTimeTests
{
    private const string Ring      = "Item:Ring";
    private const float  Tolerance = 0.001f;

    private static StatSheet Attacker(params CombatStatModifier[] modifiers)
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            s.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
        });

        sheet.AddModifiers(modifiers);

        return sheet;
    }

    private static CombatStatModifier DamageOverTime(ModificationType modification, float value)
        => new(CombatStat.DamageOverTime, modification, value, Ring);

    [Test]
    public void ErhoehtUndMultiplikator_WirkenZusammen()
    {
        var attacker = Attacker(DamageOverTime(ModificationType.Percentage, 0.3f),
                                DamageOverTime(ModificationType.Percentage, 0.2f),
                                DamageOverTime(ModificationType.More, 0.2f));

        Assert.That(StatusEffectRules.GetDamageOverTimeMultiplier(attacker), Is.EqualTo(1.5f * 1.2f).Within(Tolerance));
    }

    [Test]
    public void ZweiMultiplikatoren_MultiplizierenSich()
    {
        var attacker = Attacker(DamageOverTime(ModificationType.More, 0.1f), DamageOverTime(ModificationType.More, 0.2f));

        Assert.That(StatusEffectRules.GetDamageOverTimeMultiplier(attacker), Is.EqualTo(1.1f * 1.2f).Within(Tolerance));
    }

    [Test]
    public void AngriffUndZauber_TragenDenMultiplikatorDesAngreifers()
    {
        var attacker = Attacker(DamageOverTime(ModificationType.More, 0.25f));
        var weapon   = new WeaponProfile(10, 20, 1f, 0f, DamageType.Slash, WeaponProfile.DefaultMeleeRange);
        var spell    = new SpellDefinition("Fireball", 10f, 20f, DamageType.Fire, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(HitRequests.ForAttack(attacker, weapon, AttackDefinition.Standard).DamageOverTimeMultiplier, Is.EqualTo(1.25f).Within(Tolerance));
            Assert.That(HitRequests.ForSpell(attacker, spell).DamageOverTimeMultiplier, Is.EqualTo(1.25f).Within(Tolerance));
            Assert.That(HitRequests.ForSpell(Attacker(), spell).DamageOverTimeMultiplier, Is.EqualTo(1f));
        });
    }

    [TestCase(DamageType.Slash, StatusEffectKind.Bleed)]
    [TestCase(DamageType.Fire, StatusEffectKind.Burn)]
    public void Multiplikator_ErhoehtJedenEffektMitSchaden(DamageType damageType, StatusEffectKind kind)
    {
        var plain   = StatusEffectRules.GetEffectOfHit(damageType, 100f, 80);
        var boosted = StatusEffectRules.GetEffectOfHit(damageType, 100f, 80, 1.5f);

        Assert.Multiple(() =>
        {
            Assert.That(boosted.Kind, Is.EqualTo(kind));
            Assert.That(boosted.Magnitude, Is.EqualTo(plain.Magnitude * 1.5f).Within(Tolerance));
            Assert.That(boosted.DurationSec, Is.EqualTo(plain.DurationSec), "länger dauert er nicht");
        });
    }

    [TestCase(DamageType.Lightning)]
    [TestCase(DamageType.Frost)]
    public void Multiplikator_LaesstEffekteOhneSchadenUnberuehrt(DamageType damageType)
        => Assert.That(StatusEffectRules.GetEffectOfHit(damageType, 100f, 80, 3f), Is.EqualTo(StatusEffectRules.GetEffectOfHit(damageType, 100f, 80)));

    //Wer einen neuen Effekt mit Schaden einführt, muss ihm eine Regel für Schaden über Zeit geben, sonst griffe der Multiplikator nicht
    [Test]
    public void JederEffektMitSchaden_HatEineRegelFuerSchadenUeberZeit()
    {
        var damaging = Enum.GetValues<StatusEffectKind>().Where(kind => StatusEffectRules.Get(kind).DealsDamage);
        var withRule = Enum.GetValues<DamageType>().Select(StatusEffectRules.FindDamageOverTime).Where(rule => rule is not null).Select(rule => rule.Kind);

        Assert.That(withRule, Is.EquivalentTo(damaging));
    }

    [Test]
    public void Treffer_GibtDenMultiplikatorAnDenBleedWeiter()
    {
        var plain   = new HitRequest(100, 100, DamageType.Slash, SkillKind.Attack);
        var boosted = plain with { DamageOverTimeMultiplier = 2f };

        var plainBleed   = HitResolver.Resolve(plain, new StatSheet(), Rolls.Create()).InflictedEffect;
        var boostedBleed = HitResolver.Resolve(boosted, new StatSheet(), Rolls.Create()).InflictedEffect;

        Assert.That(boostedBleed.Magnitude, Is.EqualTo(plainBleed.Magnitude * 2f).Within(Tolerance));
    }

    [Test]
    public void Schaetzung_RechnetDenMultiplikatorEin()
    {
        var weapon   = new WeaponProfile(100, 200, 1f, 0f, DamageType.Slash, WeaponProfile.DefaultMeleeRange);
        var skill    = SkillDefinition.ForAttack("attack", AttackDefinition.Standard);
        var plain    = Attacker();
        var boosted  = Attacker(DamageOverTime(ModificationType.More, 0.5f));

        weapon.ApplyTo(plain);
        weapon.ApplyTo(boosted);

        var plainEstimate   = SkillDamageEstimator.Estimate(plain, weapon, skill);
        var boostedEstimate = SkillDamageEstimator.Estimate(boosted, weapon, skill);

        Assert.Multiple(() =>
        {
            Assert.That(boostedEstimate.EffectDps, Is.EqualTo(plainEstimate.EffectDps * 1.5f).Within(0.01f));
            Assert.That(boostedEstimate.HitDps, Is.EqualTo(plainEstimate.HitDps), "der Treffer selbst bleibt gleich");
        });
    }
}
