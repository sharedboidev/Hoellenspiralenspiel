using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Rng;

[TestFixture]
public class RandomSourceExtensionsTests
{
    [TestCase(0f, 2)]
    [TestCase(0.49f, 3)]
    [TestCase(0.5f, 4)]
    [TestCase(0.999f, 5)]
    public void NextInt_VerteiltDenWurfAufDieSpanne(float roll, int expected)
        => Assert.That(new FixedRandom(roll).NextInt(2, 6), Is.EqualTo(expected));

    [Test]
    public void NextInt_ErreichtDieObergrenzeNie()
        => Assert.That(new FixedRandom(1f).NextInt(2, 6), Is.EqualTo(5));

    [TestCase(3, 3)]
    [TestCase(3, 1)]
    public void NextInt_OhneSpanne_LiefertDieUntergrenze(int min, int max)
        => Assert.That(new FixedRandom(0.7f).NextInt(min, max), Is.EqualTo(min));

    [Test]
    public void NextInt_TrifftJedenWertDerSpanne()
    {
        var random = new SeededRandom(5);
        var values = Enumerable.Range(0, 500).Select(_ => random.NextInt(-2, 3)).Distinct().ToArray();

        Assert.That(values, Is.EquivalentTo(new[] { -2, -1, 0, 1, 2 }));
    }
}
