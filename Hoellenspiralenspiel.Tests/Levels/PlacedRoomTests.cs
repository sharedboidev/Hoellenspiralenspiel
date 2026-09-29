using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class PlacedRoomTests
{
    private static readonly RoomBlueprint Wide = new()
    {
        Id     = "wide",
        Width  = 5,
        Height = 2,
        Doors  = [new DoorSpot(CellSide.North, 0), new DoorSpot(CellSide.East, 1), new DoorSpot(CellSide.South, 3), new DoorSpot(CellSide.West, 0)]
    };

    [Test]
    public void OhneDrehung_LiegenDieTuerenWieInDerVorlage()
    {
        var room = new PlacedRoom(0, Wide, 10, 20, 0);

        Assert.That(room.Rect, Is.EqualTo(new CellRect(10, 20, 5, 2)));
        Assert.That(room.Doors.Select(door => door.Inside), Is.EqualTo(new[] { new Cell(10, 20), new Cell(14, 21), new Cell(13, 21), new Cell(10, 20) }));
        Assert.That(room.Doors.Select(door => door.Side), Is.EqualTo(new[] { CellSide.North, CellSide.East, CellSide.South, CellSide.West }));
    }

    [Test]
    public void EineVierteldrehung_TauschtBreiteUndHoehe()
    {
        var room = new PlacedRoom(0, Wide, 10, 20, 1);

        Assert.That(room.Rect, Is.EqualTo(new CellRect(10, 20, 2, 5)));
    }

    [Test]
    public void EineVierteldrehung_DrehtDieTuerenImUhrzeigersinn()
    {
        var room = new PlacedRoom(0, Wide, 10, 20, 1);

        Assert.That(room.Doors.Select(door => door.Side), Is.EqualTo(new[] { CellSide.East, CellSide.South, CellSide.West, CellSide.North }));
        Assert.That(room.Doors.Select(door => door.Inside), Is.EqualTo(new[] { new Cell(11, 20), new Cell(10, 24), new Cell(10, 23), new Cell(11, 20) }));
    }

    [Test]
    public void EineHalbeDrehung_SpiegeltDieTuerenUmDieMitte()
    {
        var room = new PlacedRoom(0, Wide, 0, 0, 2);

        Assert.That(room.Rect, Is.EqualTo(new CellRect(0, 0, 5, 2)));
        Assert.That(room.Doors.Select(door => door.Side), Is.EqualTo(new[] { CellSide.South, CellSide.West, CellSide.North, CellSide.East }));
        Assert.That(room.Doors.Select(door => door.Inside), Is.EqualTo(new[] { new Cell(4, 1), new Cell(0, 0), new Cell(1, 0), new Cell(4, 1) }));
    }

    [Test]
    public void VierVierteldrehungen_ErgebenDieVorlage()
    {
        var plain  = new PlacedRoom(0, Wide, 3, 4, 0);
        var turned = new PlacedRoom(0, Wide, 3, 4, 4);

        Assert.That(turned.QuarterTurns, Is.EqualTo(0));
        Assert.That(turned.Doors, Is.EqualTo(plain.Doors));
    }

    [Test]
    public void TuerenLiegenInJederDrehungAmRand()
    {
        for (var turns = 0; turns < 4; turns++)
        {
            var room = new PlacedRoom(0, Wide, 7, 9, turns);

            foreach (var door in room.Doors)
            {
                Assert.That(room.Rect.Contains(door.Inside), Is.True, $"Drehung {turns}, Tür {door}");
                Assert.That(room.Rect.Contains(door.Outside), Is.False, $"Drehung {turns}, Tür {door}");
            }
        }
    }

    [Test]
    public void Verschieben_NimmtDieTuerenMit()
    {
        var room  = new PlacedRoom(3, Wide, 0, 0, 1);
        var moved = room.MoveBy(5, -2);

        Assert.That(moved.Index, Is.EqualTo(3));
        Assert.That(moved.Rect, Is.EqualTo(new CellRect(5, -2, 2, 5)));
        Assert.That(moved.Doors.Select(door => door.Inside), Is.EqualTo(room.Doors.Select(door => new Cell(door.Inside.X + 5, door.Inside.Y - 2))));
    }
}
