using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class HitRequestsTests
{
    private const string Ring = "Item:Ring";

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
    public void Standardangriff_VerursachtHundertProzentWaffenschaden()
    {
        var attacker   = Attacker();
        var multiplier = attacker.GetTotalMultiplier(CombatStat.PhysicalDamage);

        var request = HitRequests.ForAttack(attacker, Sword, AttackDefinition.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(request.SkillKind, Is.EqualTo(SkillKind.Attack));
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Slash));
            Assert.That(request.MinDamage, Is.EqualTo(12f * multiplier).Within(0.001f));
            Assert.That(request.MaxDamage, Is.EqualTo(28f * multiplier).Within(0.001f));
            Assert.That(request.HitChance, Is.EqualTo(100f));
            Assert.That(request.CriticalDamageBonus, Is.EqualTo(50f));
        });
    }

    [Test]
    public void Attack_SkaliertMitDemProzentsatzDesSkills()
    {
        var attacker   = Attacker();
        var multiplier = attacker.GetTotalMultiplier(CombatStat.ElementalDamage);
        var strike     = new AttackDefinition("Lightning Strike", 180f, DamageType.Lightning);

        var request = HitRequests.ForAttack(attacker, Sword, strike);

        Assert.Multiple(() =>
        {
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Lightning), "der Skill wandelt die Schadensart um");
            Assert.That(request.MinDamage, Is.EqualTo(12f * 1.8f * multiplier).Within(0.001f));
            Assert.That(request.MaxDamage, Is.EqualTo(28f * 1.8f * multiplier).Within(0.001f));
        });
    }

    [Test]
    public void Attack_WirdDurchStaerkeVerstaerkt()
    {
        var weak   = Attacker();
        var strong = Attacker();

        strong.SetBase(CombatStat.Strength, 50);

        var weakRequest   = HitRequests.ForAttack(weak, Sword, AttackDefinition.Standard);
        var strongRequest = HitRequests.ForAttack(strong, Sword, AttackDefinition.Standard);

        Assert.That(strongRequest.MaxDamage, Is.GreaterThan(weakRequest.MaxDamage));
    }

    [Test]
    public void Attack_RechnetGlobalenFlachenSchadenVorDenMultiplikatoren()
    {
        var attacker = Attacker();

        attacker.AddModifiers(
        [
            new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.Flat, 8, Ring),
            new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.Percentage, 0.5f, Ring)
        ]);

        var multiplier = attacker.GetTotalMultiplier(CombatStat.PhysicalDamage);
        var request    = HitRequests.ForAttack(attacker, Sword, AttackDefinition.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(multiplier, Is.GreaterThan(1.5f));
            Assert.That(request.MinDamage, Is.EqualTo((12f + 8f) * multiplier).Within(0.001f));
        });
    }

    [Test]
    public void Attack_KritChanceKommtVonDerWaffeUndWirdVomAngreiferVerstaerkt()
    {
        var attacker = Attacker();

        attacker.SetBase(CombatStat.Awareness, 40);

        var multiplier = attacker.GetTotalMultiplier(CombatStat.CriticalHitChance);
        var request    = HitRequests.ForAttack(attacker, Sword, AttackDefinition.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(multiplier, Is.GreaterThan(1f));
            Assert.That(request.CriticalHitChance, Is.EqualTo(5f * multiplier).Within(0.001f));
        });
    }

    [Test]
    public void Waffe_LegtAngriffstempoUndKritAlsGrundwerteInsBlatt()
    {
        var attacker = Attacker();

        Sword.ApplyTo(attacker);

        var request = HitRequests.ForAttack(attacker, Sword, AttackDefinition.Standard);

        Assert.Multiple(() =>
        {
            Assert.That(attacker.GetBase(CombatStat.Attackspeed), Is.EqualTo(1.14f));
            Assert.That(attacker.GetFinal(CombatStat.Attackspeed), Is.EqualTo(1.14f * attacker.GetTotalMultiplier(CombatStat.Attackspeed)).Within(0.001f));
            Assert.That(attacker.GetFinal(CombatStat.CriticalHitChance), Is.EqualTo(request.CriticalHitChance).Within(0.001f), "Anzeige und Kampf rechnen gleich");
        });
    }

    [Test]
    public void Spell_SkaliertNichtMitDerWaffe()
    {
        var attacker = Attacker();
        var fireball = new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10);

        Sword.ApplyTo(attacker);

        var multiplier = attacker.GetTotalMultiplier(CombatStat.SpellDamage) * attacker.GetTotalMultiplier(CombatStat.ElementalDamage);
        var request    = HitRequests.ForSpell(attacker, fireball);

        Assert.Multiple(() =>
        {
            Assert.That(request.SkillKind, Is.EqualTo(SkillKind.Spell));
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Fire));
            Assert.That(request.MinDamage, Is.EqualTo(50f * multiplier).Within(0.001f));
            Assert.That(request.MaxDamage, Is.EqualTo(75f * multiplier).Within(0.001f));
            Assert.That(request.CriticalHitChance, Is.EqualTo(10f * attacker.GetTotalMultiplier(CombatStat.CriticalHitChance)).Within(0.001f));
        });
    }

    [Test]
    public void Spell_WirdDurchZauberschadenVerstaerkt()
    {
        var attacker = Attacker();
        var fireball = new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10);
        var before   = HitRequests.ForSpell(attacker, fireball);

        attacker.AddModifiers(
        [
            new CombatStatModifier(CombatStat.SpellDamage, ModificationType.Flat, 10, Ring),
            new CombatStatModifier(CombatStat.SpellDamage, ModificationType.Percentage, 0.2f, Ring)
        ]);

        var multiplier = attacker.GetTotalMultiplier(CombatStat.SpellDamage) * attacker.GetTotalMultiplier(CombatStat.ElementalDamage);
        var after      = HitRequests.ForSpell(attacker, fireball);

        Assert.Multiple(() =>
        {
            Assert.That(after.MinDamage, Is.EqualTo((50f + 10f) * multiplier).Within(0.001f));
            Assert.That(after.MinDamage, Is.GreaterThan(before.MinDamage));
        });
    }

    [Test]
    public void Spell_WirdNichtDurchPhysischenSchadenVerstaerkt()
    {
        var attacker = Attacker();
        var fireball = new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10);
        var before   = HitRequests.ForSpell(attacker, fireball);

        attacker.AddModifier(new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.Percentage, 1f, Ring));

        Assert.That(HitRequests.ForSpell(attacker, fireball).MinDamage, Is.EqualTo(before.MinDamage));
    }
}
