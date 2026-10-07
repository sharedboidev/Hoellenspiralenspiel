using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Auch ohne Attribute verstärkt das Stat-Blatt jeden Wert um 2 %, die Erwartungen leiten sich deshalb aus dem gebauten Treffer ab
[TestFixture]
public class SkillDamageEstimatorTests
{
    private const string Gear      = "Item:Gear";
    private const float  Tolerance = 0.01f;

    private static readonly SkillDefinition StandardAttack = SkillDefinition.ForAttack("attack", AttackDefinition.Standard);

    private static SkillDefinition Spell(DamageType damageType  = DamageType.Frost,
                                         float      critChance  = 0f,
                                         double     cooldownSec = 1,
                                         float      manaCost    = 0f)
        => SkillDefinition.ForSpell("spell", new SpellDefinition("Spell", 100f, 200f, damageType, critChance)) with
        {
            CooldownSec = cooldownSec,
            ManaCost = manaCost
        };

    private static WeaponProfile Weapon(DamageType damageType, float attacksPerSecond = 2f, float critChance = 0f)
        => new(100, 200, attacksPerSecond, critChance, damageType, WeaponProfile.DefaultMeleeRange);

    private static StatSheet Attacker(WeaponProfile weapon = null, float manaRegeneration = 0f)
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            s.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
            s.SetBase(CombatStat.Manaregeneration, manaRegeneration);
        });

        weapon?.ApplyTo(sheet);

        return sheet;
    }

    private static StatSheet DefencelessTarget()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.Armor, -sheet.GetAddedFlat(CombatStat.Armor));

        return sheet;
    }

    private static float AverageOf(HitRequest request)
        => (request.MinDamage + request.MaxDamage) / 2f;

    #region Treffer

    [Test]
    public void Treffer_OhneKrit_LiegenInDerSpanneDesSkills()
    {
        var attacker = Attacker();
        var spell    = Spell();
        var request  = HitRequests.ForSkill(attacker, WeaponProfile.Unarmed, spell);

        var estimate = SkillDamageEstimator.Estimate(attacker, WeaponProfile.Unarmed, spell);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.MinHit, Is.EqualTo(request.MinDamage));
            Assert.That(estimate.MaxHit, Is.EqualTo(request.MaxDamage));
            Assert.That(estimate.AverageHit, Is.EqualTo(AverageOf(request)).Within(Tolerance));
            Assert.That(estimate.MaxCriticalHit, Is.EqualTo(request.MaxDamage * 1.5f).Within(Tolerance), "Krit-Schaden von 50 %");
            Assert.That(estimate.DamageType, Is.EqualTo(DamageType.Frost));
            Assert.That(estimate.MinHit, Is.GreaterThanOrEqualTo(100f));
        });
    }

    [Test]
    public void MittlererTreffer_RechnetKritNachSeinerChanceEin()
    {
        var attacker = Attacker();
        var spell    = Spell(critChance: 20f);
        var request  = HitRequests.ForSkill(attacker, WeaponProfile.Unarmed, spell);

        var estimate = SkillDamageEstimator.Estimate(attacker, WeaponProfile.Unarmed, spell);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.CriticalHitChance, Is.EqualTo(request.CriticalHitChance));
            Assert.That(estimate.CriticalDamageBonus, Is.EqualTo(50f));
            Assert.That(estimate.AverageHit, Is.EqualTo(AverageOf(request) * (1f + request.CriticalHitChance / 100f * 0.5f)).Within(Tolerance));
            Assert.That(estimate.AverageHit, Is.GreaterThan(AverageOf(request)));
            Assert.That(estimate.MinHit, Is.EqualTo(request.MinDamage), "die Spanne bleibt ohne Krit");
            Assert.That(estimate.MaxHit, Is.EqualTo(request.MaxDamage));
        });
    }

    [Test]
    public void KritSchaden_VomAngreifer_ErhoehtDenMittlerenTreffer()
    {
        var attacker = Attacker();
        var spell    = Spell(critChance: 50f);

        attacker.AddModifier(new CombatStatModifier(CombatStat.CriticalDamage, ModificationType.Flat, 50, Gear));

        var request  = HitRequests.ForSkill(attacker, WeaponProfile.Unarmed, spell);
        var estimate = SkillDamageEstimator.Estimate(attacker, WeaponProfile.Unarmed, spell);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.CriticalDamageBonus, Is.EqualTo(100f));
            Assert.That(estimate.MaxCriticalHit, Is.EqualTo(request.MaxDamage * 2f).Within(Tolerance));
            Assert.That(estimate.AverageHit, Is.EqualTo(AverageOf(request) * (1f + request.CriticalHitChance / 100f)).Within(Tolerance));
        });
    }

    [Test]
    public void KritChance_UeberHundertProzent_WirdGekappt()
    {
        var attacker = Attacker();
        var spell    = Spell(critChance: 250f);
        var request  = HitRequests.ForSkill(attacker, WeaponProfile.Unarmed, spell);

        var estimate = SkillDamageEstimator.Estimate(attacker, WeaponProfile.Unarmed, spell);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.CriticalHitChance, Is.EqualTo(100f));
            Assert.That(estimate.AverageHit, Is.EqualTo(AverageOf(request) * 1.5f).Within(Tolerance), "jeder Treffer ist kritisch");
        });
    }

    [Test]
    public void Attack_SkaliertMitWaffeUndProzentsatzDesSkills()
    {
        var weapon   = Weapon(DamageType.Frost);
        var attacker = Attacker(weapon);
        var strike   = SkillDefinition.ForAttack("strike", new AttackDefinition("Strike", 180f));

        var standard = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);
        var stronger = SkillDamageEstimator.Estimate(attacker, weapon, strike);

        Assert.Multiple(() =>
        {
            Assert.That(stronger.MinHit, Is.EqualTo(standard.MinHit * 1.8f).Within(Tolerance));
            Assert.That(stronger.MaxHit, Is.EqualTo(standard.MaxHit * 1.8f).Within(Tolerance));
            Assert.That(stronger.Dps, Is.EqualTo(standard.Dps * 1.8f).Within(Tolerance));
        });
    }

    [Test]
    public void Crush_VerursachtMehrSchaden()
    {
        var weapon   = Weapon(DamageType.Crush);
        var attacker = Attacker(weapon);
        var request  = HitRequests.ForSkill(attacker, weapon, StandardAttack);

        var estimate = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.MinHit, Is.EqualTo(request.MinDamage * 1.2f).Within(Tolerance));
            Assert.That(estimate.MaxHit, Is.EqualTo(request.MaxDamage * 1.2f).Within(Tolerance));
            Assert.That(estimate.AverageHit, Is.EqualTo(AverageOf(request) * 1.2f).Within(Tolerance));
        });
    }

    [Test]
    public void Pierce_TrifftNurHalbSoOft()
    {
        var pierce = Weapon(DamageType.Pierce);
        var frost  = Weapon(DamageType.Frost);

        var pierceEstimate = SkillDamageEstimator.Estimate(Attacker(pierce), pierce, StandardAttack);
        var frostEstimate  = SkillDamageEstimator.Estimate(Attacker(frost), frost, SkillDefinition.ForAttack("attack", new AttackDefinition("Attack", 100f, DamageType.Pierce)));
        var reference      = SkillDamageEstimator.Estimate(Attacker(pierce), pierce, SkillDefinition.ForAttack("attack", new AttackDefinition("Attack", 100f, DamageType.Crush)));

        Assert.Multiple(() =>
        {
            Assert.That(pierceEstimate.HitChance, Is.EqualTo(50f));
            Assert.That(frostEstimate.HitChance, Is.EqualTo(50f), "es zählt die Schadensart des Treffers, nicht die der Waffe");
            Assert.That(reference.HitChance, Is.EqualTo(100f));
            Assert.That(pierceEstimate.HitDps, Is.EqualTo(pierceEstimate.AverageHit * (float)pierceEstimate.UsesPerSecond * 0.5f).Within(Tolerance));
        });
    }

    #endregion

    #region Tempo

    [Test]
    public void Attack_FolgtDemAngriffstempo()
    {
        var weapon   = Weapon(DamageType.Frost, 2f);
        var attacker = Attacker(weapon);
        var tempo    = attacker.GetFinal(CombatStat.Attackspeed);

        var estimate = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(tempo, Is.GreaterThanOrEqualTo(2f));
            Assert.That(estimate.UsesPerSecond, Is.EqualTo(tempo).Within(0.0001));
            Assert.That(estimate.Dps, Is.EqualTo(estimate.AverageHit * tempo).Within(Tolerance));
        });
    }

    [Test]
    public void Attack_WirdDurchModifierAufDasAngriffstempoSchneller()
    {
        var weapon   = Weapon(DamageType.Frost, 2f);
        var attacker = Attacker(weapon);
        var before   = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);

        attacker.AddModifier(new CombatStatModifier(CombatStat.Attackspeed, ModificationType.Percentage, 0.5f, Gear));

        var after = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(after.UsesPerSecond, Is.EqualTo(before.UsesPerSecond * 1.5).Within(0.0001));
            Assert.That(after.Dps, Is.EqualTo(before.Dps * 1.5f).Within(Tolerance));
            Assert.That(after.AverageHit, Is.EqualTo(before.AverageHit), "der einzelne Treffer bleibt gleich");
        });
    }

    [Test]
    public void Attack_MitLangerAbklingzeit_WirdVonIhrGebremst()
    {
        var weapon = Weapon(DamageType.Frost, 2f);
        var slam   = StandardAttack with { CooldownSec = 4 };

        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, slam);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.UsesPerSecond, Is.EqualTo(0.25).Within(0.0001));
            Assert.That(estimate.Dps, Is.EqualTo(estimate.AverageHit / 4f).Within(Tolerance));
        });
    }

    [Test]
    public void Attack_MitKurzerAbklingzeit_BleibtBeimTaktDerWaffe()
    {
        var weapon   = Weapon(DamageType.Frost, 2f);
        var attacker = Attacker(weapon);
        var quick    = StandardAttack with { CooldownSec = 0.1 };

        Assert.That(SkillDamageEstimator.GetUsesPerSecond(attacker, quick), Is.EqualTo(attacker.GetFinal(CombatStat.Attackspeed)).Within(0.0001));
    }

    [Test]
    public void Attack_OhneAngriffstempo_BleibtBeimMindesttempo()
        => Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(), StandardAttack), Is.EqualTo(CombatRules.MinAttacksPerSecond).Within(0.0001));

    [Test]
    public void Spell_FolgtSeinerAbklingzeitUndNichtDerWaffe()
    {
        var weapon   = Weapon(DamageType.Slash, 5f);
        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, Spell(cooldownSec: 0.5));

        Assert.Multiple(() =>
        {
            Assert.That(estimate.UsesPerSecond, Is.EqualTo(2).Within(0.0001));
            Assert.That(estimate.Dps, Is.EqualTo(estimate.AverageHit * 2f).Within(Tolerance));
        });
    }

    [Test]
    public void Spell_OhneAbklingzeit_BleibtBeiDerMindestzeit()
        => Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(), Spell(cooldownSec: 0)), Is.EqualTo(1 / CombatRules.MinSpellCooldownSec).Within(0.0001));

    [Test]
    public void Spell_MitLaengererWirkzeit_FolgtDerWirkzeit()
    {
        var fireball = Spell(cooldownSec: 0.25) with { CastSec = 0.4 };

        Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(), fireball), Is.EqualTo(2.5).Within(0.0001));
    }

    [Test]
    public void Spell_MitKuerzererWirkzeit_FolgtDerAbklingzeit()
    {
        var thunderbolt = Spell(cooldownSec: 1) with { CastSec = 0.4 };

        Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(), thunderbolt), Is.EqualTo(1).Within(0.0001));
    }

    [Test]
    public void Attack_KenntKeineWirkzeit()
    {
        var weapon = Weapon(DamageType.Frost, 2f);
        var slow   = StandardAttack with { CastSec = 3 };

        Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(weapon), slow), Is.EqualTo(SkillDamageEstimator.GetUsesPerSecond(Attacker(weapon), StandardAttack)).Within(0.0001));
    }

    [Test]
    public void Fehlschlaege_SenkenDieDps()
    {
        var healthy = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell());
        var shocked = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell(), CombatRules.ShockActionFailureChance);

        Assert.Multiple(() =>
        {
            Assert.That(shocked.ActionFailureChance, Is.EqualTo(0.25f));
            Assert.That(shocked.Dps, Is.EqualTo(healthy.Dps * 0.75f).Within(Tolerance));
            Assert.That(shocked.AverageHit, Is.EqualTo(healthy.AverageHit), "der einzelne Treffer bleibt gleich");
        });
    }

    #endregion

    #region Statuseffekte

    [Test]
    public void Frost_UndLightning_HabenKeinenSchadenUeberZeit()
    {
        var frost     = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell());
        var lightning = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell(DamageType.Lightning));

        Assert.Multiple(() =>
        {
            Assert.That(frost.DamagingEffect, Is.Null);
            Assert.That(frost.EffectDps, Is.Zero);
            Assert.That(lightning.DamagingEffect, Is.Null);
            Assert.That(lightning.Dps, Is.EqualTo(lightning.HitDps));
        });
    }

    [Test]
    public void Burn_LegtEinViertelDesSchadensObendrauf()
    {
        var estimate = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell(DamageType.Fire, cooldownSec: 1));

        Assert.Multiple(() =>
        {
            Assert.That(estimate.DamagingEffect, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(estimate.HitDps, Is.EqualTo(estimate.AverageHit).Within(Tolerance), "ein Treffer pro Sekunde");
            Assert.That(estimate.EffectDps, Is.EqualTo(estimate.HitDps * CombatRules.BurnDamageFraction).Within(Tolerance));
            Assert.That(estimate.Dps, Is.EqualTo(estimate.HitDps * 1.25f).Within(Tolerance));
        });
    }

    [Test]
    public void Burn_IstDurchDenStapelBegrenzt()
    {
        var estimate = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell(DamageType.Fire, cooldownSec: 0.25));
        var perStack = estimate.AverageHit * CombatRules.BurnDamageFraction / CombatRules.BurnDurationSec;

        Assert.Multiple(() =>
        {
            Assert.That(estimate.HitDps, Is.EqualTo(estimate.AverageHit * 4f).Within(Tolerance));
            Assert.That(estimate.EffectDps, Is.EqualTo(perStack * CombatRules.BurnMaxStacks).Within(Tolerance));
            Assert.That(estimate.EffectDps, Is.LessThan(estimate.HitDps * CombatRules.BurnDamageFraction));
        });
    }

    [Test]
    public void Bleed_LegtDieHaelfteDesSchadensObendrauf()
    {
        var weapon   = Weapon(DamageType.Slash, 1f);
        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.DamagingEffect, Is.EqualTo(StatusEffectKind.Bleed));
            Assert.That(estimate.EffectDps, Is.EqualTo(estimate.HitDps * CombatRules.BleedDamageFraction).Within(Tolerance));
            Assert.That(estimate.Dps, Is.EqualTo(estimate.HitDps * 1.5f).Within(Tolerance));
        });
    }

    [Test]
    public void Bleed_WaechstMitDemAngriffstempo()
    {
        var slow = Weapon(DamageType.Slash, 1f);
        var fast = Weapon(DamageType.Slash, 4f);

        var slowEstimate = SkillDamageEstimator.Estimate(Attacker(slow), slow, StandardAttack);
        var fastEstimate = SkillDamageEstimator.Estimate(Attacker(fast), fast, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(fastEstimate.HitDps, Is.EqualTo(slowEstimate.HitDps * 4f).Within(Tolerance));
            Assert.That(fastEstimate.EffectDps, Is.EqualTo(slowEstimate.EffectDps * 4f).Within(Tolerance));
        });
    }

    [Test]
    public void Bleed_HatKeineObergrenze()
    {
        var weapon   = Weapon(DamageType.Slash, 5f);
        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, StandardAttack);
        var perStack = estimate.AverageHit * CombatRules.BleedDamageFraction / CombatRules.BleedDurationSec;
        var stacks   = estimate.UsesPerSecond * CombatRules.BleedDurationSec;

        Assert.Multiple(() =>
        {
            Assert.That(stacks, Is.GreaterThan(CombatRules.BurnMaxStacks));
            Assert.That(estimate.EffectDps, Is.EqualTo(perStack * stacks).Within(Tolerance));
        });
    }

    [Test]
    public void Bleed_MitSeltenenTreffern_WirktNurEinenTeilDerZeit()
    {
        var weapon = Weapon(DamageType.Slash, 1f);
        var rare   = StandardAttack with { CooldownSec = 8 };

        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, rare);
        var bleedDps = estimate.AverageHit * CombatRules.BleedDamageFraction / CombatRules.BleedDurationSec;

        Assert.That(estimate.EffectDps, Is.EqualTo(bleedDps * 0.5f).Within(Tolerance), "vier Sekunden Blutung alle acht Sekunden");
    }

    #endregion

    #region Mana

    [Test]
    public void GenugManaregeneration_BegrenztNicht()
    {
        var estimate = SkillDamageEstimator.Estimate(Attacker(manaRegeneration: 5f), WeaponProfile.Unarmed, Spell(cooldownSec: 1, manaCost: 4f));

        Assert.Multiple(() =>
        {
            Assert.That(estimate.ManaPerSecond, Is.EqualTo(4f));
            Assert.That(estimate.IsLimitedByMana, Is.False);
            Assert.That(estimate.SustainedDps, Is.EqualTo(estimate.Dps));
        });
    }

    [Test]
    public void ZuWenigManaregeneration_SenktDieDpsAufDauer()
    {
        var attacker     = Attacker(manaRegeneration: 1f);
        var regeneration = attacker.GetFinal(CombatStat.Manaregeneration);

        var estimate = SkillDamageEstimator.Estimate(attacker, WeaponProfile.Unarmed, Spell(cooldownSec: 1, manaCost: 4f));

        Assert.Multiple(() =>
        {
            Assert.That(estimate.IsLimitedByMana, Is.True);
            Assert.That(estimate.Dps, Is.EqualTo(estimate.AverageHit).Within(Tolerance), "kurzfristig zählt das Mana nicht");
            Assert.That(estimate.SustainedDps, Is.EqualTo(estimate.AverageHit * regeneration / 4f).Within(Tolerance), "auf Dauer nur so viele Zauber, wie das Mana nachkommt");
            Assert.That(estimate.SustainedDps, Is.LessThan(estimate.Dps));
        });
    }

    [Test]
    public void SkillOhneKosten_IstNieDurchManaBegrenzt()
    {
        var estimate = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell());

        Assert.Multiple(() =>
        {
            Assert.That(estimate.IsLimitedByMana, Is.False);
            Assert.That(estimate.ManaPerSecond, Is.Zero);
            Assert.That(estimate.SustainedDps, Is.EqualTo(estimate.Dps));
        });
    }

    [Test]
    public void EinheitOhneMana_IstNieDurchManaBegrenzt()
    {
        var estimate = SkillDamageEstimator.Estimate(Attacker(), WeaponProfile.Unarmed, Spell(manaCost: 4f), paysMana: false);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.IsLimitedByMana, Is.False);
            Assert.That(estimate.ManaPerSecond, Is.Zero);
            Assert.That(estimate.SustainedDps, Is.EqualTo(estimate.Dps));
        });
    }

    #endregion

    #region Abgleich mit der Trefferauflösung

    [TestCase(DamageType.Crush)]
    [TestCase(DamageType.Pierce)]
    [TestCase(DamageType.Slash)]
    [TestCase(DamageType.Fire)]
    [TestCase(DamageType.Frost)]
    [TestCase(DamageType.Lightning)]
    public void Grenzen_StimmenMitDerTrefferaufloesungUeberein(DamageType damageType)
    {
        var weapon   = Weapon(damageType, critChance: 30f);
        var attacker = Attacker(weapon);
        var defender = DefencelessTarget();
        var request  = HitRequests.ForSkill(attacker, weapon, StandardAttack);

        var estimate = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);
        var lowest   = HitResolver.Resolve(request, defender, Rolls.Create(damage: 0f));
        var highest  = HitResolver.Resolve(request, defender, Rolls.Create(damage: 1f));
        var critical = HitResolver.Resolve(request, defender, Rolls.Create(crit: 0f, damage: 1f));

        Assert.Multiple(() =>
        {
            Assert.That(critical.IsCritical, Is.True);
            Assert.That(estimate.MinHit, Is.EqualTo(lowest.FinalDamage).Within(0.5f));
            Assert.That(estimate.MaxHit, Is.EqualTo(highest.FinalDamage).Within(0.5f));
            Assert.That(estimate.MaxCriticalHit, Is.EqualTo(critical.FinalDamage).Within(0.5f));
        });
    }

    [TestCase(DamageType.Crush)]
    [TestCase(DamageType.Pierce)]
    [TestCase(DamageType.Slash)]
    [TestCase(DamageType.Fire)]
    [TestCase(DamageType.Frost)]
    [TestCase(DamageType.Lightning)]
    public void Schaetzung_TrifftDenMittelwertVielerGewuerfelterTreffer(DamageType damageType)
    {
        const int attempts = 200_000;

        var weapon   = Weapon(damageType, critChance: 30f);
        var attacker = Attacker(weapon);
        var defender = DefencelessTarget();
        var request  = HitRequests.ForSkill(attacker, weapon, StandardAttack);
        var random   = new SeededRandom(20260928);

        long totalDamage = 0;
        var  landed      = 0;

        for (var i = 0; i < attempts; i++)
        {
            var result = HitResolver.Resolve(request, defender, random);

            if (!result.HasLanded)
                continue;

            landed++;
            totalDamage += result.FinalDamage;
        }

        var estimate = SkillDamageEstimator.Estimate(attacker, weapon, StandardAttack);

        Assert.Multiple(() =>
        {
            Assert.That(landed * 100.0 / attempts, Is.EqualTo(estimate.HitChance).Within(0.5), "Anteil der Treffer");
            Assert.That((double)totalDamage / landed, Is.EqualTo(estimate.AverageHit).Within(estimate.AverageHit * 0.005), "mittlerer Treffer");
            Assert.That(totalDamage / (double)attempts * estimate.UsesPerSecond, Is.EqualTo(estimate.HitDps).Within(estimate.HitDps * 0.01), "Schaden pro Sekunde aus Treffern");
        });
    }

    [TestCase(DamageType.Slash, 0.2f)]
    [TestCase(DamageType.Slash, 2f)]
    [TestCase(DamageType.Slash, 5f)]
    [TestCase(DamageType.Fire, 0.2f)]
    [TestCase(DamageType.Fire, 2f)]
    [TestCase(DamageType.Fire, 5f)]
    public void SchadenUeberZeit_StimmtMitDemVerlaufDerEffekteUeberein(DamageType damageType, float attacksPerSecond)
    {
        const double frameSec    = 1.0 / 60.0;
        const double warmUpSec   = 10;
        const double measuredSec = 60;

        var weapon   = Weapon(damageType, attacksPerSecond);
        var estimate = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, StandardAttack);
        var effect   = StatusEffectRules.GetEffectOfHit(damageType, estimate.AverageHit, (int)MathF.Round(estimate.AverageHit));
        var tracker  = new StatusEffectTracker(new StatSheet());
        var ticks    = new List<StatusTick>();

        var hitIntervalSec = 1.0 / estimate.UsesPerSecond;
        var nextHitSec     = 0.0;
        var damage         = 0;

        for (var nowSec = 0.0; nowSec < warmUpSec + measuredSec; nowSec += frameSec)
        {
            while (nextHitSec <= nowSec)
            {
                tracker.Apply(effect);

                nextHitSec += hitIntervalSec;
            }

            ticks.Clear();
            tracker.Advance(frameSec, ticks);

            if (nowSec < warmUpSec)
                continue;

            foreach (var tick in ticks)
                damage += tick.Damage;
        }

        Assert.That(damage / measuredSec, Is.EqualTo(estimate.EffectDps).Within(estimate.EffectDps * 0.02));
    }

    #endregion

    #region Kugeln

    private static SkillDefinition MagmaStrike(int balls = 3, float manaCost = 0f)
        => SkillDefinition.ForAttack("magma_strike", new AttackDefinition("Magma Strike", 80f, DamageType.Fire)) with
        {
            ManaCost = manaCost,
            Delivery = SkillDelivery.MeleeStrike,
            Scatter = balls > 0 ? new ScatterSettings(balls, 65f, 75f, 0.6f) : null
        };

    private static HitRequest MagmaHit(StatSheet attacker, WeaponProfile weapon, float weaponDamagePercent)
        => HitRequests.ForAttack(attacker, weapon, new AttackDefinition("Magma Strike", weaponDamagePercent, DamageType.Fire));

    //So langsam, dass der Brand nicht an seine Obergrenze stößt
    [Test]
    public void Kugeln_ZaehlenMitIhremAnteilZuDenDps()
    {
        var weapon   = Weapon(DamageType.Slash, 0.25f);
        var attacker = Attacker(weapon);
        var strike   = SkillDamageEstimator.Estimate(attacker, weapon, MagmaStrike(0));
        var magma    = SkillDamageEstimator.Estimate(attacker, weapon, MagmaStrike());
        var ball     = AverageOf(MagmaHit(attacker, weapon, 65f));
        var share    = ball / AverageOf(MagmaHit(attacker, weapon, 80f));
        var landing  = magma.HitChance / 100f;

        Assert.Multiple(() =>
        {
            Assert.That(magma.AverageHit, Is.EqualTo(strike.AverageHit), "der Schlag selbst bleibt gleich");
            Assert.That(magma.ScatterCount, Is.EqualTo(3));
            Assert.That(strike.ScatterCount, Is.Zero);
            Assert.That(magma.ScatterAverageHit, Is.EqualTo(ball).Within(Tolerance));
            Assert.That(share, Is.EqualTo(65f / 80f).Within(Tolerance));
            Assert.That(magma.HitDps, Is.EqualTo(strike.HitDps + 3f * ball * (float)magma.UsesPerSecond * landing * landing).Within(Tolerance),
                        "nur ein gelandeter Schlag wirft Kugeln, und jede muss selbst treffen");
            Assert.That(magma.EffectDps, Is.EqualTo(strike.EffectDps * (1f + 3f * landing * share)).Within(Tolerance), "die Kugeln brennen wie der Schlag");
        });
    }

    [Test]
    public void KugelnUndSchlag_TeilenSichDieStapelDesBrands()
    {
        var weapon   = Weapon(DamageType.Slash, 1f);
        var attacker = Attacker(weapon);
        var magma    = SkillDamageEstimator.Estimate(attacker, weapon, MagmaStrike());
        var landing  = magma.HitChance / 100f;
        var strikes  = (float)magma.UsesPerSecond * landing;
        var balls    = strikes * 3f * landing;
        var average  = (strikes * AverageOf(MagmaHit(attacker, weapon, 80f)) + balls * AverageOf(MagmaHit(attacker, weapon, 65f))) / (strikes + balls);
        var perStack = average * CombatRules.BurnDamageFraction / CombatRules.BurnDurationSec;

        Assert.Multiple(() =>
        {
            Assert.That(strikes * CombatRules.BurnDurationSec, Is.LessThan(CombatRules.BurnMaxStacks), "der Schlag allein bliebe darunter");
            Assert.That((strikes + balls) * CombatRules.BurnDurationSec, Is.GreaterThan(CombatRules.BurnMaxStacks));
            Assert.That(magma.EffectDps, Is.EqualTo(perStack * CombatRules.BurnMaxStacks).Within(Tolerance));
        });
    }

    [Test]
    public void Kugeln_WerdenVomManaGebremstWieDerSchlag()
    {
        var weapon   = Weapon(DamageType.Slash, 0.25f);
        var attacker = Attacker(weapon, 0.25f);
        var magma    = SkillDamageEstimator.Estimate(attacker, weapon, MagmaStrike(manaCost: 2f));
        var affords  = attacker.GetFinal(CombatStat.Manaregeneration) / 2f;

        Assert.Multiple(() =>
        {
            Assert.That(magma.IsLimitedByMana, Is.True);
            Assert.That(magma.SustainedDps, Is.EqualTo(magma.Dps * affords / (float)magma.UsesPerSecond).Within(Tolerance));
        });
    }

    [Test]
    public void Bonusprojektile_WerfenMehrKugeln()
    {
        var weapon   = Weapon(DamageType.Slash, 0.25f);
        var attacker = Attacker(weapon);

        attacker.SetBase(CombatStat.ProjectileCount, 3);

        var magma = SkillDamageEstimator.Estimate(attacker, weapon, MagmaStrike());
        var plain = SkillDamageEstimator.Estimate(Attacker(weapon), weapon, MagmaStrike());

        Assert.Multiple(() =>
        {
            Assert.That(magma.ScatterCount, Is.EqualTo(5), "drei Kugeln und zwei Projektile über das erste hinaus");
            Assert.That(magma.HitDps - plain.HitDps, Is.EqualTo(2f * (plain.HitDps - plain.AverageHit * (float)plain.UsesPerSecond * plain.HitChance / 100f) / 3f).Within(Tolerance),
                        "jede weitere Kugel zählt wie die drei ersten");
        });
    }

    #endregion

    #region Pfeilregen

    private static SkillDefinition DarkenSky(int arrows = 5, double cooldownSec = 3)
        => SkillDefinition.ForAttack("darken_sky", new AttackDefinition("Darken Sky", 80f)) with
        {
            ManaCost = 3f,
            CooldownSec = cooldownSec,
            Delivery = SkillDelivery.ArrowRain,
            Rain = new RainSettings(arrows, 200f, 75f, 0.5f, 1f)
        };

    private static WeaponProfile Bow(float attacksPerSecond = 2f, int extraArrows = 0)
        => new WeaponProfile(100, 200, attacksPerSecond, 0f, DamageType.Pierce, 600f, true, 1100f) with { ExtraProjectiles = extraArrows };

    //Nur ein Teil der Pfeile erreicht ein Ziel in der Mitte, Einschlagradius durch Radius. Die Abklingzeit bremst die Attack
    [Test]
    public void Pfeilregen_ZaehltDiePfeileAufDasZiel_UndWartetDieAbklingzeitAb()
    {
        var bow      = Bow();
        var attacker = Attacker(bow);
        var shot     = SkillDamageEstimator.Estimate(attacker, bow, DarkenSky() with { Delivery = SkillDelivery.Weapon, Rain = null, CooldownSec = 0 });
        var sky      = SkillDamageEstimator.Estimate(attacker, bow, DarkenSky());
        var landing  = sky.HitChance / 100f;

        Assert.Multiple(() =>
        {
            Assert.That(sky.AverageHit, Is.EqualTo(shot.AverageHit), "jeder Pfeil trifft wie ein Schuss mit 80 %");
            Assert.That(sky.ArrowCount, Is.EqualTo(5));
            Assert.That(shot.ArrowCount, Is.Zero);
            Assert.That(sky.ArrowsOnTarget, Is.EqualTo(1.875f).Within(Tolerance));
            Assert.That(sky.UsesPerSecond, Is.EqualTo(1.0 / 3).Within(Tolerance), "die Abklingzeit, nicht das Angriffstempo");
            Assert.That(shot.UsesPerSecond, Is.EqualTo(attacker.GetFinal(CombatStat.Attackspeed)).Within(Tolerance));
            Assert.That(sky.HitDps, Is.EqualTo(sky.AverageHit / 3f * 1.875f * landing).Within(Tolerance));
            Assert.That(sky.ManaPerSecond, Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void Bonusprojektile_WerdenZuPfeilen_MitDenenDesBogens()
    {
        var bow      = Bow(extraArrows: 1);
        var attacker = Attacker(bow);

        attacker.SetBase(CombatStat.ProjectileCount, 2);

        var sky = SkillDamageEstimator.Estimate(attacker, bow, DarkenSky());

        Assert.Multiple(() =>
        {
            Assert.That(sky.ArrowCount, Is.EqualTo(7), "fünf Pfeile, einer aus dem Stat, einer vom Bogen");
            Assert.That(sky.ArrowsOnTarget, Is.EqualTo(7f * 0.375f).Within(Tolerance));
            Assert.That(SkillDamageEstimator.Estimate(Attacker(bow), bow, DarkenSky()).ArrowCount, Is.EqualTo(6), "der Bogen allein gibt einen");
        });
    }

    #endregion

    #region Geladener Schuss

    private static SkillDefinition ChargedShot(float ratePerSec = 20f)
        => SkillDefinition.ForAttack("charged_shot", new AttackDefinition("Charged Shot", 300f)) with
        {
            ManaCost = 2f,
            Delivery = SkillDelivery.ChargedShot,
            Charge = new ChargeSettings(ratePerSec, 100f / 3f, 150f, 0.5f, 5)
        };

    //Der Tooltip rechnet mit voller Ladung: 5 s Laden beim Tempo des Helden, dann die halbe Dauer eines Schusses als Erholung
    [Test]
    public void GeladenerSchuss_TrifftWieVollGeladen_UndBrauchtDieLadezeitJeSchuss()
    {
        var bow      = Bow();
        var attacker = Attacker(bow);
        var tempo    = attacker.GetFinal(CombatStat.Attackspeed);
        var full     = SkillDamageEstimator.Estimate(attacker, bow, SkillDefinition.ForAttack("shot", new AttackDefinition("Shot", 300f)));
        var charged  = SkillDamageEstimator.Estimate(attacker, bow, ChargedShot());
        var interval = 5 / ChargeSettings.GetRateFactor(attacker) + 1 / tempo * (1 - CombatRules.ActionImpactFraction);

        Assert.Multiple(() =>
        {
            Assert.That(charged.AverageHit, Is.EqualTo(full.AverageHit), "300 % Waffenschaden bei voller Ladung");
            Assert.That(charged.UsesPerSecond, Is.EqualTo(1 / interval).Within(0.0001));
            Assert.That(charged.UsesPerSecond, Is.LessThan(full.UsesPerSecond));
            Assert.That(charged.HitDps, Is.EqualTo(charged.AverageHit * (float)charged.UsesPerSecond * charged.HitChance / 100f).Within(Tolerance));
            Assert.That(charged.ManaPerSecond, Is.EqualTo(2f * (float)charged.UsesPerSecond).Within(Tolerance));
            Assert.That(charged.ArrowCount, Is.Zero);
        });
    }

    //Die Waffe allein ändert die Ladezeit nicht, nur ihre Erholung. Erst Modifier auf das Angriffstempo laden schneller
    [Test]
    public void SchnelleresLaden_GibtMehrSchuesse_DieWaffeAlleinNurKuerzereErholung()
    {
        var bow      = Bow();
        var quick    = Attacker(Bow(4f));
        var factor   = ChargeSettings.GetRateFactor(Attacker(bow));
        var slow     = SkillDamageEstimator.GetUsesPerSecond(Attacker(bow), ChargedShot());
        var fast     = SkillDamageEstimator.GetUsesPerSecond(Attacker(bow), ChargedShot(40f));
        var recovery = 0.5 / quick.GetFinal(CombatStat.Attackspeed);

        Assert.Multiple(() =>
        {
            Assert.That(factor, Is.EqualTo(ChargeSettings.GetRateFactor(quick)).Within(0.0001), "die Waffe selbst ist kein Modifier");
            Assert.That(1 / fast, Is.EqualTo(1 / slow - 2.5 / factor).Within(0.0001), "halb so lange laden");
            Assert.That(1 / SkillDamageEstimator.GetUsesPerSecond(quick, ChargedShot()), Is.EqualTo(5 / factor + recovery).Within(0.0001), "bei rund 4 Schuss je Sekunde bleibt ein Achtel Erholung");
            Assert.That(recovery, Is.EqualTo(0.125).Within(0.01));
        });
    }

    [Test]
    public void ErhoehtesAngriffstempo_HalbiertDieLadezeit_UndDieErholung()
    {
        var bow      = Bow();
        var attacker = Attacker(bow);
        var before   = 1 / SkillDamageEstimator.GetUsesPerSecond(attacker, ChargedShot());
        var recovery = 0.5 / attacker.GetFinal(CombatStat.Attackspeed);

        attacker.AddModifier(new CombatStatModifier(CombatStat.Attackspeed, ModificationType.Percentage, 1f, Gear));

        var after = 1 / SkillDamageEstimator.GetUsesPerSecond(attacker, ChargedShot());

        Assert.That(after, Is.EqualTo(before / 2).Within(0.0001), "Ladezeit und Erholung halbieren sich beide");
        Assert.That(before - recovery, Is.EqualTo(5 / ChargeSettings.GetRateFactor(Attacker(bow))).Within(0.0001));
    }

    //Rückmeldung des Users vom 07.10.2026: Der geladene Schuss muss immer treffen, der Malus von Pierce gilt für ihn nicht
    [Test]
    public void GeladenerSchuss_TrifftTrotzPierceMitVollerChance()
    {
        var bow      = Bow();
        var attacker = Attacker(bow);
        var shot     = SkillDefinition.ForAttack("charged_shot", new AttackDefinition("Charged Shot", 300f) { IgnoresPierceHitPenalty = true }) with
        {
            Delivery = SkillDelivery.ChargedShot,
            Charge = ChargedShot().Charge
        };

        var plain     = SkillDamageEstimator.Estimate(attacker, bow, ChargedShot());
        var certain   = SkillDamageEstimator.Estimate(attacker, bow, shot);
        var request   = HitRequests.ForSkill(attacker, bow, shot);
        var wouldMiss = HitResolver.Resolve(request, DefencelessTarget(), Rolls.Create(hit: 0.75f));

        Assert.Multiple(() =>
        {
            Assert.That(plain.HitChance, Is.EqualTo(50f), "ohne das Kennzeichen halbiert Pierce");
            Assert.That(certain.HitChance, Is.EqualTo(100f));
            Assert.That(certain.HitDps, Is.EqualTo(plain.HitDps * 2f).Within(Tolerance));
            Assert.That(request.IgnoresPierceHitPenalty, Is.True);
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Pierce), "die Schadensart bleibt Pierce, samt Rüstungsregel");
            Assert.That(wouldMiss.HasLanded, Is.True, "ein Wurf von 75 % verfehlte mit Malus, trifft ohne");
            Assert.That(shot.Charge.GetAttack(shot.Attack, 150f).IgnoresPierceHitPenalty, Is.True, "die Ladung behält das Kennzeichen");
        });
    }

    [Test]
    public void GeladenerSchuss_MitAbklingzeit_WartetDieLaengereZeitAb()
    {
        var bow = Bow();

        Assert.Multiple(() =>
        {
            Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(bow), ChargedShot() with { CooldownSec = 10 }), Is.EqualTo(0.1).Within(0.0001));
            Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(bow), ChargedShot() with { CooldownSec = 1 }), Is.EqualTo(SkillDamageEstimator.GetUsesPerSecond(Attacker(bow), ChargedShot())).Within(0.0001));
        });
    }

    #endregion
}
