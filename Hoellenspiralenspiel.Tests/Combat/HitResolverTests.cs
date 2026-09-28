using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class HitResolverTests
{
    private const string Shield = "Item:Shield";

    private static HitRequest Request(DamageType damageType,
                                      float      min               = 100f,
                                      float      max               = 100f,
                                      SkillKind  skillKind         = SkillKind.Attack,
                                      float      hitChance         = 100f,
                                      float      criticalHitChance = 0f,
                                      float      criticalDamage    = 50f)
        => new(min, max, damageType, skillKind, hitChance, criticalHitChance, criticalDamage);

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

    #region Schaden und Minderung

    [Test]
    public void Treffer_WirdDurchRuestungGemindert()
    {
        var defender = Defender((CombatStat.Armor, 50));
        var armor    = defender.GetFinal(CombatStat.Armor);

        var result = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasLanded, Is.True);
            Assert.That(result.UnmitigatedDamage, Is.EqualTo(100f));
            Assert.That(result.FinalDamage, Is.EqualTo((int)System.MathF.Round(CombatFormulas.MitigateByArmor(100f, armor))));
            Assert.That(result.FinalDamage, Is.LessThan(100));
        });
    }

    [Test]
    public void Schaden_WirdInDerSpanneGewuerfelt()
    {
        var defender = Defender();

        var lowest  = HitResolver.Resolve(Request(DamageType.Fire, 10, 20), defender, Rolls.Create(damage: 0f));
        var middle  = HitResolver.Resolve(Request(DamageType.Fire, 10, 20), defender, Rolls.Create(damage: 0.5f));
        var highest = HitResolver.Resolve(Request(DamageType.Fire, 10, 20), defender, Rolls.Create(damage: 0.999f));

        Assert.Multiple(() =>
        {
            Assert.That(lowest.RolledDamage, Is.EqualTo(10f));
            Assert.That(middle.RolledDamage, Is.EqualTo(15f));
            Assert.That(highest.RolledDamage, Is.EqualTo(20f).Within(0.02f));
        });
    }

    [Test]
    public void Resistenz_MindertElementarschaden()
    {
        var defender = Defender((CombatStat.FrostResistance, 75));

        var result = HitResolver.Resolve(Request(DamageType.Frost), defender, Rolls.Create());

        Assert.That(result.FinalDamage, Is.EqualTo(25));
    }

    [Test]
    public void Resistenz_GiltNurFuerIhreSchadensart()
    {
        var defender = Defender((CombatStat.FrostResistance, 75));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());

        Assert.That(result.FinalDamage, Is.EqualTo(100));
    }

    [Test]
    public void VolleResistenz_VerhindertSchadenUndEffekt()
    {
        var defender = Defender((CombatStat.FireResistance, 100));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasLanded, Is.True);
            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(result.InflictedEffect, Is.Null);
        });
    }

    [Test]
    public void Krit_ErhoehtDenSchadenUmDenKritSchaden()
    {
        var defender = Defender();

        var result = HitResolver.Resolve(Request(DamageType.Fire, criticalHitChance: 25f, criticalDamage: 50f), defender, Rolls.Create(crit: 0.2f));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.RolledDamage, Is.EqualTo(100f));
            Assert.That(result.FinalDamage, Is.EqualTo(150));
        });
    }

    [Test]
    public void Krit_BleibtAusWennDerWurfZuHochIst()
    {
        var result = HitResolver.Resolve(Request(DamageType.Fire, criticalHitChance: 25f), Defender(), Rolls.Create(crit: 0.25f));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.FinalDamage, Is.EqualTo(100));
        });
    }

    #endregion

    #region Abwehr

    [Test]
    public void Ausweichen_VerhindertDenTreffer()
    {
        var defender = Defender((CombatStat.Dodge, 30));

        var result = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create(dodge: 0.1f));

        Assert.Multiple(() =>
        {
            Assert.That(result.Avoidance, Is.EqualTo(HitAvoidance.Dodged));
            Assert.That(result.HasLanded, Is.False);
            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(result.InflictedEffect, Is.Null);
        });
    }

    [Test]
    public void Parry_NegiertDenTrefferKomplett()
    {
        var defender = Defender((CombatStat.MeleeParry, 40));

        var result = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create(parry: 0.1f));

        Assert.Multiple(() =>
        {
            Assert.That(result.Avoidance, Is.EqualTo(HitAvoidance.Parried));
            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(result.InflictedEffect, Is.Null);
        });
    }

    [Test]
    public void Parry_UnterscheidetAttackUndSpell()
    {
        var meleeParry = Defender((CombatStat.MeleeParry, 100));
        var spellParry = Defender((CombatStat.SpellParry, 100));

        Assert.Multiple(() =>
        {
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Spell), meleeParry, Rolls.Create()).HasLanded, Is.True);
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Attack), meleeParry, Rolls.Create()).Avoidance, Is.EqualTo(HitAvoidance.Parried));
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Spell), spellParry, Rolls.Create()).Avoidance, Is.EqualTo(HitAvoidance.Parried));
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Attack), spellParry, Rolls.Create()).HasLanded, Is.True);
        });
    }

    [Test]
    public void Ausweichen_KommtVorParry()
    {
        var defender = Defender((CombatStat.Dodge, 100), (CombatStat.MeleeParry, 100));

        var result = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());

        Assert.That(result.Avoidance, Is.EqualTo(HitAvoidance.Dodged));
    }

    [Test]
    public void Block_LaesstDieHaelfteDurch()
    {
        var defender = Defender((CombatStat.MeleeBlock, 40), (CombatStat.BlockReduction, CombatRules.BaseBlockReduction));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create(block: 0.1f));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasLanded, Is.True);
            Assert.That(result.WasBlocked, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(50));
        });
    }

    [Test]
    public void Block_AbgefangenerAnteilIstDurchModifierVeraenderbar()
    {
        var defender = Defender((CombatStat.MeleeBlock, 100), (CombatStat.BlockReduction, CombatRules.BaseBlockReduction));

        defender.AddModifier(new CombatStatModifier(CombatStat.BlockReduction, ModificationType.Flat, 25, Shield));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.WasBlocked, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(25), "75 % werden abgefangen");
        });
    }

    [Test]
    public void Block_FaengtHoechstensAllesAb()
    {
        var defender = Defender((CombatStat.MeleeBlock, 100), (CombatStat.BlockReduction, 140));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());

        Assert.That(result.FinalDamage, Is.Zero);
    }

    [Test]
    public void Block_UnterscheidetAttackUndSpell()
    {
        var defender = Defender((CombatStat.SpellBlock, 100), (CombatStat.BlockReduction, 50));

        Assert.Multiple(() =>
        {
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Spell), defender, Rolls.Create()).WasBlocked, Is.True);
            Assert.That(HitResolver.Resolve(Request(DamageType.Fire, skillKind: SkillKind.Attack), defender, Rolls.Create()).WasBlocked, Is.False);
        });
    }

    [Test]
    public void Trefferchance_LaesstAngriffeVerfehlen()
    {
        var result = HitResolver.Resolve(Request(DamageType.Slash, hitChance: 80f), Defender(), Rolls.Create(hit: 0.85f));

        Assert.Multiple(() =>
        {
            Assert.That(result.Avoidance, Is.EqualTo(HitAvoidance.Missed));
            Assert.That(result.FinalDamage, Is.Zero);
        });
    }

    #endregion

    #region Schadensarten

    [Test]
    public void Crush_VerursachtMehrSchaden()
    {
        var defender = Defender();
        var armor    = defender.GetFinal(CombatStat.Armor);

        var crush = HitResolver.Resolve(Request(DamageType.Crush), defender, Rolls.Create());
        var slash = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(crush.UnmitigatedDamage, Is.EqualTo(100f * (1f + CombatRules.CrushMoreDamage)).Within(0.001f));
            Assert.That(crush.FinalDamage, Is.EqualTo((int)System.MathF.Round(CombatFormulas.MitigateByArmor(120f, armor))));
            Assert.That(crush.FinalDamage, Is.GreaterThan(slash.FinalDamage));
            Assert.That(crush.InflictedEffect, Is.Null);
        });
    }

    [Test]
    public void Pierce_IgnoriertRuestung()
    {
        var defender = Defender((CombatStat.Armor, 5000));

        var pierce = HitResolver.Resolve(Request(DamageType.Pierce), defender, Rolls.Create());
        var slash  = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(pierce.FinalDamage, Is.EqualTo(100));
            Assert.That(slash.FinalDamage, Is.LessThan(20));
            Assert.That(pierce.InflictedEffect, Is.Null);
        });
    }

    [Test]
    public void Pierce_TrifftNurHalbSoOft()
    {
        var defender = Defender();

        Assert.Multiple(() =>
        {
            Assert.That(HitResolver.Resolve(Request(DamageType.Pierce), defender, Rolls.Create(hit: 0.49f)).HasLanded, Is.True);
            Assert.That(HitResolver.Resolve(Request(DamageType.Pierce), defender, Rolls.Create(hit: 0.5f)).Avoidance, Is.EqualTo(HitAvoidance.Missed));
            Assert.That(HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create(hit: 0.5f)).HasLanded, Is.True);
        });
    }

    [Test]
    public void Slash_LoestBleedAusDemUngemindertenSchadenAus()
    {
        var defender = Defender((CombatStat.Armor, 200));

        var result = HitResolver.Resolve(Request(DamageType.Slash), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalDamage, Is.LessThan(100));
            Assert.That(result.InflictedEffect, Is.Not.Null);
            Assert.That(result.InflictedEffect.Kind, Is.EqualTo(StatusEffectKind.Bleed));
            Assert.That(result.InflictedEffect.DurationSec, Is.EqualTo(CombatRules.BleedDurationSec));
            Assert.That(result.InflictedEffect.Magnitude * result.InflictedEffect.DurationSec,
                        Is.EqualTo(100f * CombatRules.BleedDamageFraction).Within(0.001f),
                        "Bleed rechnet mit dem Schaden vor der Rüstung");
        });
    }

    [Test]
    public void Fire_LoestBurnAusDemErlittenenSchadenAus()
    {
        var defender = Defender((CombatStat.FireResistance, 50));

        var result = HitResolver.Resolve(Request(DamageType.Fire), defender, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalDamage, Is.EqualTo(50));
            Assert.That(result.InflictedEffect.Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.InflictedEffect.Magnitude * result.InflictedEffect.DurationSec,
                        Is.EqualTo(50f * CombatRules.BurnDamageFraction).Within(0.001f),
                        "Resistenz mindert auch den Brand");
        });
    }

    [Test]
    public void Lightning_LoestShockAus()
    {
        var result = HitResolver.Resolve(Request(DamageType.Lightning), Defender(), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.InflictedEffect.Kind, Is.EqualTo(StatusEffectKind.Shock));
            Assert.That(result.InflictedEffect.Magnitude, Is.EqualTo(CombatRules.ShockActionFailureChance));
            Assert.That(result.InflictedEffect.DurationSec, Is.EqualTo(CombatRules.ShockDurationSec));
        });
    }

    [Test]
    public void Frost_LoestChillAus()
    {
        var result = HitResolver.Resolve(Request(DamageType.Frost), Defender(), Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.InflictedEffect.Kind, Is.EqualTo(StatusEffectKind.Chill));
            Assert.That(result.InflictedEffect.Magnitude, Is.EqualTo(CombatRules.ChillSlow));
            Assert.That(result.InflictedEffect.DurationSec, Is.EqualTo(CombatRules.ChillDurationSec));
        });
    }

    #endregion

    #region Würfeln

    [Test]
    public void JederTreffer_VerbrauchtGleichVieleWuerfe()
    {
        var landed = Rolls.Create();
        var dodged = Rolls.Create(dodge: 0f);
        var missed = Rolls.Create(hit: 0.999f);

        HitResolver.Resolve(Request(DamageType.Slash), Defender(), landed);
        HitResolver.Resolve(Request(DamageType.Slash), Defender((CombatStat.Dodge, 50)), dodged);
        HitResolver.Resolve(Request(DamageType.Slash, hitChance: 50f), Defender(), missed);

        Assert.Multiple(() =>
        {
            Assert.That(landed.Draws, Is.EqualTo(6));
            Assert.That(dodged.Draws, Is.EqualTo(6));
            Assert.That(missed.Draws, Is.EqualTo(6));
        });
    }

    [Test]
    public void GleicherSeed_ErgibtDenselbenKampf()
    {
        var defender = Defender((CombatStat.Dodge, 30), (CombatStat.MeleeBlock, 30), (CombatStat.BlockReduction, 50));
        var request  = Request(DamageType.Slash, 10, 90, criticalHitChance: 30f);
        var first    = new SeededRandom(1337);
        var second   = new SeededRandom(1337);

        for (var i = 0; i < 200; i++)
            Assert.That(HitResolver.Resolve(request, defender, first), Is.EqualTo(HitResolver.Resolve(request, defender, second)));
    }

    [Test]
    public void Chancen_TretenUngefaehrSoOftEinWieAngegeben()
    {
        var defender = Defender((CombatStat.Dodge, 25));
        var request  = Request(DamageType.Fire);
        var random   = new SeededRandom(42);
        var dodges   = 0;

        for (var i = 0; i < 20000; i++)
        {
            if (HitResolver.Resolve(request, defender, random).Avoidance == HitAvoidance.Dodged)
                dodges++;
        }

        Assert.That(dodges / 20000f, Is.EqualTo(defender.GetFinal(CombatStat.Dodge) / 100f).Within(0.015f));
    }

    #endregion
}
