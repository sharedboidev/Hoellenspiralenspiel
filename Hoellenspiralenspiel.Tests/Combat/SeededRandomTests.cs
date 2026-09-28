using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class SeededRandomTests
{
    private static float[] Draw(IRandomSource random, int amount)
        => Enumerable.Range(0, amount).Select(_ => random.NextFloat()).ToArray();

    [Test]
    public void GleicherSeed_ErgibtGleicheFolge()
    {
        var first  = Draw(new SeededRandom(7), 100);
        var second = Draw(new SeededRandom(7), 100);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void AndererSeed_ErgibtAndereFolge()
        => Assert.That(Draw(new SeededRandom(7), 100), Is.Not.EqualTo(Draw(new SeededRandom(8), 100)));

    [Test]
    public void Wuerfe_LiegenVonNullBisUnterEins()
        => Assert.That(Draw(new SeededRandom(3), 10000), Is.All.InRange(0f, 1f).And.All.LessThan(1f));

    [Test]
    public void Prozentwurf_LiegtVonNullBisUnterHundert()
    {
        var random = new SeededRandom(3);

        for (var i = 0; i < 10000; i++)
            Assert.That(random.NextPercent(), Is.InRange(0f, 100f).And.LessThan(100f));
    }

    [Test]
    public void Bereichswurf_BleibtImBereich()
    {
        var random = new SeededRandom(3);

        for (var i = 0; i < 10000; i++)
            Assert.That(random.NextRange(4f, 9f), Is.InRange(4f, 9f));
    }

    [Test]
    public void Reseed_ErsetztDieGemeinsameQuelle()
    {
        GameRandom.Reseed(99);

        var first = Draw(GameRandom.Shared, 10);

        GameRandom.Reseed(99);

        Assert.Multiple(() =>
        {
            Assert.That(GameRandom.Shared.Seed, Is.EqualTo(99));
            Assert.That(Draw(GameRandom.Shared, 10), Is.EqualTo(first));
        });
    }
}
