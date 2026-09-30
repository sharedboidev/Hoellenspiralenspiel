using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class GoldDropRuleTests
{
    private const float Lucky   = 0f;
    private const float Unlucky = 0.999f;

    [Test]
    public void OhneGlueck_FaelltNichts()
        => Assert.That(GoldDropRule.Roll(1, 4, 1, 0.15f, 1f, 50f, new FixedRandom(0.5f, 0.6f)), Is.Zero);

    [Test]
    public void MitGlueck_FaelltEinBetragAusDemBereich()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GoldDropRule.Roll(2, 6, 1, 0.15f, 1f, 50f, new FixedRandom(0f, Lucky, 0f)), Is.EqualTo(2));
            Assert.That(GoldDropRule.Roll(2, 6, 1, 0.15f, 1f, 50f, new FixedRandom(0f, Lucky, Unlucky)), Is.EqualTo(6));
        });
    }

    [Test]
    public void HundertProzent_FaelltImmer()
        => Assert.That(GoldDropRule.Roll(3, 3, 1, 0f, 1f, 100f, new FixedRandom(Unlucky)), Is.EqualTo(3));

    [Test]
    public void NullProzent_FaelltNie()
        => Assert.That(GoldDropRule.Roll(3, 3, 1, 0f, 1f, 0f, new FixedRandom(Lucky)), Is.Zero);

    [TestCase(1, 10)]
    [TestCase(2, 12)]
    [TestCase(11, 30)]
    public void DasLevelLaesstDenBetragWachsen(int level, int expected)
        => Assert.That(GoldDropRule.Roll(10, 10, level, 0.2f, 1f, 100f, new FixedRandom(0f)), Is.EqualTo(expected));

    [Test]
    public void DieSeltenheitVervielfachtDenBetrag()
        => Assert.That(GoldDropRule.Roll(10, 10, 1, 0.2f, 3f, 100f, new FixedRandom(0f)), Is.EqualTo(30));

    [Test]
    public void WerEtwasFallenLaesst_LaesstMindestensEineMuenzeFallen()
        => Assert.That(GoldDropRule.Roll(1, 1, 1, 0f, 0.1f, 100f, new FixedRandom(0f)), Is.EqualTo(1));

    [Test]
    public void OhneBereich_FaelltNichts()
        => Assert.That(GoldDropRule.Roll(0, 0, 5, 0.2f, 3f, 100f, new FixedRandom(0f)), Is.Zero);

    [Test]
    public void VertauschteGrenzen_GeltenTrotzdem()
        => Assert.That(GoldDropRule.Roll(6, 2, 1, 0f, 1f, 100f, new FixedRandom(0f, Lucky, 0f)), Is.EqualTo(2));

    [Test]
    public void EinLevelUnterEins_ZaehltAlsEins()
        => Assert.That(GoldDropRule.GetLevelFactor(-4, 0.2f), Is.EqualTo(1f));

    [Test]
    public void GleicherSeed_ErgibtGleichesGold()
    {
        var first  = new SeededRandom(99);
        var second = new SeededRandom(99);

        for (var i = 0; i < 50; i++)
            Assert.That(GoldDropRule.Roll(1, 9, 7, 0.15f, 1f, 60f, first), Is.EqualTo(GoldDropRule.Roll(1, 9, 7, 0.15f, 1f, 60f, second)));
    }

    [Test]
    public void DieChanceStimmtUeberVieleWuerfe()
    {
        var random = new SeededRandom(4242);
        var drops  = 0;

        for (var i = 0; i < 10000; i++)
        {
            if (GoldDropRule.Roll(1, 4, 1, 0f, 1f, 40f, random) > 0)
                drops++;
        }

        Assert.That(drops, Is.InRange(3800, 4200));
    }
}
