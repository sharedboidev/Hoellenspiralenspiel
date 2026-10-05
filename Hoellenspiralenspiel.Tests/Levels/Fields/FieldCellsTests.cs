using System;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

//Die Zellarten der Flächen im Grundriss, den Karte und Erkundung lesen
[TestFixture]
public class FieldCellsTests
{
    private static readonly RoomBlueprint Box = TestFields.Ruin("box", 3, 3, new DoorSpot(CellSide.West, 1));

    //10 × 5 Zellen Boden. Die Ruine steht bei x 6 bis 8 und y 1 bis 3, ihre Tür liegt im Westen bei (6, 2).
    //Eine Spalte Hindernisse bei x 3 trennt die Fläche, außer mit Lücke in Zeile 2
    private static LevelLayout CreateField(bool withGap, bool withDoor)
    {
        var ruin   = new PlacedRoom(0, Box, 6, 1, 0);
        var layout = new LevelLayout(0, 10, 5, [ruin], LevelLayout.NoRoom, LevelLayout.NoRoom);

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                if (layout.GetKind(new Cell(x, y)) != CellKind.Room)
                    layout.Paint(new Cell(x, y), x == 3 && (!withGap || y != 2) ? CellKind.Obstacle : CellKind.Ground);
            }
        }

        if (withDoor)
            layout.Open(ruin.Doors[0]);

        return layout;
    }

    [Test]
    public void BodenIstBegehbar_HindernisUndAbgrundNicht()
    {
        var layout = CreateField(true, true);

        layout.Paint(new Cell(0, 0), CellKind.Void);

        Assert.Multiple(() =>
        {
            Assert.That(layout.IsFloor(new Cell(1, 1)), Is.True);
            Assert.That(layout.IsFloor(new Cell(3, 0)), Is.False, "Hindernis");
            Assert.That(layout.IsFloor(new Cell(0, 0)), Is.False, "Abgrund");
            Assert.That(layout.CanStep(new Cell(2, 0), CellSide.East), Is.False, "Ins Hindernis");
            Assert.That(layout.CanStep(new Cell(1, 0), CellSide.West), Is.False, "In den Abgrund");
            Assert.That(layout.FindReachable(new Cell(3, 0)), Is.Empty, "Vom Hindernis aus geht nichts");
        });
    }

    [Test]
    public void ZwischenBodenzellenStehtKeineMauer_ZwischenBodenUndRuineNurOhneTuer()
    {
        var layout = CreateField(true, true);

        Assert.Multiple(() =>
        {
            Assert.That(layout.HasWall(new Cell(1, 1), CellSide.East), Is.False);
            Assert.That(layout.HasWall(new Cell(5, 2), CellSide.East), Is.False, "Durch die Tür");
            Assert.That(layout.HasWall(new Cell(5, 1), CellSide.East), Is.True, "Neben der Tür");
            Assert.That(layout.HasWall(new Cell(9, 2), CellSide.West), Is.True, "Rückseite der Ruine");
        });
    }

    [Test]
    public void FreierBodenGehoertWieEinGangZuKeinemRaum()
    {
        var layout = CreateField(true, true);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetRegion(new Cell(1, 1)), Is.EqualTo(LevelLayout.Corridor));
            Assert.That(layout.GetRegion(new Cell(3, 0)), Is.EqualTo(LevelLayout.Rock));
            Assert.That(layout.GetRegion(new Cell(7, 2)), Is.EqualTo(0));
        });
    }

    [Test]
    public void ZellenEinerVorlage_LassenSichNichtUebermalen()
    {
        var layout = CreateField(true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => layout.Paint(new Cell(7, 2), CellKind.Ground));
            Assert.Throws<ArgumentException>(() => layout.Paint(new Cell(1, 1), CellKind.Room));
        });
    }

    [Test]
    public void ErkundungUeberBoden_StopptAnHindernissen()
    {
        var layout = CreateField(false, true);
        var map    = new ExplorationMap(layout.Width, layout.Height);

        map.RevealAround(layout, new Cell(1, 2), 20f);

        Assert.Multiple(() =>
        {
            Assert.That(map.IsRevealed(new Cell(2, 4)), Is.True);
            Assert.That(map.IsRevealed(new Cell(3, 2)), Is.False, "Das Hindernis selbst");
            Assert.That(map.IsRevealed(new Cell(4, 2)), Is.False, "Dahinter");
            Assert.That(map.RevealedCount, Is.EqualTo(15));
        });
    }

    [Test]
    public void ErkundungUeberBoden_GehtDurchDieTuerInDieRuine_AberNichtDurchIhreMauern()
    {
        var withDoor    = new ExplorationMap(10, 5);
        var withoutDoor = new ExplorationMap(10, 5);

        withDoor.RevealAround(CreateField(true, true), new Cell(1, 2), 20f);
        withoutDoor.RevealAround(CreateField(true, false), new Cell(1, 2), 20f);

        Assert.Multiple(() =>
        {
            Assert.That(withDoor.IsRevealed(new Cell(4, 2)), Is.True, "Durch die Lücke");
            Assert.That(withDoor.IsRevealed(new Cell(7, 2)), Is.True, "Durch die Tür in die Ruine");
            Assert.That(withDoor.IsRevealed(new Cell(9, 2)), Is.True, "Um die Ruine herum");
            Assert.That(withoutDoor.IsRevealed(new Cell(9, 2)), Is.True, "Um die Ruine herum");
            Assert.That(withoutDoor.IsRevealed(new Cell(7, 2)), Is.False, "Ohne Tür bleibt die Ruine dunkel");
        });
    }

    [Test]
    public void BodenUndHindernisse_ZerfallenInRechteckeOhneLueckeUndUeberlappung()
    {
        var layout = CreateField(false, true);
        var rects  = layout.GetRects(CellKind.Ground, CellKind.Obstacle);
        var count  = new int[layout.Width, layout.Height];

        foreach (var rect in rects)
        {
            for (var y = rect.Y; y < rect.Bottom; y++)
            {
                for (var x = rect.X; x < rect.Right; x++)
                    count[x, y]++;
            }
        }

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                var expected = layout.GetKind(new Cell(x, y)) == CellKind.Room ? 0 : 1;

                Assert.That(count[x, y], Is.EqualTo(expected), $"Zelle {x}, {y}");
            }
        }
    }
}
