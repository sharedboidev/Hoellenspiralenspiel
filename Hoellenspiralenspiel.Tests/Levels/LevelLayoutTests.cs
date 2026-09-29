using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class LevelLayoutTests
{
    //  0123456789
    // 1 AAA   BBB
    // 2 AAA...BBB
    // 3 AAA   BBB
    private static LevelLayout CreateTwoRooms()
    {
        var left   = new PlacedRoom(0, TestRooms.Chamber, 1, 1, 0);
        var right  = new PlacedRoom(1, TestRooms.Chamber, 7, 1, 0);
        var layout = new LevelLayout(1, 11, 5, [left, right], 0, 1);

        for (var x = 4; x <= 6; x++)
            layout.DigCorridor(new Cell(x, 2));

        layout.Open(left.Doors.Single(door => door.Side == CellSide.East));
        layout.Open(right.Doors.Single(door => door.Side == CellSide.West));

        return layout;
    }

    [Test]
    public void ZellenKennenIhreArtUndIhrenRaum()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.GetKind(new Cell(0, 0)), Is.EqualTo(CellKind.Solid));
        Assert.That(layout.GetKind(new Cell(2, 2)), Is.EqualTo(CellKind.Room));
        Assert.That(layout.GetKind(new Cell(5, 2)), Is.EqualTo(CellKind.Corridor));
        Assert.That(layout.GetKind(new Cell(-1, 50)), Is.EqualTo(CellKind.Solid));
        Assert.That(layout.GetRoomIndex(new Cell(8, 3)), Is.EqualTo(1));
        Assert.That(layout.GetRoomIndex(new Cell(5, 2)), Is.EqualTo(-1));
    }

    [Test]
    public void ZumFelsStehtEineMauer()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.HasWall(new Cell(1, 1), CellSide.North), Is.True);
        Assert.That(layout.HasWall(new Cell(1, 1), CellSide.West), Is.True);
        Assert.That(layout.HasWall(new Cell(5, 2), CellSide.North), Is.True);
        Assert.That(layout.HasWall(new Cell(5, 2), CellSide.South), Is.True);
    }

    [Test]
    public void ImRaumUndImGangStehtKeineMauer()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.HasWall(new Cell(1, 1), CellSide.East), Is.False);
        Assert.That(layout.HasWall(new Cell(2, 2), CellSide.South), Is.False);
        Assert.That(layout.HasWall(new Cell(5, 2), CellSide.East), Is.False);
    }

    [Test]
    public void EineTuerOeffnetDieMauerVonBeidenSeiten()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.HasWall(new Cell(3, 2), CellSide.East), Is.False);
        Assert.That(layout.HasWall(new Cell(4, 2), CellSide.West), Is.False);
        Assert.That(layout.CanStep(new Cell(3, 2), CellSide.East), Is.True);
        Assert.That(layout.CanStep(new Cell(7, 2), CellSide.West), Is.True);
    }

    [Test]
    public void EinGangNebenEinemRaumBleibtOhneTuerGetrennt()
    {
        var layout = CreateTwoRooms();

        layout.DigCorridor(new Cell(4, 1));

        Assert.That(layout.HasWall(new Cell(3, 1), CellSide.East), Is.True);
        Assert.That(layout.HasWall(new Cell(4, 1), CellSide.West), Is.True);
        Assert.That(layout.CanStep(new Cell(4, 1), CellSide.West), Is.False);
        Assert.That(layout.CanStep(new Cell(4, 1), CellSide.South), Is.True);
    }

    [Test]
    public void FelsMeldetKeineMauern()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.HasWall(new Cell(0, 0), CellSide.East), Is.False);
        Assert.That(layout.CanStep(new Cell(0, 1), CellSide.East), Is.False);
    }

    [Test]
    public void DurchEinenRaumFuehrtKeinGang()
        => Assert.That(() => CreateTwoRooms().DigCorridor(new Cell(2, 2)), Throws.InvalidOperationException);

    [Test]
    public void MauernAufEinerLinieWerdenZuEinemStueck()
    {
        var runs = CreateTwoRooms().GetWallRuns();

        Assert.That(runs, Does.Contain(new WallRun(true, 1, 1, 4, LevelLayout.Rock, 0)));
        Assert.That(runs, Does.Contain(new WallRun(true, 4, 1, 4, 0, LevelLayout.Rock)));
        Assert.That(runs, Does.Contain(new WallRun(false, 1, 1, 4, LevelLayout.Rock, 0)));
        Assert.That(runs, Does.Contain(new WallRun(true, 2, 4, 7, LevelLayout.Rock, LevelLayout.Corridor)));
        Assert.That(runs, Does.Contain(new WallRun(true, 3, 4, 7, LevelLayout.Corridor, LevelLayout.Rock)));
        Assert.That(runs, Does.Contain(new WallRun(false, 10, 1, 4, 1, LevelLayout.Rock)));
    }

    [Test]
    public void EineTuerTeiltDieMauer()
    {
        var runs = CreateTwoRooms().GetWallRuns();

        Assert.That(runs, Does.Contain(new WallRun(false, 4, 1, 2, 0, LevelLayout.Rock)));
        Assert.That(runs, Does.Contain(new WallRun(false, 4, 3, 4, 0, LevelLayout.Rock)));
        Assert.That(runs.Where(run => !run.IsAlongX && run.Line == 4).Sum(run => run.Length), Is.EqualTo(2));
    }

    [Test]
    public void EinStueckEndetWoSichDieSeitenAendern()
    {
        var layout = CreateTwoRooms();

        layout.DigCorridor(new Cell(1, 4));

        var runs = layout.GetWallRuns();

        Assert.That(runs, Does.Contain(new WallRun(true, 4, 1, 2, 0, LevelLayout.Corridor)));
        Assert.That(runs, Does.Contain(new WallRun(true, 4, 2, 4, 0, LevelLayout.Rock)));
    }

    [Test]
    public void ZellenKennenIhrGebiet()
    {
        var layout = CreateTwoRooms();

        Assert.That(layout.GetRegion(new Cell(0, 0)), Is.EqualTo(LevelLayout.Rock));
        Assert.That(layout.GetRegion(new Cell(5, 2)), Is.EqualTo(LevelLayout.Corridor));
        Assert.That(layout.GetRegion(new Cell(2, 2)), Is.EqualTo(0));
        Assert.That(layout.GetRegion(new Cell(8, 1)), Is.EqualTo(1));
        Assert.That(layout.GetRegion(new Cell(-3, 40)), Is.EqualTo(LevelLayout.Rock));
    }

    [Test]
    public void JedesStueckKenntWasAufSeinenSeitenLiegt()
    {
        foreach (var seed in Enumerable.Range(1, 40))
        {
            var layout = LevelGenerator.Generate(TestRooms.All, new LevelSettings(), seed);

            foreach (var run in layout.GetWallRuns())
            {
                Assert.That(run.RegionBefore != LevelLayout.Rock || run.RegionAfter != LevelLayout.Rock, Is.True, $"Seed {seed}, {run}");
                Assert.That(run.RegionBefore, Is.Not.EqualTo(run.RegionAfter), $"Seed {seed}, {run}");

                for (var along = run.From; along < run.To; along++)
                {
                    var before = run.IsAlongX ? new Cell(along, run.Line - 1) : new Cell(run.Line - 1, along);
                    var after  = run.IsAlongX ? new Cell(along, run.Line) : new Cell(run.Line, along);

                    Assert.That(layout.GetRegion(before), Is.EqualTo(run.RegionBefore), $"Seed {seed}, {run}, Zelle {before}");
                    Assert.That(layout.GetRegion(after), Is.EqualTo(run.RegionAfter), $"Seed {seed}, {run}, Zelle {after}");
                }
            }
        }
    }

    [Test]
    public void JedeMauerkanteLiegtInGenauEinemStueck()
    {
        foreach (var seed in Enumerable.Range(1, 40))
        {
            var layout = LevelGenerator.Generate(TestRooms.All, new LevelSettings(), seed);
            var runs   = layout.GetWallRuns();
            var edges  = 0;

            for (var y = 0; y <= layout.Height; y++)
            {
                for (var x = 0; x <= layout.Width; x++)
                {
                    if (layout.HasWall(new Cell(x, y - 1), CellSide.South) || layout.HasWall(new Cell(x, y), CellSide.North))
                        edges++;

                    if (layout.HasWall(new Cell(x - 1, y), CellSide.East) || layout.HasWall(new Cell(x, y), CellSide.West))
                        edges++;
                }
            }

            Assert.That(runs.Sum(run => run.Length), Is.EqualTo(edges), $"Seed {seed}");
            Assert.That(runs.All(run => run.Length > 0), Is.True);
        }
    }

    [Test]
    public void GaengeZerfallenInRechtecke()
    {
        var layout = CreateTwoRooms();

        layout.DigCorridor(new Cell(5, 3));
        layout.DigCorridor(new Cell(5, 4));

        Assert.That(layout.GetCorridorRects(), Is.EqualTo(new[] { new CellRect(4, 2, 3, 1), new CellRect(5, 3, 1, 2) }));
    }

    [Test]
    public void DieRechteckeDeckenJedenGangGenauEinmal()
    {
        foreach (var seed in Enumerable.Range(1, 40))
        {
            var layout = LevelGenerator.Generate(TestRooms.All, new LevelSettings(), seed);
            var rects  = layout.GetCorridorRects();

            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var covering = rects.Count(rect => rect.Contains(new Cell(x, y)));

                    Assert.That(covering, Is.EqualTo(layout.GetKind(new Cell(x, y)) == CellKind.Corridor ? 1 : 0), $"Seed {seed}, Zelle {x},{y}");
                }
            }
        }
    }
}
