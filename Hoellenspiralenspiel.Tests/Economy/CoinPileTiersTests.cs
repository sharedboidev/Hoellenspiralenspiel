using Hoellenspiralenspiel.Scripts.Core.Economy;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class CoinPileTiersTests
{
    [Test]
    public void EsGibtNeunStufen()
        => Assert.That(CoinPileTiers.DefaultThresholds, Has.Count.EqualTo(CoinPileTiers.Count));

    [Test]
    public void DieSchwellenSteigen()
        => Assert.That(CoinPileTiers.DefaultThresholds, Is.Ordered.Ascending.And.Unique);

    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 4)]
    public void BisFuenfMuenzen_LiegtJedeEinzeln(int amount, int expectedTier)
        => Assert.That(CoinPileTiers.GetTier(amount, CoinPileTiers.DefaultThresholds), Is.EqualTo(expectedTier));

    [TestCase(6, 5)]
    [TestCase(19, 5)]
    [TestCase(20, 6)]
    [TestCase(49, 6)]
    [TestCase(50, 7)]
    [TestCase(149, 7)]
    public void Danach_WachsenDieStapel(int amount, int expectedTier)
        => Assert.That(CoinPileTiers.GetTier(amount, CoinPileTiers.DefaultThresholds), Is.EqualTo(expectedTier));

    [TestCase(150)]
    [TestCase(100000)]
    [TestCase(int.MaxValue)]
    public void DieLetzteStufeIstDieGrenze(int amount)
        => Assert.That(CoinPileTiers.GetTier(amount, CoinPileTiers.DefaultThresholds), Is.EqualTo(CoinPileTiers.Count - 1));

    [TestCase(0)]
    [TestCase(-7)]
    public void OhneBetrag_GibtEsKeineStufe(int amount)
        => Assert.That(CoinPileTiers.GetTier(amount, CoinPileTiers.DefaultThresholds), Is.EqualTo(-1));

    [Test]
    public void OhneSchwellen_GeltenDieStandardwerte()
        => Assert.That(CoinPileTiers.GetTier(25, null), Is.EqualTo(6));

    [Test]
    public void EigeneSchwellen_Gelten()
    {
        int[] thresholds = [10, 100];

        Assert.Multiple(() =>
        {
            Assert.That(CoinPileTiers.GetTier(5, thresholds), Is.EqualTo(-1));
            Assert.That(CoinPileTiers.GetTier(10, thresholds), Is.Zero);
            Assert.That(CoinPileTiers.GetTier(99, thresholds), Is.Zero);
            Assert.That(CoinPileTiers.GetTier(100, thresholds), Is.EqualTo(1));
        });
    }
}
