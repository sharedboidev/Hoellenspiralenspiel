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
}
