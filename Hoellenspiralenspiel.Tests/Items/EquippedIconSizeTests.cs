using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class EquippedIconSizeTests
{
    [Test]
    public void Get_SchwertImWaffenplatz_BleibtEineZelleBreit()
        => Assert.That(EquippedIconSize.Get(TestItems.Sword, 2, 6, 80, 8), Is.EqualTo((72, 232)));

    [Test]
    public void Get_StabImWaffenplatz_BleibtVierZellenHoch()
        => Assert.That(EquippedIconSize.Get(TestItems.Staff, 2, 6, 80, 8), Is.EqualTo((72, 312)));

    [Test]
    public void Get_PassendesItem_FuelltDenPlatzBisAufDenRahmen()
        => Assert.That(EquippedIconSize.Get(TestItems.Helmet, 2, 2, 80, 8), Is.EqualTo((152, 152)));

    [Test]
    public void Get_SchildImWaffenplatz_BleibtQuadratisch()
        => Assert.That(EquippedIconSize.Get(TestItems.Shield, 2, 6, 80, 8), Is.EqualTo((152, 152)));

    [Test]
    public void Get_ZuGrossesItem_WirdAufDenPlatzBegrenzt()
        => Assert.That(EquippedIconSize.Get(TestItems.Helmet, 1, 1, 78, 8), Is.EqualTo((70, 70)));
}
