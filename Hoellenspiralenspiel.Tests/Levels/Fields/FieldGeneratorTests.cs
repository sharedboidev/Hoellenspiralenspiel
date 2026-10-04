using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

[TestFixture]
public class FieldGeneratorTests
{
    private const int SeedCount = 50;

    private static readonly FieldSettings Normal = new();

    private static readonly FieldSettings WithEvent = new() { EventRoomId = TestFields.RitualSite.Id };

    private static readonly FieldSettings Last = new() { IsLastField = true, EventRoomId = TestFields.EyeOfTheStorm.Id };

    private static readonly Lazy<List<(FieldLayout Field, FieldSettings Settings)>> Generated = new(() =>
    [
        ..Seeds().Select(seed => (FieldGenerator.Generate(TestFields.All, Normal, seed), Normal)),
        ..Seeds().Select(seed => (FieldGenerator.Generate(TestFields.All, WithEvent, seed), WithEvent)),
        ..Seeds().Select(seed => (FieldGenerator.Generate(TestFields.All, Last, seed), Last))
    ]);

    private static IEnumerable<int> Seeds()
        => Enumerable.Range(1, SeedCount).Select(seed => seed * 977);

    private static IEnumerable<FieldLayout> AllFields => Generated.Value.Select(entry => entry.Field);

    private static IEnumerable<FieldLayout> FieldsWith(FieldSettings settings)
        => Generated.Value.Where(entry => entry.Settings == settings).Select(entry => entry.Field);

    private static IEnumerable<Cell> CellsOf(FieldLayout field)
    {
        for (var y = 0; y < field.Height; y++)
        {
            for (var x = 0; x < field.Width; x++)
                yield return new Cell(x, y);
        }
    }

    private static IEnumerable<Cell> CellsOf(FieldLayout field, CellKind kind)
        => CellsOf(field).Where(cell => field.Grid.GetKind(cell) == kind);

    private static CellRect Inner(FieldLayout field)
        => new(field.Ground.X + 1, field.Ground.Y + 1, field.Ground.Width - 2, field.Ground.Height - 2);

    private static CellRect Spot(Cell cell)
        => new(cell.X, cell.Y, 1, 1);

    #region Seed

    [Test]
    public void DerselbeSeed_ErgibtDieselbeFlaeche()
    {
        foreach (var seed in new[] { 1, 42, -7, 2026, int.MaxValue })
        {
            var first  = FieldGenerator.Generate(TestFields.All, WithEvent, seed);
            var second = FieldGenerator.Generate(TestFields.All, WithEvent, seed);

            Assert.That(TestFields.Describe(second), Is.EqualTo(TestFields.Describe(first)), $"Seed {seed}");
        }
    }

    [Test]
    public void VerschiedeneSeeds_ErgebenVerschiedeneFlaechen()
        => Assert.That(FieldsWith(Normal).Select(TestFields.Draw).Distinct().Count(), Is.EqualTo(SeedCount));

    [Test]
    public void DieFlaecheMerktSichIhrenSeed()
    {
        var field = FieldGenerator.Generate(TestFields.All, Normal, 4711);

        Assert.Multiple(() =>
        {
            Assert.That(field.Seed, Is.EqualTo(4711));
            Assert.That(field.Grid.Seed, Is.EqualTo(4711));
        });
    }

    //Das Fertig-Kriterium der Etappe: 50 Seeds je Art von Fläche, keiner wirft
    [Test]
    public void FuenfzigSeedsJeArt_LiefernFlaechen_OhneAusnahme()
        => Assert.That(Generated.Value, Has.Count.EqualTo(SeedCount * 3));

    #endregion

    #region Rand, Tore, Wege

    [Test]
    public void DerSaumIstZweiZellenAbgrund_UndSonstLiegtNirgendsAbgrund()
    {
        foreach (var field in AllFields)
        {
            foreach (var cell in CellsOf(field))
                Assert.That(field.Grid.GetKind(cell) == CellKind.Void, Is.EqualTo(!field.Ground.Contains(cell)), $"Seed {field.Seed}, {cell}");

            Assert.That(field.Ground, Is.EqualTo(new CellRect(2, 2, 28, 20)));
        }
    }

