using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class SkillDefinitionTests
{
    private static readonly WeaponProfile Sword = new(12, 28, 1.14f, 5, DamageType.Slash, WeaponProfile.DefaultMeleeRange);

    private static StatSheet Attacker()
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            s.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
        });

        return sheet;
    }

    [Test]
    public void Attack_IstEineAttackUndTraegtDenNamenDerDefinition()
    {
        var skill = SkillDefinition.ForAttack("lightning_strike", new AttackDefinition("Lightning Strike", 180f, DamageType.Lightning));

        Assert.Multiple(() =>
        {
            Assert.That(skill.Kind, Is.EqualTo(SkillKind.Attack));
            Assert.That(skill.Name, Is.EqualTo("Lightning Strike"));
            Assert.That(skill.Spell, Is.Null);
            Assert.That(skill.Delivery, Is.EqualTo(SkillDelivery.Weapon), "ohne Angabe trifft die Waffe");
        });
    }

    [Test]
    public void Spell_IstEinSpell()
    {
        var skill = SkillDefinition.ForSpell("fireball", new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10));

        Assert.Multiple(() =>
        {
            Assert.That(skill.Kind, Is.EqualTo(SkillKind.Spell));
            Assert.That(skill.Attack, Is.Null);
            Assert.That(skill.CastSec, Is.Zero, "ohne Angabe wirkt der Zauber sofort");
        });
    }

    [Test]
    public void BogenschlagUndNahkampfschlag_BrauchenEineNahkampfwaffe()
    {
        var cleave = SkillDefinition.ForAttack("cleave", new AttackDefinition("Cleave", 120f)) with { Delivery = SkillDelivery.WeaponSweep };
        var magma  = SkillDefinition.ForAttack("magma_strike", new AttackDefinition("Magma Strike", 80f, DamageType.Fire)) with { Delivery = SkillDelivery.MeleeStrike };

        Assert.Multiple(() =>
        {
            Assert.That(cleave.NeedsMeleeWeapon, Is.True);
            Assert.That(magma.NeedsMeleeWeapon, Is.True);
            Assert.That(magma.IsArea, Is.False);
            Assert.That(magma.Scatter, Is.Null, "ohne Angabe springen keine Kugeln");
            Assert.That(SkillDefinition.ForAttack("attack", AttackDefinition.Standard).NeedsMeleeWeapon, Is.False);
            Assert.That(cleave.IsArea, Is.False, "der Bogen gehört zur Waffe, nicht zu den Flächen");
        });
    }

    [Test]
    public void Pfeilregen_BrauchtEinenBogen_UndSonstNiemand()
    {
        var sky    = SkillDefinition.ForAttack("darken_sky", new AttackDefinition("Darken Sky", 80f)) with { Delivery = SkillDelivery.ArrowRain };
        var cleave = SkillDefinition.ForAttack("cleave", new AttackDefinition("Cleave", 120f)) with { Delivery = SkillDelivery.WeaponSweep };

        Assert.Multiple(() =>
        {
            Assert.That(sky.NeedsBow, Is.True);
            Assert.That(sky.NeedsMeleeWeapon, Is.False);
            Assert.That(sky.IsArea, Is.False, "die Pfeile sind eigene Flächen, der Skill selbst keine");
            Assert.That(sky.Rain, Is.Null, "ohne Angabe fallen keine Pfeile");
            Assert.That(cleave.NeedsBow, Is.False);
            Assert.That(SkillDefinition.ForAttack("attack", AttackDefinition.Standard).NeedsBow, Is.False);
        });
    }

    [Test]
    public void GeladenerSchuss_BrauchtEinenBogen_UndLaedtNurMitSeinenWerten()
    {
        var charge = new ChargeSettings(20f, 100f / 3f, 150f, 0.5f, 5);
        var shot   = SkillDefinition.ForAttack("charged_shot", new AttackDefinition("Charged Shot", 300f)) with { Delivery = SkillDelivery.ChargedShot };

        Assert.Multiple(() =>
        {
            Assert.That(shot.NeedsBow, Is.True);
            Assert.That(shot.NeedsMeleeWeapon, Is.False);
            Assert.That(shot.IsArea, Is.False);
            Assert.That(shot.IsCharged, Is.False, "ohne Werte lädt nichts");
            Assert.That((shot with { Charge = charge }).IsCharged, Is.True);
            Assert.That((shot with { Delivery = SkillDelivery.Weapon, Charge = charge }).IsCharged, Is.False, "die Werte allein machen keinen geladenen Schuss");
            Assert.That(SkillDefinition.ForAttack("attack", AttackDefinition.Standard).IsCharged, Is.False);
        });
    }

    [Test]
    public void SkillOhneId_IstNichtErlaubt()
        => Assert.That(() => SkillDefinition.ForAttack(" ", AttackDefinition.Standard), Throws.InstanceOf<ArgumentException>());

    [Test]
    public void IsArea_GiltFuerBeideFlaechen()
    {
        var spell = SkillDefinition.ForSpell("frost_nova", new SpellDefinition("Frost Nova", 10, 50, DamageType.Frost, 10));

        Assert.Multiple(() =>
        {
            Assert.That((spell with { Delivery = SkillDelivery.AreaAroundCaster }).IsArea, Is.True);
            Assert.That((spell with { Delivery = SkillDelivery.AreaAtPoint }).IsArea, Is.True);
            Assert.That((spell with { Delivery = SkillDelivery.Projectile }).IsArea, Is.False);
            Assert.That((spell with { Delivery = SkillDelivery.Weapon }).IsArea, Is.False);
        });
    }

    [Test]
    public void Treffer_EinerAttack_SkaliertMitDerWaffe()
    {
        var attacker = Attacker();
        var skill    = SkillDefinition.ForAttack("lightning_strike", new AttackDefinition("Lightning Strike", 180f, DamageType.Lightning));

        var request  = HitRequests.ForSkill(attacker, Sword, skill);
        var expected = HitRequests.ForAttack(attacker, Sword, skill.Attack);

        Assert.Multiple(() =>
        {
            Assert.That(request, Is.EqualTo(expected));
            Assert.That(request.SkillKind, Is.EqualTo(SkillKind.Attack));
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Lightning));
        });
    }

    [Test]
    public void Treffer_EinesSpells_IgnoriertDieWaffe()
    {
        var attacker = Attacker();
        var skill    = SkillDefinition.ForSpell("fireball", new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10));

        var withSword = HitRequests.ForSkill(attacker, Sword, skill);
        var unarmed   = HitRequests.ForSkill(attacker, WeaponProfile.Unarmed, skill);

        Assert.Multiple(() =>
        {
            Assert.That(withSword, Is.EqualTo(unarmed));
            Assert.That(withSword.SkillKind, Is.EqualTo(SkillKind.Spell));
        });
    }

    [Test]
    public void Treffer_IstFuerJedenWirkendenGleichGebaut()
    {
        var hero    = Attacker();
        var monster = Attacker();
        var skill   = SkillDefinition.ForSpell("fireball", new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10));

        Assert.That(HitRequests.ForSkill(hero, WeaponProfile.Unarmed, skill), Is.EqualTo(HitRequests.ForSkill(monster, WeaponProfile.Unarmed, skill)));
    }
}
