using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class ExplorationMapTests
{
    //  0123456789
    // 1 AAA   BBB
    // 2 AAA...BBB
    // 3 AAA.  BBB
    private static LevelLayout CreateTwoRooms()
    {
        var left   = new PlacedRoom(0, TestRooms.Chamber, 1, 1, 0);
        var right  = new PlacedRoom(1, TestRooms.Chamber, 7, 1, 0);
        var layout = new LevelLayout(1, 11, 5, [left, right], 0, 1);

        for (var x = 4; x <= 6; x++)
            layout.DigCorridor(new Cell(x, 2));

        layout.DigCorridor(new Cell(4, 3));

        layout.Open(left.Doors.Single(door => door.Side == CellSide.East));
        layout.Open(right.Doors.Single(door => door.Side == CellSide.West));

        return layout;
    }

    [Test]
    public void AmAnfangIstNichtsAufgedeckt()
    {
        var map = new ExplorationMap(11, 5);

        Assert.That(map.RevealedCount, Is.EqualTo(0));
        Assert.That(map.IsRevealed(new Cell(2, 2)), Is.False);
    }

    [Test]
    public void DerUmkreisWirdAufgedeckt()
    {
        var map   = new ExplorationMap(11, 5);
        var found = map.RevealAround(CreateTwoRooms(), new Cell(2, 2), 1.5f);

        Assert.That(found, Is.EqualTo(9));
        Assert.That(map.IsRevealed(new Cell(1, 1)), Is.True);
        Assert.That(map.IsRevealed(new Cell(3, 3)), Is.True);
        Assert.That(map.IsRevealed(new Cell(4, 2)), Is.False);
    }

    [Test]
    public void DurchDieTuerSiehtManInDenGang()
    {
        var map = new ExplorationMap(11, 5);

        map.RevealAround(CreateTwoRooms(), new Cell(3, 2), 1.5f);

        Assert.That(map.IsRevealed(new Cell(4, 2)), Is.True);
    }

    [Test]
    public void HinterDerMauerBleibtDieKarteDunkel()
    {
        var map = new ExplorationMap(11, 5);

        map.RevealAround(CreateTwoRooms(), new Cell(3, 3), 1f);

        Assert.That(map.IsRevealed(new Cell(3, 2)), Is.True);
        Assert.That(map.IsRevealed(new Cell(4, 3)), Is.False);
    }

    [Test]
    public void ImFelsWirdNichtsAufgedeckt()
    {
        var map = new ExplorationMap(11, 5);

        Assert.That(map.RevealAround(CreateTwoRooms(), new Cell(0, 0), 5f), Is.EqualTo(0));
        Assert.That(map.RevealedCount, Is.EqualTo(0));
    }

    [Test]
    public void GemeldetWirdNurNeues()
    {
        var map     = new ExplorationMap(11, 5);
        var layout  = CreateTwoRooms();
        var changes = 0;

        map.Changed += () => changes++;

        Assert.That(map.RevealAround(layout, new Cell(2, 2), 1.5f), Is.EqualTo(9));
        Assert.That(map.RevealAround(layout, new Cell(2, 2), 1.5f), Is.EqualTo(0));
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void DerStandUeberstehtDasSpeichern()
    {
        var layout = CreateTwoRooms();
        var map    = new ExplorationMap(11, 5);
        var loaded = new ExplorationMap(11, 5);

        map.RevealAround(layout, new Cell(3, 2), 2f);

        Assert.That(loaded.TryRestore(map.Encode()), Is.True);
        Assert.That(loaded.RevealedCount, Is.EqualTo(map.RevealedCount));

        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 11; x++)
                Assert.That(loaded.IsRevealed(new Cell(x, y)), Is.EqualTo(map.IsRevealed(new Cell(x, y))), $"Zelle {x},{y}");
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("kein base64 !!")]
    [TestCase("AAAA")]
    public void EinFremderStandWirdAbgelehnt(string encoded)
    {
        var map = new ExplorationMap(11, 5);

        map.Reveal(new Cell(2, 2));

        Assert.That(map.TryRestore(encoded), Is.False);
        Assert.That(map.IsRevealed(new Cell(2, 2)), Is.True);
    }

    [Test]
    public void EinStandEinerAnderenGroesseWirdAbgelehnt()
    {
        var small = new ExplorationMap(4, 4);

        small.Reveal(new Cell(1, 1));

        Assert.That(new ExplorationMap(11, 5).TryRestore(small.Encode()), Is.False);
    }
}
