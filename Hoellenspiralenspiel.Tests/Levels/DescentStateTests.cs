using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class DescentStateTests
{
    private static ExplorationMap CreateExplored(params Cell[] cells)
    {
        var map = new ExplorationMap(8, 6);

        foreach (var cell in cells)
            map.Reveal(cell);

        return map;
    }

    [Test]
    public void JedeTiefeHatIhrenEigenenSeed()
    {
        var seeds = Enumerable.Range(0, 200).Select(depth => DescentState.GetSeedOf(2026, depth)).Distinct().Count();

        Assert.That(seeds, Is.EqualTo(200));
    }

    [Test]
    public void JederAbstiegHatEigeneSeeds()
    {
        var seeds = Enumerable.Range(0, 200).Select(seed => DescentState.GetSeedOf(seed, 1)).Distinct().Count();

        Assert.That(seeds, Is.EqualTo(200));
    }

    [Test]
    public void DerSeedEinerTiefeBleibtGleich()
    {
        var descent = new DescentState();

        descent.Begin(99);

        var first = descent.GetSeedOf(3);

        descent.GoTo(5);

        Assert.That(first, Is.EqualTo(DescentState.GetSeedOf(99, 3)));
        Assert.That(descent.GetSeedOf(3), Is.EqualTo(first));
    }

    [Test]
    public void EinNeuerAbstiegVergisstDieAltenKarten()
    {
        var descent = new DescentState();

        descent.Begin(1);
        descent.GoTo(2);
        descent.Remember(2, CreateExplored(new Cell(1, 1)));
        descent.Begin(2);

        Assert.That(descent.Depth, Is.EqualTo(0));
        Assert.That(descent.IsBelowGround, Is.False);
        Assert.That(descent.GetRevealed(2), Is.Null);
    }

    [Test]
    public void EineLeereKarteWirdNichtGemerkt()
    {
        var descent = new DescentState();

        descent.Remember(1, CreateExplored());

        Assert.That(descent.RevealedByDepth, Is.Empty);
    }

    [Test]
    public void EinNeuerAbstiegBehaeltDieCheckpoints()
    {
        var descent = new DescentState();

        descent.Begin(1);
        descent.GoTo(3);
        descent.Begin(2);

        Assert.That(descent.DeepestDepth, Is.EqualTo(3));
        Assert.That(descent.HasReached(3), Is.True);
        Assert.That(descent.HasReached(4), Is.False);
    }

    [Test]
    public void DieErsteEbeneStehtJedemOffen()
    {
        var descent = new DescentState();

        Assert.That(descent.HasReached(1), Is.True);
        Assert.That(descent.HasReached(2), Is.False);
        Assert.That(descent.HasReached(0), Is.False);
    }

    [Test]
    public void DerWegHinaufSenktDenCheckpointNicht()
    {
        var descent = new DescentState();

        descent.Begin(1);
        descent.GoTo(3);
        descent.GoTo(2);
        descent.Leave();

        Assert.That(descent.Depth, Is.EqualTo(0));
        Assert.That(descent.DeepestDepth, Is.EqualTo(3));
    }

    [Test]
    public void TotBleibtTotBisZumNeuenAbstieg()
    {
        var descent = new DescentState();

        descent.Begin(1);

        Assert.That(descent.RememberKill(2, 7), Is.True);
        Assert.That(descent.RememberKill(2, 7), Is.False);
        Assert.That(descent.IsKilled(2, 7), Is.True);
        Assert.That(descent.IsKilled(1, 7), Is.False);
        Assert.That(descent.IsKilled(2, 8), Is.False);

        descent.Begin(2);

        Assert.That(descent.IsKilled(2, 7), Is.False);
        Assert.That(descent.GetKilled(2), Is.Empty);
    }

    [Test]
    public void BeschworeneGegnerZaehlenNicht()
    {
        var descent = new DescentState();

        descent.Begin(1);

        Assert.That(descent.RememberKill(1, -1), Is.False);
        Assert.That(descent.RememberKill(0, 3), Is.False);
        Assert.That(descent.DepthsWithKills, Is.Empty);
    }
}
