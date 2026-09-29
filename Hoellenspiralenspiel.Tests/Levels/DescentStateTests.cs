using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Saving;
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
    public void DerAbstiegUeberstehtDasSpeichern()
    {
        var descent = new DescentState();
        var first   = CreateExplored(new Cell(1, 1), new Cell(2, 1));
        var second  = CreateExplored(new Cell(7, 5));
        var save    = new SaveGame();
        var loaded  = new DescentState();

        descent.Begin(4711);
        descent.GoTo(2);
        descent.Remember(1, first);
        descent.Remember(2, second);

        SaveGameMapper.CaptureDescent(descent, save);

        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var read), Is.True);
        Assert.That(SaveGameMapper.RestoreDescent(read, loaded), Is.True);

        Assert.That(loaded.Seed, Is.EqualTo(4711));
        Assert.That(loaded.Depth, Is.EqualTo(2));
        Assert.That(loaded.GetRevealed(1), Is.EqualTo(first.Encode()));
        Assert.That(loaded.GetRevealed(2), Is.EqualTo(second.Encode()));
    }

    [Test]
    public void EinAlterSpielstandHatKeinenAbstieg()
    {
        var descent = new DescentState();

        descent.Begin(5);
        descent.GoTo(3);

        Assert.That(SaveGameSerializer.TryDeserialize("{\"Version\":1,\"Character\":{\"Level\":3}}", out var read), Is.True);
        Assert.That(SaveGameMapper.RestoreDescent(read, descent), Is.False);
        Assert.That(descent.Depth, Is.EqualTo(3));
    }
}
