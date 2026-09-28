using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

[TestFixture]
public class EnemyRarityTests
{
    private static readonly EnemyRarityChances Chances = new(10f, 4f);

    [TestCase(0, EnemyRarity.Normal)]
    [TestCase(1, EnemyRarity.Elite)]
    [TestCase(2, EnemyRarity.Elite)]
    [TestCase(3, EnemyRarity.RareElite)]
    [TestCase(5, EnemyRarity.RareElite)]
    [TestCase(9, EnemyRarity.RareElite)]
    [TestCase(-1, EnemyRarity.Normal)]
    public void Seltenheit_FolgtAusDerZahlDerMods(int modCount, EnemyRarity expected)
        => Assert.That(EnemyRarityRules.FromModCount(modCount), Is.EqualTo(expected));

    [TestCase(0.00f, 0.0f, 3)]
    [TestCase(0.039f, 0.34f, 4)]
    [TestCase(0.039f, 0.999f, 5)]
    [TestCase(0.04f, 0.0f, 1)]
    [TestCase(0.139f, 0.5f, 2)]
    [TestCase(0.139f, 0.999f, 2)]
    [TestCase(0.14f, 0.5f, 0)]
    [TestCase(0.99f, 0.5f, 0)]
    public void ZahlDerMods_FolgtDenChancen(float rarityRoll, float countRoll, int expected)
    {
        var random = new FixedRandom(0.5f, rarityRoll, countRoll);

        Assert.That(EnemyRarityRules.RollModCount(Chances, random), Is.EqualTo(expected));
    }

    [TestCase(0.0f)]
    [TestCase(0.1f)]
    [TestCase(0.9f)]
    public void JederWurf_VerbrauchtZweiZahlen(float rarityRoll)
    {
        var random = new FixedRandom(0.5f, rarityRoll);

        EnemyRarityRules.RollModCount(Chances, random);

        Assert.That(random.Draws, Is.EqualTo(2));
    }

    [Test]
    public void OhneChancen_BleibtJederGegnerNormal()
    {
        var random = new SeededRandom(7);

        for (var i = 0; i < 500; i++)
            Assert.That(EnemyRarityRules.RollModCount(new EnemyRarityChances(0f, 0f), random), Is.Zero);
    }

    [Test]
    public void VieleWuerfe_TreffenDieChancen()
    {
        var random    = new SeededRandom(1234);
        var elite     = 0;
        var rareElite = 0;

        const int rolls = 100_000;

        for (var i = 0; i < rolls; i++)
        {
            switch (EnemyRarityRules.FromModCount(EnemyRarityRules.RollModCount(Chances, random)))
            {
                case EnemyRarity.Elite:
                    elite++;

                    break;
                case EnemyRarity.RareElite:
                    rareElite++;

                    break;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(elite / (float)rolls, Is.EqualTo(0.10f).Within(0.01f));
            Assert.That(rareElite / (float)rolls, Is.EqualTo(0.04f).Within(0.01f));
        });
    }
}
