using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class AttackOrdersTests
{
    [Test]
    public void MitZiel_LaeuftDerHeldHin()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AttackOrders.Choose(true, true, false), Is.EqualTo(AttackOrder.Approach));
            Assert.That(AttackOrders.Choose(false, true, false), Is.EqualTo(AttackOrder.Approach));
        });
    }

    [Test]
    public void NahkampfOhneZiel_PassiertNichts()
        => Assert.That(AttackOrders.Choose(true, false, false), Is.EqualTo(AttackOrder.None));

    [Test]
    public void FernkampfOhneZiel_SchiesstAusDemStand()
        => Assert.That(AttackOrders.Choose(false, false, false), Is.EqualTo(AttackOrder.InPlace));

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Stehenbleiben_SchlaegtImmerAusDemStand(bool isMelee, bool hasTarget)
        => Assert.That(AttackOrders.Choose(isMelee, hasTarget, true), Is.EqualTo(AttackOrder.InPlace));
}