    [Test]
    public void EingangUndAusgang_LiegenAufBodenAmRand_EinanderGegenueber()
    {
        foreach (var field in FieldsWith(Normal).Concat(FieldsWith(WithEvent)))
        {
            var exit = field.Exit ?? throw new AssertionException($"Seed {field.Seed} hat keinen Ausgang");

            foreach (var gate in new[] { field.Entrance, exit })
            {
                Assert.That(field.Grid.GetKind(gate.Cell), Is.EqualTo(CellKind.Ground), $"Seed {field.Seed}, {gate}");
                Assert.That(field.Grid.GetKind(gate.Cell.Step(gate.Side)), Is.EqualTo(CellKind.Void), $"Seed {field.Seed}, {gate} liegt nicht am Rand");
            }

            Assert.That(exit.Side, Is.EqualTo(field.Entrance.Side.Opposite()), $"Seed {field.Seed}");
        }
    }

    [Test]
    public void EingangUndAusgang_LiegenMindestens70ProzentDerBreiteAuseinander()
    {
        foreach (var field in FieldsWith(Normal).Concat(FieldsWith(WithEvent)))
        {
            var distance = MathF.Sqrt(PackScatter.DistanceSquared(field.Entrance.Cell, field.Exit!.Value.Cell));

            Assert.That(distance, Is.GreaterThanOrEqualTo(0.7f * field.Width), $"Seed {field.Seed}");
        }
    }

    [Test]
    public void DerAusgangIstVomEingangZuFussErreichbar()
    {
        foreach (var field in FieldsWith(Normal).Concat(FieldsWith(WithEvent)))
            Assert.That(field.Grid.FindReachable(field.Entrance.Cell), Does.Contain(field.Exit!.Value.Cell), $"Seed {field.Seed}");
    }

    [Test]
    public void JedeVorlageIstVomEingangZuFussErreichbar()
    {
        foreach (var field in AllFields)
        {
            var reached = field.Grid.FindReachable(field.Entrance.Cell);

            foreach (var placed in field.Placed)
                Assert.That(reached, Does.Contain(field.Grid.GetCenterCell(placed.Index)), $"Seed {field.Seed}, {placed.Blueprint.Id}");
        }
    }

    [Test]
    public void JederBodenIstVomEingangZuFussErreichbar()
    {
        foreach (var field in AllFields)
        {
            var reached = field.Grid.FindReachable(field.Entrance.Cell);

            Assert.That(CellsOf(field, CellKind.Ground).Where(cell => !reached.Contains(cell)), Is.Empty, $"Seed {field.Seed}");
        }
    }

    [Test]
    public void JedeTuerOeffnetSichAufBoden()
    {
        foreach (var field in AllFields)
        {
            foreach (var door in field.Placed.SelectMany(placed => placed.Doors))
            {
                Assert.That(field.Grid.GetKind(door.Outside), Is.EqualTo(CellKind.Ground), $"Seed {field.Seed}, {door}");
                Assert.That(field.Grid.CanStep(door.Inside, door.Side), Is.True, $"Seed {field.Seed}, {door}");
            }
        }
    }

    #endregion

    #region Vorlagen

    [Test]
    public void VorlagenUeberlappenNie_UndLassenZweiZellenPlatz()
    {
        foreach (var field in AllFields)
        {
            foreach (var first in field.Placed)
            {
                foreach (var second in field.Placed.Where(other => other.Index > first.Index))
                    Assert.That(first.Rect.IsCloserThan(second.Rect, FieldGenerator.TemplateGap), Is.False, $"Seed {field.Seed}, {first.Blueprint.Id} und {second.Blueprint.Id}");
            }
        }
    }

    [Test]
    public void VorlagenLassenEineZelleBodenZumAbgrund()
    {
        foreach (var field in AllFields)
        {
            var inner = Inner(field);

            foreach (var placed in field.Placed)
            {
                Assert.That(placed.Rect.X >= inner.X && placed.Rect.Y >= inner.Y && placed.Rect.Right <= inner.Right && placed.Rect.Bottom <= inner.Bottom,
                            Is.True,
                            $"Seed {field.Seed}, {placed.Blueprint.Id} bei {placed.Rect}");
            }
        }
    }

