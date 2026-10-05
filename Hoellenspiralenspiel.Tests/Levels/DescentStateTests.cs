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

        Assert.That(descent.RevealedByLocation, Is.Empty);
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
        Assert.That(descent.LocationsWithKills, Is.Empty);
    }

    [Test]
    public void EinAndererInhaltsstandMachtDenAbstiegVeraltet()
    {
        var descent = new DescentState();

        descent.Begin(1, 2);

        Assert.That(descent.ContentVersion, Is.EqualTo(2));
        Assert.That(descent.IsStale(2), Is.False);
        Assert.That(descent.IsStale(3), Is.True);
    }

    [Test]
    public void OhneBeginnIstEinAbstiegNieVeraltet()
    {
        var descent = new DescentState();

        Assert.That(descent.IsStale(1), Is.False);
        Assert.That(descent.IsStale(7), Is.False);
    }

    [Test]
    public void EinUnbekannterInhaltsstandGiltAlsPassendUndUebernimmtDenAktuellen()
    {
        var descent = new DescentState();

        descent.Begin(1);

        Assert.That(descent.IsStale(5), Is.False);

        descent.AdoptContentVersion(5);

        Assert.That(descent.ContentVersion, Is.EqualTo(5));
        Assert.That(descent.IsStale(5), Is.False);
        Assert.That(descent.IsStale(6), Is.True);

        descent.AdoptContentVersion(9);

        Assert.That(descent.ContentVersion, Is.EqualTo(5));
    }

    [Test]
    public void EinNeuerAbstiegNimmtDenNeuenStandUndBehaeltDieCheckpoints()
    {
        var descent = new DescentState();

        descent.Begin(1, 1);
        descent.GoTo(3);
        descent.RememberKill(2, 4);
        descent.Begin(1, 2);

        Assert.That(descent.ContentVersion, Is.EqualTo(2));
        Assert.That(descent.Depth, Is.EqualTo(0));
        Assert.That(descent.DeepestDepth, Is.EqualTo(3));
        Assert.That(descent.IsKilled(2, 4), Is.False);
    }

    [Test]
    public void KarteUndTote_HaengenAmOrt_EinDungeonIstEinEigenerOrt()
    {
        var descent = new DescentState();
        var field   = LocationKey.Of(2);
        var dungeon = field.InDungeon(0, 1);
        var map     = CreateExplored(new Cell(3, 3));

        descent.Begin(1);
        descent.Remember(dungeon, map);
        descent.RememberKill(dungeon, 5);

        Assert.Multiple(() =>
        {
            Assert.That(descent.GetRevealed(dungeon), Is.EqualTo(map.Encode()));
            Assert.That(descent.GetRevealed(field), Is.Null);
            Assert.That(descent.IsKilled(dungeon, 5), Is.True);
            Assert.That(descent.IsKilled(field, 5), Is.False);
            Assert.That(descent.LocationsWithKills, Is.EqualTo(new[] { dungeon }));
        });
    }

    [Test]
    public void TiefeNUndFlaecheNSindDerselbeOrt()
    {
        var descent = new DescentState();

        descent.Begin(1);
        descent.RememberKill(2, 4);
        descent.Remember(LocationKey.Of(3), CreateExplored(new Cell(1, 2)));

        Assert.Multiple(() =>
        {
            Assert.That(descent.IsKilled(LocationKey.Of(2), 4), Is.True);
            Assert.That(descent.GetKilled(LocationKey.Of(2)), Is.EqualTo(new[] { 4 }));
            Assert.That(descent.GetRevealed(3), Is.Not.Null);
        });
    }

    //Die Ebenen der alten Kreise behalten so ihre Seeds aus den Spielständen vor Version 6
    [Test]
    public void EineFlaecheNimmtDenSeedIhrerTiefe_EinDungeonEinenEigenen()
    {
        var descent = new DescentState();

        descent.Begin(4711);

        var field    = descent.GetSeedOf(LocationKey.Of(2));
        var first    = descent.GetSeedOf(LocationKey.Of(2).InDungeon(0, 1));
        var second   = descent.GetSeedOf(LocationKey.Of(2).InDungeon(0, 2));
        var neighbor = descent.GetSeedOf(LocationKey.Of(2).InDungeon(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(field, Is.EqualTo(descent.GetSeedOf(2)));
            Assert.That(new[] { field, first, second, neighbor }.Distinct().Count(), Is.EqualTo(4));
            Assert.That(descent.GetSeedOf(LocationKey.Of(2).InDungeon(0, 1)), Is.EqualTo(first));
        });
    }
}
