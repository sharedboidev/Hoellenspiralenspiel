using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Spatial;

[TestFixture]
public class SpotSearchTests
{
    private static readonly Spot Center = new(1000f, -500f);

    private static float DistanceBetween(Spot left, Spot right)
        => MathF.Sqrt((left.X - right.X) * (left.X - right.X) + (left.Y - right.Y) * (left.Y - right.Y));

    private static List<Spot> PlaceGroup(int count, float radius, float minDistance, int seed)
    {
        var random = new SeededRandom(seed);
        var placed = new List<Spot>();

        for (var i = 0; i < count; i++)
        {
            var wasFound = SpotSearch.TryFind(Center, radius, random, spot => placed.TrueForAll(other => DistanceBetween(spot, other) >= minDistance), out var found);

            Assert.That(wasFound, Is.True, $"Platz {i} fehlt");

            placed.Add(found);
        }

        return placed;
    }

    [Test]
    public void OhneRadius_LiefertDieMitte()
    {
        var random   = new FixedRandom(0.5f);
        var wasFound = SpotSearch.TryFind(Center, 0f, random, _ => true, out var found);

        Assert.That(wasFound, Is.True);
        Assert.That(found, Is.EqualTo(Center));
        Assert.That(random.Draws, Is.Zero);
    }

    [Test]
    public void FreierKreis_LiefertEinenPunktImRadius()
    {
        var random = new SeededRandom(7);

        for (var i = 0; i < 200; i++)
        {
            SpotSearch.TryFind(Center, 300f, random, _ => true, out var found);

            Assert.That(DistanceBetween(found, Center), Is.LessThanOrEqualTo(300.01f));
        }
    }

    [Test]
    public void DiePunkteVerteilenSichUeberDieFlaeche()
    {
        var random = new SeededRandom(11);
        var inner  = 0;

        for (var i = 0; i < 4000; i++)
        {
            SpotSearch.TryFind(Center, 200f, random, _ => true, out var found);

            if (DistanceBetween(found, Center) <= 100f)
                inner++;
        }

        //Der innere Kreis mit halbem Radius hat ein Viertel der Fläche
        Assert.That(inner / 4000f, Is.EqualTo(0.25f).Within(0.03f));
    }

    [Test]
    public void BelegtePunkteWerdenUebergangen()
    {
        var random = new SeededRandom(3);

        for (var i = 0; i < 100; i++)
        {
            var wasFound = SpotSearch.TryFind(Center, 300f, random, spot => spot.X >= Center.X, out var found);

            Assert.That(wasFound, Is.True);
            Assert.That(found.X, Is.GreaterThanOrEqualTo(Center.X));
        }
    }

    [Test]
    public void IstDerKreisBelegt_WaechstEr()
    {
        var random   = new SeededRandom(5);
        var wasFound = SpotSearch.TryFind(Center, 50f, random, spot => DistanceBetween(spot, Center) > 120f, out var found);

        Assert.That(wasFound, Is.True);
        Assert.That(DistanceBetween(found, Center), Is.GreaterThan(120f));
    }

    [Test]
    public void IstDieMitteBelegt_SuchtEinKreisOhneRadiusImUmkreis()
    {
        var random   = new SeededRandom(5);
        var wasFound = SpotSearch.TryFind(Center, 0f, random, spot => DistanceBetween(spot, Center) > 30f, out var found);

        Assert.That(wasFound, Is.True);
        Assert.That(DistanceBetween(found, Center), Is.GreaterThan(30f));
    }

    [Test]
    public void IstNichtsFrei_ScheitertDieSucheUndLiefertDieMitte()
    {
        var wasFound = SpotSearch.TryFind(Center, 100f, new SeededRandom(1), _ => false, out var found);

        Assert.That(wasFound, Is.False);
        Assert.That(found, Is.EqualTo(Center));
    }

    [Test]
    public void DieZahlDerVersucheIstBegrenzt()
    {
        var checks   = 0;
        var settings = new SpotSearchSettings { TriesPerRound = 5, Rounds = 4 };

        SpotSearch.TryFind(Center, 100f, new SeededRandom(1), _ =>
        {
            checks++;

            return false;
        }, out _, settings);

        Assert.That(checks, Is.EqualTo(20));
    }

    [Test]
    public void EineGruppeHaeltIhrenMindestabstand()
    {
        var placed = PlaceGroup(16, 400f, 190f, 42);

        for (var i = 0; i < placed.Count; i++)
        {
            for (var k = i + 1; k < placed.Count; k++)
                Assert.That(DistanceBetween(placed[i], placed[k]), Is.GreaterThanOrEqualTo(190f));
        }
    }

    [Test]
    public void GleicherSeed_GleicheGruppe()
        => Assert.That(PlaceGroup(12, 400f, 150f, 99), Is.EqualTo(PlaceGroup(12, 400f, 150f, 99)));

    [Test]
    public void AndererSeed_AndereGruppe()
        => Assert.That(PlaceGroup(12, 400f, 150f, 99), Is.Not.EqualTo(PlaceGroup(12, 400f, 150f, 100)));
}