    [Test]
    public void VorlagenHaltenDreiZellenAbstandZuDenToren()
    {
        foreach (var field in AllFields)
        {
            var gates = field.Exit is { } exit ? new[] { field.Entrance, exit } : new[] { field.Entrance };

            foreach (var placed in field.Placed)
            {
                foreach (var gate in gates)
                    Assert.That(placed.Rect.IsCloserThan(Spot(gate.Cell), FieldGenerator.GateClearance), Is.False, $"Seed {field.Seed}, {placed.Blueprint.Id}");
            }
        }
    }

    [Test]
    public void JedeFlaecheHatGenauEinenDungeonEingang()
    {
        foreach (var field in AllFields)
            Assert.That(field.WithRole(RoomRole.Entrance).Select(placed => placed.Blueprint), Is.EqualTo(new[] { TestFields.Trapdoor }), $"Seed {field.Seed}");
    }

    [Test]
    public void DieZahlDerRuinenLiegtZwischenMinUndMax()
    {
        foreach (var field in AllFields)
            Assert.That(field.WithRole(RoomRole.Ruin).Count(), Is.InRange(Normal.MinRuins, Normal.MaxRuins), $"Seed {field.Seed}");
    }

    [Test]
    public void PflichtruinenStehenAufJederFlaeche()
    {
        var shrine     = TestFields.Ruin("shrine_ruin", 3, 3, new DoorSpot(CellSide.South, 1)) with { IsRequired = true, Weight = 0f };
        var blueprints = TestFields.All.Append(shrine).ToList();

        foreach (var seed in Seeds())
            Assert.That(FieldGenerator.Generate(blueprints, Normal, seed).Placed.Count(placed => placed.Blueprint == shrine), Is.EqualTo(1), $"Seed {seed}");
    }

