using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class LevelGeneratorTests
{
    private const int SeedCount = 150;

    private static readonly LevelSettings Settings = new() { RoomCount = 10 };

    private static IEnumerable<LevelLayout> GenerateMany(LevelSettings settings = null, IReadOnlyList<RoomBlueprint> blueprints = null)
        => Enumerable.Range(1, SeedCount).Select(seed => LevelGenerator.Generate(blueprints ?? TestRooms.All, settings ?? Settings, seed * 977));

    private static HashSet<Cell> WalkFrom(LevelLayout layout, Cell origin)
    {
        var reached = new HashSet<Cell> { origin };
        var waiting = new Queue<Cell>();

        waiting.Enqueue(origin);

        while (waiting.Count > 0)
        {
            var cell = waiting.Dequeue();

            foreach (var side in SideExtensions.All)
            {
                if (layout.CanStep(cell, side) && reached.Add(cell.Step(side)))
                    waiting.Enqueue(cell.Step(side));
            }
        }

        return reached;
    }

    [Test]
    public void DerselbeSeed_ErgibtDieselbeEbene()
    {
        foreach (var seed in new[] { 1, 42, -7, 2026, int.MaxValue })
        {
            var first  = LevelGenerator.Generate(TestRooms.All, Settings, seed);
            var second = LevelGenerator.Generate(TestRooms.All, Settings, seed);

            Assert.That(TestRooms.Describe(second), Is.EqualTo(TestRooms.Describe(first)), $"Seed {seed}");
        }
    }

    [Test]
    public void VerschiedeneSeeds_ErgebenVerschiedeneEbenen()
    {
        var layouts = GenerateMany().Select(TestRooms.Describe).Distinct().Count();

        Assert.That(layouts, Is.EqualTo(SeedCount));
    }

    [Test]
    public void DieEbeneMerktSichIhrenSeed()
        => Assert.That(LevelGenerator.Generate(TestRooms.All, Settings, 4711).Seed, Is.EqualTo(4711));

    [Test]
    public void JederRaumIstVomStartAusErreichbar()
    {
        foreach (var layout in GenerateMany())
        {
            var reached = WalkFrom(layout, layout.GetCenterCell(layout.StartRoom));

            foreach (var room in layout.Rooms)
                Assert.That(reached, Does.Contain(layout.GetCenterCell(room.Index)), $"Seed {layout.Seed}, Raum {room.Index}");
        }
    }

    [Test]
    public void JederGangIstVomStartAusErreichbar()
    {
        foreach (var layout in GenerateMany())
        {
            var reached = WalkFrom(layout, layout.GetCenterCell(layout.StartRoom));

            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    if (layout.IsFloor(new Cell(x, y)))
                        Assert.That(reached, Does.Contain(new Cell(x, y)), $"Seed {layout.Seed}");
                }
            }
        }
    }

    [Test]
    public void DieZahlDerRaeumeStimmt()
    {
        foreach (var layout in GenerateMany())
            Assert.That(layout.Rooms, Has.Count.EqualTo(Settings.RoomCount), $"Seed {layout.Seed}");
    }

    [Test]
    public void StartUndAusgangKommenGenauEinmalVor()
    {
        foreach (var layout in GenerateMany())
        {
            Assert.That(layout.Rooms.Count(room => room.Blueprint.Role == RoomRole.Start), Is.EqualTo(1));
            Assert.That(layout.Rooms.Count(room => room.Blueprint.Role == RoomRole.Exit), Is.EqualTo(1));
            Assert.That(layout.Rooms[layout.StartRoom].Blueprint, Is.EqualTo(TestRooms.Start));
            Assert.That(layout.Rooms[layout.ExitRoom].Blueprint, Is.EqualTo(TestRooms.Exit));
        }
    }

    [Test]
    public void EinPflichtraumErscheintInJederEbeneGenauEinmal()
    {
        foreach (var layout in GenerateMany())
            Assert.That(layout.Rooms.Count(room => room.Blueprint == TestRooms.Shrine), Is.EqualTo(1), $"Seed {layout.Seed}");
    }

    [Test]
    public void PflichtraeumeErscheinenAuchInEinerKleinenEbene()
    {
        foreach (var layout in GenerateMany(Settings with { RoomCount = 2 }))
        {
            Assert.That(layout.Rooms, Has.Count.EqualTo(3), $"Seed {layout.Seed}");
            Assert.That(layout.Rooms.Select(room => room.Blueprint), Does.Contain(TestRooms.Shrine));
        }
    }

    [Test]
    public void RaeumeHaltenAbstand()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var room in layout.Rooms)
            {
                foreach (var other in layout.Rooms.Where(other => other.Index > room.Index))
                    Assert.That(room.Rect.IsCloserThan(other.Rect, Settings.MinGap), Is.False, $"Seed {layout.Seed}, Räume {room.Index} und {other.Index}");
            }
        }
    }

    [Test]
    public void UmDieEbeneBleibtEinRand()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var room in layout.Rooms)
            {
                Assert.That(room.Rect.X, Is.GreaterThanOrEqualTo(Settings.Margin));
                Assert.That(room.Rect.Y, Is.GreaterThanOrEqualTo(Settings.Margin));
                Assert.That(room.Rect.Right, Is.LessThanOrEqualTo(layout.Width - Settings.Margin));
                Assert.That(room.Rect.Bottom, Is.LessThanOrEqualTo(layout.Height - Settings.Margin));
            }
        }
    }

    [Test]
    public void GaengeFuehrenNieDurchRaeume()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var cell in layout.Connections.SelectMany(connection => connection.Path))
            {
                Assert.That(layout.GetKind(cell), Is.EqualTo(CellKind.Corridor), $"Seed {layout.Seed}, Zelle {cell}");
                Assert.That(layout.Rooms.Any(room => room.Rect.Contains(cell)), Is.False);
            }
        }
    }

    [Test]
    public void EinGangBeginntUndEndetVorEinerTuer()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var connection in layout.Connections)
            {
                Assert.That(connection.Path[0], Is.EqualTo(connection.FromDoor.Outside));
                Assert.That(connection.Path[^1], Is.EqualTo(connection.ToDoor.Outside));
                Assert.That(layout.CanStep(connection.FromDoor.Inside, connection.FromDoor.Side), Is.True);
                Assert.That(layout.CanStep(connection.ToDoor.Inside, connection.ToDoor.Side), Is.True);
            }
        }
    }

    [Test]
    public void EinGangHaengtInSichZusammen()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var connection in layout.Connections)
            {
                for (var i = 1; i < connection.Path.Count; i++)
                    Assert.That(connection.Path[i].StepsTo(connection.Path[i - 1]), Is.EqualTo(1), $"Seed {layout.Seed}");
            }
        }
    }

    [Test]
    public void DieMeistenEbenenHabenRundwege()
    {
        var withLoops = GenerateMany().Count(layout => layout.Connections.Count >= layout.Rooms.Count);

        Assert.That(withLoops, Is.GreaterThan(SeedCount * 0.9));
    }

    [Test]
    public void OhneAnteilFuerRundwege_BleibtEsBeimBaum()
    {
        foreach (var layout in GenerateMany(Settings with { LoopShare = 0f }))
            Assert.That(layout.Connections, Has.Count.EqualTo(layout.Rooms.Count - 1), $"Seed {layout.Seed}");
    }

    [Test]
    public void StartUndAusgangSindNichtDirektVerbunden()
    {
        foreach (var layout in GenerateMany())
        {
            foreach (var connection in layout.Connections)
            {
                var rooms = new[] { connection.FromRoom, connection.ToRoom };

                Assert.That(rooms.Contains(layout.StartRoom) && rooms.Contains(layout.ExitRoom), Is.False, $"Seed {layout.Seed}");
            }
        }
    }

    [Test]
    public void DerAusgangLiegtWeitVomStart()
    {
        var gaps = GenerateMany().Select(layout => layout.Rooms[layout.StartRoom].Rect.GapTo(layout.Rooms[layout.ExitRoom].Rect)).ToList();

        Assert.That(gaps.Average(), Is.GreaterThan(12));
        Assert.That(gaps.Min(), Is.GreaterThanOrEqualTo(Settings.MinGap));
    }

    [Test]
    public void DasFruehesteLevelSperrtVorlagen()
    {
        var deep       = TestRooms.Create("deep", 4, 3) with { MinAreaLevel = 5, Weight = 100f };
        var blueprints = TestRooms.All.Append(deep).ToList();

        foreach (var layout in GenerateMany(Settings with { AreaLevel = 4 }, blueprints))
            Assert.That(layout.Rooms.Select(room => room.Blueprint), Does.Not.Contain(deep));

        var atLevel = GenerateMany(Settings with { AreaLevel = 5 }, blueprints).Sum(layout => layout.Rooms.Count(room => room.Blueprint == deep));

        Assert.That(atLevel, Is.GreaterThan(SeedCount));
    }

    [Test]
    public void DieHoechstzahlProEbeneGilt()
    {
        var rare       = TestRooms.Create("rare", 3, 3) with { MaxPerLevel = 1, Weight = 100f };
        var blueprints = TestRooms.All.Append(rare).ToList();

        foreach (var layout in GenerateMany(blueprints: blueprints))
            Assert.That(layout.Rooms.Count(room => room.Blueprint == rare), Is.LessThanOrEqualTo(1), $"Seed {layout.Seed}");
    }

    [Test]
    public void HaeufigeVorlagenErscheinenOefter()
    {
        var counts = GenerateMany().SelectMany(layout => layout.Rooms).GroupBy(room => room.Blueprint.Id).ToDictionary(group => group.Key, group => group.Count());

        Assert.That(counts["hall"], Is.GreaterThan(counts["chamber"] * 1.5));
    }

    [Test]
    public void GesperrteVorlagenDrehenSichNicht()
    {
        var blueprints = TestRooms.All.Select(blueprint => blueprint with { CanRotate = false }).ToList();

        foreach (var layout in GenerateMany(blueprints: blueprints))
            Assert.That(layout.Rooms.Select(room => room.QuarterTurns), Is.All.EqualTo(0));
    }

    [Test]
    public void OhneVorlageFuerDenStart_GibtEsEinenFehler()
    {
        var blueprints = TestRooms.All.Where(blueprint => blueprint.Role != RoomRole.Start).ToList();

        Assert.That(() => LevelGenerator.Generate(blueprints, Settings, 1), Throws.TypeOf<LevelGenerationException>());
    }

    [Test]
    public void OhneVorlageFuerDenAusgang_GibtEsEinenFehler()
    {
        var blueprints = TestRooms.All.Where(blueprint => blueprint.Role != RoomRole.Exit).ToList();

        Assert.That(() => LevelGenerator.Generate(blueprints, Settings, 1), Throws.TypeOf<LevelGenerationException>());
    }

    [Test]
    public void RaeumeOhneTueren_ErgebenKeineEbene()
    {
        var blueprints = TestRooms.All.Select(blueprint => blueprint with { Doors = [] }).ToList();

        Assert.That(() => LevelGenerator.Generate(blueprints, Settings, 1), Throws.TypeOf<LevelGenerationException>());
    }

    [Test]
    public void OhneLueckeZwischenRaeumen_GibtEsEinenFehler()
        => Assert.That(() => LevelGenerator.Generate(TestRooms.All, Settings with { MinGap = 0 }, 1), Throws.TypeOf<ArgumentOutOfRangeException>());

    [Test]
    public void GruppenStehenImGangUndNichtVorTueren()
    {
        var packs = 0;

        foreach (var layout in GenerateMany())
        {
            var doorways = layout.Connections.SelectMany(connection => new[] { connection.FromDoor.Outside, connection.ToDoor.Outside }).ToList();

            foreach (var pack in layout.CorridorPacks)
            {
                Assert.That(layout.GetKind(pack), Is.EqualTo(CellKind.Corridor));
                Assert.That(doorways.Min(doorway => doorway.StepsTo(pack)), Is.GreaterThanOrEqualTo(2), $"Seed {layout.Seed}");
            }

            packs += layout.CorridorPacks.Count;
        }

        Assert.That(packs, Is.GreaterThan(SeedCount));
    }

    [Test]
    public void OhneDichte_BleibenDieGaengeLeer()
    {
        foreach (var layout in GenerateMany(Settings with { CellsPerCorridorPack = 0 }))
            Assert.That(layout.CorridorPacks, Is.Empty);
    }

    [Test]
    public void GrosseEbenenEntstehenAuch()
    {
        foreach (var seed in Enumerable.Range(1, 20))
        {
            var layout = LevelGenerator.Generate(TestRooms.All, Settings with { RoomCount = 30 }, seed);

            Assert.That(layout.Rooms, Has.Count.EqualTo(30));
        }
    }

    [Test, Explicit("Zeigt Ebenen als Text, zum Ansehen beim Einstellen des Generators")]
    public void ZeigeEbenen()
    {
        foreach (var seed in Enumerable.Range(1, 4))
            TestContext.Out.WriteLine(TestRooms.Describe(LevelGenerator.Generate(TestRooms.All, Settings, seed)));
    }
}
