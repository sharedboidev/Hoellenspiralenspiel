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