    [Test]
    public void RuinenAchtenAufMindestlevelUndHoechstzahl()
    {
        var highTemple = TestFields.Ruin("high_temple", 3, 3, new DoorSpot(CellSide.South, 1)) with { MinAreaLevel = 6, Weight = 3f };
        var blueprints = TestFields.All.Append(highTemple).ToList();
        var low        = Seeds().Select(seed => FieldGenerator.Generate(blueprints, Normal with { AreaLevel = 5 }, seed)).ToList();
        var high       = Seeds().Select(seed => FieldGenerator.Generate(blueprints, Normal with { AreaLevel = 6 }, seed)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(low.SelectMany(field => field.Placed).Count(placed => placed.Blueprint == highTemple), Is.Zero, "Unter Bereichslevel 6 nie");
            Assert.That(high.SelectMany(field => field.Placed).Count(placed => placed.Blueprint == highTemple), Is.GreaterThan(0), "Ab Bereichslevel 6 schon");
            Assert.That(low.Concat(high).Max(field => field.Placed.Count(placed => placed.Blueprint == TestFields.Temple)), Is.LessThanOrEqualTo(1), "Höchstens ein Tempel je Fläche");
        });
    }

    #endregion

    #region Arena und Event

    [Test]
    public void AufFlaechenVorDerLetzten_StehtKeineArena()
    {
        foreach (var field in FieldsWith(Normal).Concat(FieldsWith(WithEvent)))
        {
            Assert.That(field.WithRole(RoomRole.Boss), Is.Empty, $"Seed {field.Seed}");
            Assert.That(field.Arena, Is.EqualTo(LevelLayout.NoRoom), $"Seed {field.Seed}");
        }
    }

    [Test]
    public void AufDerLetztenFlaeche_ErsetztDieArenaDenAusgang_AmFernenRand()
    {
        foreach (var field in FieldsWith(Last))
        {
            var arena = field.Placed[field.Arena].Rect;
            var inner = Inner(field);
            var gap = field.Entrance.Side switch
            {
                CellSide.West  => inner.Right - arena.Right,
                CellSide.East  => arena.X - inner.X,
                CellSide.North => inner.Bottom - arena.Bottom,
                _              => arena.Y - inner.Y
            };

            Assert.That(field.Exit, Is.Null, $"Seed {field.Seed}");
            Assert.That(field.Placed[field.Arena].Blueprint, Is.EqualTo(TestFields.JudgementArena), $"Seed {field.Seed}");
            Assert.That(field.WithRole(RoomRole.Boss).Count(), Is.EqualTo(1), $"Seed {field.Seed}");
            Assert.That(gap, Is.InRange(0, FieldGenerator.ArenaFarBand), $"Seed {field.Seed}: Die Arena steht nicht am fernen Rand");
        }
    }

    [Test]
    public void EineLetzteFlaecheOhneArenaVorlage_BehaeltIhrenAusgang()
    {
        var withoutArena = TestFields.All.Where(blueprint => blueprint.Role != RoomRole.Boss).ToList();

        foreach (var seed in Seeds().Take(10))
        {
            var field = FieldGenerator.Generate(withoutArena, Last, seed);

            Assert.That(field.Exit, Is.Not.Null, $"Seed {seed}");
            Assert.That(field.Arena, Is.EqualTo(LevelLayout.NoRoom), $"Seed {seed}");
        }
    }

    [Test]
    public void DasGeplanteEvent_ErscheintGenauEinmal()
    {
        Assert.Multiple(() =>
        {
            foreach (var field in FieldsWith(WithEvent))
                Assert.That(field.WithRole(RoomRole.Event).Select(placed => placed.Blueprint), Is.EqualTo(new[] { TestFields.RitualSite }), $"Seed {field.Seed}");

            foreach (var field in FieldsWith(Last))
                Assert.That(field.WithRole(RoomRole.Event).Select(placed => placed.Blueprint), Is.EqualTo(new[] { TestFields.EyeOfTheStorm }), $"Seed {field.Seed}");
        });
    }

    [Test]
    public void OhnePlan_ErscheintKeinEvent()
    {
        foreach (var field in FieldsWith(Normal))
            Assert.That(field.WithRole(RoomRole.Event), Is.Empty, $"Seed {field.Seed}");
    }

    [Test]
    public void EinGeplantesEventOhneVorlage_Wirft()
        => Assert.Throws<LevelGenerationException>(() => FieldGenerator.Generate(TestFields.All, Normal with { EventRoomId = "missing" }, 1));

    #endregion

    #region Hindernisse

    [Test]
    public void DerHindernisanteilLiegtInDerToleranz()
    {
        foreach (var field in AllFields)
        {
            var obstacles = CellsOf(field, CellKind.Obstacle).Count();
            var share     = obstacles / (float)(obstacles + CellsOf(field, CellKind.Ground).Count());

            Assert.That(share, Is.InRange(Normal.ObstacleShare - 0.02f, Normal.ObstacleShare + 0.005f), $"Seed {field.Seed}");
        }
    }

    [Test]
    public void HindernisgruppenHabenEinBisSechsZellen_UndBeruehrenSichNichtEinmalUeberEck()
    {
        foreach (var field in AllFields)
        {
            Assert.That(field.ObstacleGroups.SelectMany(group => group).Count(), Is.EqualTo(CellsOf(field, CellKind.Obstacle).Count()), $"Seed {field.Seed}");

            foreach (var group in field.ObstacleGroups)
            {
                Assert.That(group, Has.Count.InRange(ObstacleScatter.MinGroupSize, ObstacleScatter.MaxGroupSize), $"Seed {field.Seed}");

                foreach (var other in field.ObstacleGroups.Where(other => other != group))
                {
                    var touches = group.Any(cell => other.Any(neighbour => Math.Abs(cell.X - neighbour.X) <= 1 && Math.Abs(cell.Y - neighbour.Y) <= 1));

                    Assert.That(touches, Is.False, $"Seed {field.Seed}");
                }
            }
        }
    }

    [Test]
    public void VorTuerenUndAnDenTorenLiegtKeinHindernis()
    {
        foreach (var field in AllFields)
        {
            var gates = field.Exit is { } exit ? new[] { field.Entrance.Cell, exit.Cell } : new[] { field.Entrance.Cell };

            foreach (var cell in CellsOf(field, CellKind.Obstacle))
            {
                Assert.That(gates.All(gate => Math.Max(Math.Abs(gate.X - cell.X), Math.Abs(gate.Y - cell.Y)) > FieldGenerator.GateKeepFree), Is.True, $"Seed {field.Seed}, {cell}");
                Assert.That(field.Placed.SelectMany(placed => placed.Doors).Any(door => door.Outside == cell || door.Outside.Step(door.Side) == cell), Is.False, $"Seed {field.Seed}, {cell}");
            }
        }
    }

    [Test]
    public void OhneHindernisanteil_BleibtDerBodenFrei()
    {
        var field = FieldGenerator.Generate(TestFields.All, Normal with { ObstacleShare = 0f }, 3);

        Assert.Multiple(() =>
        {
            Assert.That(CellsOf(field, CellKind.Obstacle), Is.Empty);
            Assert.That(field.ObstacleGroups, Is.Empty);
        });
    }

    #endregion

    #region Gegnergruppen

    [Test]
    public void GruppenplaetzeLiegenAufBoden_FernVomEingang_NichtDichtAnVorlagen_NichtAmAbgrund()
    {
        foreach (var field in AllFields)
        {
            foreach (var spot in field.PackSpots)
            {
                Assert.That(field.Grid.GetKind(spot), Is.EqualTo(CellKind.Ground), $"Seed {field.Seed}, {spot}");
                Assert.That(MathF.Sqrt(PackScatter.DistanceSquared(spot, field.Entrance.Cell)), Is.GreaterThanOrEqualTo(4f), $"Seed {field.Seed}, {spot}");
                Assert.That(field.Placed.Any(placed => placed.Rect.IsCloserThan(Spot(spot), 1)), Is.False, $"Seed {field.Seed}, {spot} liegt direkt an einer Vorlage");
                Assert.That(SideExtensions.All.Any(side => field.Grid.GetKind(spot.Step(side)) == CellKind.Void), Is.False, $"Seed {field.Seed}, {spot} liegt am Abgrund");
            }
        }
    }

    [Test]
    public void DieZahlDerGruppenFolgtDemBoden()
    {
        foreach (var field in AllFields)
            Assert.That(field.PackSpots, Has.Count.EqualTo(CellsOf(field, CellKind.Ground).Count() / Normal.CellsPerFieldPack), $"Seed {field.Seed}");
    }

    [Test]
    public void GruppenplaetzeHaltenAbstandZueinander()
    {
        foreach (var field in AllFields)
        {
            foreach (var spot in field.PackSpots)
            {
                foreach (var other in field.PackSpots.Where(other => other != spot))
                    Assert.That(MathF.Sqrt(PackScatter.DistanceSquared(spot, other)), Is.GreaterThanOrEqualTo(PackScatter.MinSpacing), $"Seed {field.Seed}");
            }
        }
    }

    [Test]
    public void OhneGruppenanteil_BleibtDieFlaecheLeer()
        => Assert.That(FieldGenerator.Generate(TestFields.All, Normal with { CellsPerFieldPack = 0 }, 5).PackSpots, Is.Empty);

    #endregion

    #region Fehler

    [Test]
    public void EineVorlageOhneTuer_ScheitertNachAllenVersuchen()
    {
        var closed = TestFields.Trapdoor with { Id = "closed", Doors = [] };

        Assert.Throws<LevelGenerationException>(() => FieldGenerator.Generate([closed, TestFields.Courtyard], Normal with { MaxAttempts = 3 }, 1));
    }

    [Test]
    public void EineZuKleineFlaeche_Wirft()
        => Assert.Throws<ArgumentOutOfRangeException>(() => FieldGenerator.Generate(TestFields.All, Normal with { Width = 11 }, 1));

    [Test]
    public void MehrMindestruinenAlsHoechstruinen_Wirft()
        => Assert.Throws<ArgumentOutOfRangeException>(() => FieldGenerator.Generate(TestFields.All, Normal with { MinRuins = 6, MaxRuins = 5 }, 1));

    #endregion
}
