using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class MarkPlacerTests
{
    private const float Cell = 4f;

    //Drei Zellen Mauer zwischen Raum 0 im Norden und einem Gang, dazu drei Zellen zwischen Fels im Westen und Raum 0
    private static readonly WallRun RoomToCorridor = new(true, 3, 2, 5, 0, LevelLayout.Corridor);
    private static readonly WallRun RockToRoom     = new(false, 2, 0, 3, LevelLayout.Rock, 0);

    private static MarkRule Hand(float chance = 1f, int max = 0)
        => new()
        {
            Id              = "hand",
            Place           = MarkPlace.Wall,
            WidthMeters     = 0.5f,
            HeightMeters    = 0.5f,
            Chance          = chance,
            MaxPerLevel     = max,
            MinCenterHeight = 1f,
            MaxCenterHeight = 1.5f
        };

    private static MarkRule Splatter(float chance = 1f, int max = 0, string near = "hand")
        => Hand(chance, max) with { Id = "splatter", Near = near };

    private static MarkRule Pentagram(float chance = 1f, int max = 1)
        => new()
        {
            Id           = "pentagram",
            Place        = MarkPlace.Floor,
            WidthMeters  = 3f,
            HeightMeters = 3f,
            Chance       = chance,
            MaxPerLevel  = max
        };

    private static MarkRule Pool(float chance = 1f, int max = 8)
        => Pentagram(chance, max) with { Id = "pool", WidthMeters = 2.2f, HeightMeters = 2.2f, InCorridors = true };

    [Test]
    public void OhneRegelnGibtEsKeineSpuren()
    {
        Assert.That(MarkPlacer.PlaceOnWalls([RoomToCorridor], [], Cell, new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnFloors(3, 0, [], new SeededRandom(1)), Is.Empty);
    }

    [Test]
    public void SichereTrefferBelegenJedeSeiteMitBodenUndKeineZumFels()
    {
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], [Hand()], Cell, new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(3 * 2 + 3));

        var corridorWall = marks.Where(mark => mark.Run == 0).ToList();
        var rockWall     = marks.Where(mark => mark.Run == 1).ToList();

        Assert.That(corridorWall.Select(mark => mark.Along), Is.EquivalentTo(new[] { 2, 2, 3, 3, 4, 4 }));
        Assert.That(corridorWall.Count(mark => mark.IsBefore), Is.EqualTo(3));
        Assert.That(rockWall.Select(mark => mark.Along), Is.EquivalentTo(new[] { 0, 1, 2 }));
        Assert.That(rockWall.All(mark => !mark.IsBefore), Is.True);
    }

    [Test]
    public void EineSpurBleibtInIhrerZelleUndInIhrerHoehe()
    {
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor], [Hand()], Cell, new SeededRandom(7));

        Assert.That(marks.Select(mark => mark.AlongMeters), Is.All.InRange(0.25f, 3.75f));
        Assert.That(marks.Select(mark => mark.CenterHeightMeters), Is.All.InRange(1f, 1.5f));
    }

    [Test]
    public void EineBreiteSpurSitztInDerMitteIhrerZelle()
    {
        var wide  = Hand() with { WidthMeters = 6f };
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor], [wide], Cell, new SeededRandom(7));

        Assert.That(marks.Select(mark => mark.AlongMeters), Is.All.EqualTo(Cell / 2f));
    }

    [Test]
    public void DieHoechstzahlJeEbeneGilt()
    {
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], [Hand(max: 2)], Cell, new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(2));
    }

    [Test]
    public void DieErsteTreffendeRegelBekommtDieStelle()
    {
        var stain = Hand() with { Id = "stain" };
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor], [Hand(), stain], Cell, new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(6));
        Assert.That(marks.All(mark => mark.Rule == 0), Is.True);
    }

    [Test]
    public void OhneChanceBleibtDieMauerLeer()
    {
        Assert.That(MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], [Hand(0f)], Cell, new SeededRandom(1)), Is.Empty);
    }

    [Test]
    public void DerselbeSeedErgibtDieselbenSpuren()
    {
        var rules  = new[] { Hand(0.3f), Hand(0.2f) with { Id = "stain" } };
        var first  = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], rules, Cell, new SeededRandom(42));
        var second = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], rules, Cell, new SeededRandom(42));
        var other  = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], rules, Cell, new SeededRandom(43));

        Assert.That(second, Is.EqualTo(first));
        Assert.That(other, Is.Not.EqualTo(first));
    }

    [Test]
    public void EinBegleiterErscheintNieAllein()
    {
        Assert.That(MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], [Splatter()], Cell, new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnWalls([RoomToCorridor], [Hand(), Splatter(near: "stain")], Cell, new SeededRandom(1)).All(mark => mark.Rule == 0), Is.True);
    }

    [Test]
    public void EinBegleiterLiegtNebenSeinerSpurAufDerselbenSeite()
    {
        //Eine Hand mit sicherem Treffer nur in der mittleren Zelle, der Begleiter trifft sicher
        var hand  = Hand(max: 1);
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor], [hand, Splatter()], Cell, new SeededRandom(3));
        var hands = marks.Where(mark => mark.Rule == 0).ToList();
        var dots  = marks.Where(mark => mark.Rule == 1).ToList();

        Assert.That(hands, Has.Count.EqualTo(1));
        Assert.That(dots, Has.Count.EqualTo(hands[0].Along == 3 ? 3 : 2));
        Assert.That(dots.Select(dot => dot.Run), Is.All.EqualTo(hands[0].Run));
        Assert.That(dots.Select(dot => dot.IsBefore), Is.All.EqualTo(hands[0].IsBefore));
        Assert.That(dots.Select(dot => System.Math.Abs(dot.Along - hands[0].Along)), Is.All.LessThanOrEqualTo(1));
        Assert.That(dots.Select(dot => dot.Along), Is.All.InRange(2, 4));
    }

    [Test]
    public void AuchFuerBegleiterGiltDieHoechstzahl()
    {
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor, RockToRoom], [Hand(), Splatter(max: 2)], Cell, new SeededRandom(1));

        Assert.That(marks.Count(mark => mark.Rule == 1), Is.EqualTo(2));
    }

    [Test]
    public void EinBegleiterOhneChanceBleibtAus()
    {
        var marks = MarkPlacer.PlaceOnWalls([RoomToCorridor], [Hand(), Splatter(0f)], Cell, new SeededRandom(1));

        Assert.That(marks.All(mark => mark.Rule == 0), Is.True);
    }

    [Test]
    public void AmBodenNimmtJederPlatzHoechstensEineSpur()
    {
        var marks = MarkPlacer.PlaceOnFloors(1, 0, [Pentagram(max: 3)], new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(1));
        Assert.That(marks[0].Spot, Is.EqualTo(0));
        Assert.That(marks[0].TurnRadians, Is.InRange(0f, 6.2832f));
    }

    [Test]
    public void AmBodenGiltDieHoechstzahlJeEbene()
    {
        var marks = MarkPlacer.PlaceOnFloors(5, 0, [Pentagram(max: 2)], new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(2));
        Assert.That(marks.Select(mark => mark.Spot).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void OhneChanceOderPlaetzeBleibtDerBodenLeer()
    {
        Assert.That(MarkPlacer.PlaceOnFloors(4, 0, [Pentagram(0f)], new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnFloors(0, 0, [Pentagram()], new SeededRandom(1)), Is.Empty);
    }

    [Test]
    public void OhneGangerlaubnisBleibenDieGaengeFrei()
    {
        var marks = MarkPlacer.PlaceOnFloors(2, 10, [Pentagram(max: 5)], new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(2));
        Assert.That(marks.Select(mark => mark.Spot), Is.All.LessThan(2));
    }

    [Test]
    public void LachenLiegenAuchInGaengen()
    {
        var marks = MarkPlacer.PlaceOnFloors(2, 10, [Pool()], new SeededRandom(1));

        Assert.That(marks, Has.Count.EqualTo(8));
        Assert.That(marks.Select(mark => mark.Spot).Distinct().Count(), Is.EqualTo(8));
        Assert.That(marks.Count(mark => mark.Spot >= 2), Is.GreaterThanOrEqualTo(6));
    }

    [Test]
    public void DasPentagrammBekommtSeinenPlatzVorDenLachen()
    {
        var marks     = MarkPlacer.PlaceOnFloors(1, 3, [Pentagram(), Pool()], new SeededRandom(1));
        var pentagram = marks.Where(mark => mark.Rule == 0).ToList();
        var pools     = marks.Where(mark => mark.Rule == 1).ToList();

        Assert.That(pentagram, Has.Count.EqualTo(1));
        Assert.That(pentagram[0].Spot, Is.Zero);
        Assert.That(pools, Has.Count.EqualTo(3));
        Assert.That(pools.Select(mark => mark.Spot), Is.All.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void NurGaengeOhneRaumplaetzeReichenFuerLachen()
    {
        Assert.That(MarkPlacer.PlaceOnFloors(0, 4, [Pentagram()], new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnFloors(0, 4, [Pool()], new SeededRandom(1)), Has.Count.EqualTo(4));
    }

    [Test]
    public void RegelnWirkenNurAnIhremOrt()
    {
        Assert.That(MarkPlacer.PlaceOnFloors(4, 0, [Hand()], new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnFloors(4, 0, [Pentagram() with { Near = "hand" }], new SeededRandom(1)), Is.Empty);
        Assert.That(MarkPlacer.PlaceOnWalls([RoomToCorridor], [Pentagram()], Cell, new SeededRandom(1)), Is.Empty);
    }
}
