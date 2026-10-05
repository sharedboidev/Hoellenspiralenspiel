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

    [Test]
    public void OhneAngriff_LaeuftDerHeldMitDerRichtung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AttackOrders.MovementCancels(true, false, false), Is.True);
            Assert.That(AttackOrders.MovementCancels(true, false, true), Is.True);
            Assert.That(AttackOrders.MovementCancels(false, false, false), Is.False, "ohne Richtung gibt es nichts abzubrechen");
        });
    }

    [Test]
    public void GehalteneRichtung_WartetAufDenAngriff()
        => Assert.That(AttackOrders.MovementCancels(true, true, false), Is.False);

    [Test]
    public void NeuGedrueckteRichtung_BrichtDenAngriffAb()
        => Assert.That(AttackOrders.MovementCancels(true, true, true), Is.True);
}
