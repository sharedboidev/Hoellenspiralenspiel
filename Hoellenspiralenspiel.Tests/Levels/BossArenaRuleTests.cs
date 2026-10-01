using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class BossArenaRuleTests
{
    [Test]
    public void DieGitterSchliessenSichMitDemLebendenHeldenImRaumDesLebendenBosses()
    {
        Assert.That(BossArenaRule.IsSealed(true, true, true), Is.True);
    }

    [Test]
    public void OhneHeldenImRaumBleibenSieOffen()
    {
        Assert.That(BossArenaRule.IsSealed(false, true, true), Is.False);
    }

    [Test]
    public void EinToterHeldOeffnetSie()
    {
        Assert.That(BossArenaRule.IsSealed(true, false, true), Is.False);
    }

    [Test]
    public void EinToterBossOeffnetSie()
    {
        Assert.That(BossArenaRule.IsSealed(true, true, false), Is.False);
    }
}
