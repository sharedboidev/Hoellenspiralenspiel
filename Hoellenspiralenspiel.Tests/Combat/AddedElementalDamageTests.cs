using System;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

//Zusatzschaden der Elemente auf einer Waffe, etwa aus "Adds 1 to 3 Fire Damage"
[TestFixture]
public class AddedElementalDamageTests
{
    private const string Ring      = "Item:Ring";
    private const float  Tolerance = 0.001f;

    private static readonly WeaponProfile FireSword = new WeaponProfile(12, 28, 1.14f, 5, DamageType.Slash, WeaponProfile.DefaultMeleeRange)
    {
        AddedDamage = new PerElement<DamageRange>(new DamageRange(10, 20), default, new DamageRange(2, 30))
    };

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

    private static StatSheet Defender(params (CombatStat Stat, float Value)[] baseValues)
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            foreach (var (stat, value) in baseValues)
                s.SetBase(stat, value);
        });

        return sheet;
    }

    private static CombatStatModifier Increased(CombatStat stat, float value)
        => new(stat, ModificationType.Percentage, value, Ring);

    //Pierce geht an der Rüstung vorbei, so bleibt der Hauptteil ungemindert
    private static HitRequest PierceWithFire(float fireMin = 100f, float fireMax = 100f, float criticalHitChance = 0f)
        => new HitRequest(100, 100, DamageType.Pierce, SkillKind.Attack, CriticalHitChance: criticalHitChance)
        {
            AddedDamage = new PerElement<DamageRange>(new DamageRange(fireMin, fireMax), default, default)
        };

    #region Treffer bauen

    [Test]
    public void Zusatzschaden_WaechstMitDemWaffenschadenDesSkills()
    {
        var attacker   = Attacker();
        var multiplier = attacker.GetTotalMultiplier(CombatStat.ElementalDamage);
        var cleave     = new AttackDefinition("Cleave", 150f);

        var request = HitRequests.ForAttack(attacker, FireSword, cleave);

        Assert.Multiple(() =>
        {
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Slash));
            Assert.That(request.AddedDamage.Fire.Min, Is.EqualTo(10f * 1.5f * multiplier).Within(Tolerance));
            Assert.That(request.AddedDamage.Fire.Max, Is.EqualTo(20f * 1.5f * multiplier).Within(Tolerance));
            Assert.That(request.AddedDamage.Frost.IsEmpty, Is.True);
            Assert.That(request.AddedDamage.Lightning.Max, Is.EqualTo(30f * 1.5f * multiplier).Within(Tolerance));
        });
    }

    [Test]
    public void ErhoehungenEinesElements_ZaehlenZusammen()
    {
        var attacker = Attacker(Increased(CombatStat.ElementalDamage, 0.2f),
                                Increased(CombatStat.FireDamage, 0.3f),
                                Increased(CombatStat.ElementalAttackDamage, 0.5f));

        var elemental = attacker.GetIncreasedMultiplier(CombatStat.ElementalDamage);
        var more      = attacker.GetMoreMultiplier(CombatStat.ElementalDamage);

        var request = HitRequests.ForAttack(attacker, FireSword, AttackDefinition.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(request.AddedDamage.Fire.Max, Is.EqualTo(20f * (elemental + 0.3f + 0.5f) * more).Within(Tolerance), "Feuer: Elementar, Feuer und Angriff");
            Assert.That(request.AddedDamage.Lightning.Max, Is.EqualTo(30f * (elemental + 0.5f) * more).Within(Tolerance), "Blitz: ohne die Erhöhung des Feuers");
        });
    }

    [Test]
    public void PhysischerHauptteil_WaechstNichtMitDenElementen()
    {
        var plain    = HitRequests.ForAttack(Attacker(), FireSword, AttackDefinition.Standard);
        var boosted  = HitRequests.ForAttack(Attacker(Increased(CombatStat.ElementalAttackDamage, 0.5f), Increased(CombatStat.FireDamage, 0.5f)), FireSword, AttackDefinition.Standard);

        Assert.That(boosted.MaxDamage, Is.EqualTo(plain.MaxDamage));
    }

    [Test]
    public void ElementarschadenMitAngriffen_WirktNichtAufZauber()
    {
        var spell    = new SpellDefinition("Fireball", 10f, 20f, DamageType.Fire, 0f);
        var plain    = HitRequests.ForSpell(Attacker(), spell);
        var attacks  = HitRequests.ForSpell(Attacker(Increased(CombatStat.ElementalAttackDamage, 0.5f)), spell);
        var fire     = HitRequests.ForSpell(Attacker(Increased(CombatStat.FireDamage, 0.5f)), spell);
        var spellBase = Attacker();
        var expected = 20f * spellBase.GetTotalMultiplier(CombatStat.SpellDamage) * (spellBase.GetIncreasedMultiplier(CombatStat.ElementalDamage) + 0.5f) * spellBase.GetMoreMultiplier(CombatStat.ElementalDamage);

        Assert.Multiple(() =>
        {
            Assert.That(attacks.MaxDamage, Is.EqualTo(plain.MaxDamage), "nur Angriffe");
            Assert.That(fire.MaxDamage, Is.EqualTo(expected).Within(Tolerance), "erhöhter Feuerschaden wirkt auch auf Zauber");
        });
    }

    [Test]
    public void ElementarschadenMitAngriffen_WirktAufEinenGewandeltenAngriff()
    {
        var strike   = new AttackDefinition("Lightning Strike", 100f, DamageType.Lightning);
        var plain    = HitRequests.ForAttack(Attacker(), FireSword, strike);
        var boosted  = HitRequests.ForAttack(Attacker(Increased(CombatStat.ElementalAttackDamage, 0.5f)), FireSword, strike);
        var baseline = Attacker();
        var ratio    = (baseline.GetIncreasedMultiplier(CombatStat.ElementalDamage) + 0.5f) / baseline.GetIncreasedMultiplier(CombatStat.ElementalDamage);

        Assert.That(boosted.MaxDamage, Is.EqualTo(plain.MaxDamage * ratio).Within(Tolerance));
    }

    [Test]
    public void GewandelterAngriff_NimmtDenZusatzschadenSeinesElementsInDenHauptteil()
    {
        var attacker   = Attacker();
        var multiplier = attacker.GetTotalMultiplier(CombatStat.ElementalDamage);
        var strike     = new AttackDefinition("Lightning Strike", 100f, DamageType.Lightning);

        var request = HitRequests.ForAttack(attacker, FireSword, strike);

        Assert.Multiple(() =>
        {
            Assert.That(request.MinDamage, Is.EqualTo((12f + 2f) * multiplier).Within(Tolerance));
            Assert.That(request.MaxDamage, Is.EqualTo((28f + 30f) * multiplier).Within(Tolerance));
            Assert.That(request.AddedDamage.Lightning.IsEmpty, Is.True, "kein zweiter Blitz neben dem Hauptteil");
            Assert.That(request.AddedDamage.Fire.IsEmpty, Is.False, "Feuer bleibt Feuer");
        });
    }

    [Test]
    public void WaffeOhneZusatzschaden_BautDenselbenTrefferWieBisher()
    {
        var plainSword = FireSword with { AddedDamage = default };

        var request = HitRequests.ForAttack(Attacker(), plainSword, AttackDefinition.Standard);

        Assert.That(request.AddedDamage, Is.EqualTo(default(PerElement<DamageRange>)));
    }

    #endregion

    #region Treffer auflösen

    [Test]
    public void Zusatzschaden_WirdJeElementGemindert()
    {
        var result = HitResolver.Resolve(PierceWithFire(), Defender((CombatStat.FireResistance, 25)), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.AddedDamage.Fire, Is.EqualTo(75));
            Assert.That(result.FinalDamage, Is.EqualTo(175), "Hauptteil und Feuer zusammen");
            Assert.That(result.PhysicalDamage, Is.EqualTo(100));
        });
    }

    [Test]
    public void Zusatzschaden_TrifftMitDemselbenWurfUndKritisch()
    {
        var result = HitResolver.Resolve(PierceWithFire(0f, 100f, 100f), Defender(), Rolls.Create(crit: 0f, damage: 0.5f));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.AddedDamage.Fire, Is.EqualTo(75), "Mitte der Spanne, mal 1,5 für den Krit");
        });
    }

    [Test]
    public void Zusatzschaden_LoestDenEffektSeinesElementsAus()
    {
        var request = new HitRequest(100, 100, DamageType.Slash, SkillKind.Attack)
        {
            AddedDamage = new PerElement<DamageRange>(new DamageRange(40, 40), new DamageRange(10, 10), default)
        };

        var result = HitResolver.Resolve(request, Defender(), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.InflictedEffect.Kind, Is.EqualTo(StatusEffectKind.Bleed));
            Assert.That(result.AddedEffects.Fire.Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.AddedEffects.Fire.Magnitude * result.AddedEffects.Fire.DurationSec, Is.EqualTo(40f * CombatRules.BurnDamageFraction).Within(Tolerance));
            Assert.That(result.AddedEffects.Frost.Kind, Is.EqualTo(StatusEffectKind.Chill));
            Assert.That(result.AddedEffects.Lightning, Is.Null);
            Assert.That(result.InflictedEffects.Select(effect => effect.Kind), Is.EqualTo(new[] { StatusEffectKind.Bleed, StatusEffectKind.Burn, StatusEffectKind.Chill }));
        });
    }

    [Test]
    public void Zusatzschaden_VerbrauchtKeineWeiterenWuerfe()
    {
        var landed = Rolls.Create();
        var missed = Rolls.Create(hit: 0.999f);

        HitResolver.Resolve(PierceWithFire(), Defender(), landed);
        HitResolver.Resolve(PierceWithFire() with { HitChance = 50f }, Defender(), missed);

        Assert.Multiple(() =>
        {
            Assert.That(landed.Draws, Is.EqualTo(6));
            Assert.That(missed.Draws, Is.EqualTo(6));
        });
    }

    [Test]
    public void AbgewehrterTreffer_MachtAuchKeinenZusatzschaden()
    {
        var result = HitResolver.Resolve(PierceWithFire() with { HitChance = 50f }, Defender(), Rolls.Create(hit: 0.999f));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasLanded, Is.False);
            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(result.AddedDamage, Is.EqualTo(default(PerElement<int>)));
            Assert.That(result.InflictedEffects, Is.Empty);
        });
    }

    [Test]
    public void GewandelterTreffer_HatKeinenPhysischenAnteil()
    {
        var result = HitResolver.Resolve(new HitRequest(100, 100, DamageType.Lightning, SkillKind.Attack), Defender(), Rolls.Create());

        Assert.That(result.PhysicalDamage, Is.Zero);
    }

    #endregion

    #region Schätzung im Tooltip

    [Test]
    public void Schaetzung_RechnetDenZusatzschadenUndSeinenBrandMit()
    {
        var skill   = SkillDefinition.ForAttack("attack", AttackDefinition.Standard);
        var weapon  = new WeaponProfile(100, 200, 1f, 0f, DamageType.Crush, WeaponProfile.DefaultMeleeRange);
        var burning = weapon with { AddedDamage = new PerElement<DamageRange>(new DamageRange(40, 60), default, default) };

        var attacker = Attacker();

        weapon.ApplyTo(attacker);

        var plain     = SkillDamageEstimator.Estimate(attacker, weapon, skill);
        var withFire  = SkillDamageEstimator.Estimate(attacker, burning, skill);
        var fire      = HitRequests.ForAttack(attacker, burning, AttackDefinition.Standard).AddedDamage.Fire;
        var instances = Math.Min(CombatRules.BurnMaxStacks, withFire.UsesPerSecond * CombatRules.BurnDurationSec);
        var burnDps   = (fire.Min + fire.Max) / 2f * CombatRules.BurnDamageFraction / CombatRules.BurnDurationSec * instances;

        Assert.Multiple(() =>
        {
            Assert.That(withFire.MinHit, Is.EqualTo(plain.MinHit + fire.Min).Within(Tolerance));
            Assert.That(withFire.MaxHit, Is.EqualTo(plain.MaxHit + fire.Max).Within(Tolerance));
            Assert.That(plain.EffectDps, Is.Zero, "Crush löst nichts aus");
            Assert.That(withFire.EffectDps, Is.EqualTo(burnDps).Within(Tolerance), "der Feuerteil brennt, auch wenn der Hauptteil nichts auslöst");
        });
    }

    #endregion
}
