using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class SkillGateTests
{
    private static readonly SkillDefinition Fireball = SkillDefinition.ForSpell("fireball", new SpellDefinition("Fireball", 50, 75, DamageType.Fire, 10)) with
    {
        ManaCost = 2,
        CooldownSec = 0.25
    };

    private static readonly SkillDefinition StandardAttack = SkillDefinition.ForAttack("attack", AttackDefinition.Standard);

    private static readonly SkillDefinition Cleave = SkillDefinition.ForAttack("cleave", new AttackDefinition("Cleave", 120f)) with
    {
        ManaCost = 1,
        Delivery = SkillDelivery.WeaponSweep,
        Sweep = new SweepSettings(180f, 1.5f)
    };

    [Test]
    public void GenugManaUndKeineAbklingzeit_IstBereit()
        => Assert.That(SkillGate.Check(Fireball, new SkillCooldowns(), 2), Is.EqualTo(SkillUseCheck.Ready));

    [Test]
    public void ZuWenigMana_WirdAbgelehnt()
        => Assert.That(SkillGate.Check(Fireball, new SkillCooldowns(), 1.9f), Is.EqualTo(SkillUseCheck.NotEnoughMana));

    [Test]
    public void LaufendeAbklingzeit_WirdAbgelehnt()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball.Id, Fireball.CooldownSec);

        Assert.That(SkillGate.Check(Fireball, cooldowns, 100), Is.EqualTo(SkillUseCheck.OnCooldown));
    }

    [Test]
    public void AbklingzeitWirdVorDemManaGemeldet()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball.Id, Fireball.CooldownSec);

        Assert.That(SkillGate.Check(Fireball, cooldowns, 0), Is.EqualTo(SkillUseCheck.OnCooldown));
    }

    [Test]
    public void SkillOhneKosten_GehtAuchOhneMana()
        => Assert.That(SkillGate.Check(StandardAttack, new SkillCooldowns(), 0), Is.EqualTo(SkillUseCheck.Ready));

    [Test]
    public void EinheitOhneMana_ZahltNichts()
        => Assert.That(SkillGate.Check(Fireball, new SkillCooldowns(), SkillGate.UnlimitedMana), Is.EqualTo(SkillUseCheck.Ready));

    [Test]
    public void NahkampfSkillMitBogen_WirdAbgelehnt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SkillGate.Check(Cleave, new SkillCooldowns(), 10, true), Is.EqualTo(SkillUseCheck.NeedsMeleeWeapon));
            Assert.That(SkillGate.Check(Cleave, new SkillCooldowns(), 10, false), Is.EqualTo(SkillUseCheck.Ready));
            Assert.That(SkillGate.Check(Cleave, new SkillCooldowns(), 10), Is.EqualTo(SkillUseCheck.Ready), "ohne Angabe gilt eine Nahkampfwaffe");
        });
    }

    [Test]
    public void FalscheWaffeWirdVorAbklingzeitUndManaGemeldet()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Cleave.Id, 5);

        Assert.That(SkillGate.Check(Cleave, cooldowns, 0, true), Is.EqualTo(SkillUseCheck.NeedsMeleeWeapon));
    }

    [Test]
    public void AttackUndZauber_PassenZuJederWaffe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SkillGate.FitsWeapon(StandardAttack, true), Is.True, "der Bogen schießt");
            Assert.That(SkillGate.FitsWeapon(Fireball, true), Is.True);
            Assert.That(SkillGate.FitsWeapon(Cleave, true), Is.False);
            Assert.That(SkillGate.FitsWeapon(Cleave, false), Is.True);
        });
    }
}
