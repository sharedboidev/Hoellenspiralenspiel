using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Spatial;

[TestFixture]
public class SpatialHashTests
{
    private sealed class Thing(string name, float x, float y)
    {
        public string Name { get; } = name;
        public float  X    { get; set; } = x;
        public float  Y    { get; set; } = y;
    }

    private static List<Thing> Query(SpatialHash<Thing> hash, float x, float y, float radius)
    {
        var results = new List<Thing>();

        hash.Query(x, y, radius, results);

        return results;
    }

    [Test]
    public void LeereSuche_LiefertNichts()
    {
        var hash = new SpatialHash<Thing>(100f);

        Assert.That(Query(hash, 0f, 0f, 1000f), Is.Empty);
    }

    [Test]
    public void Suche_FindetWasInDerNaeheLiegt()
    {
        var hash = new SpatialHash<Thing>(100f);
        var near = new Thing("near", 50f, 50f);
        var far  = new Thing("far", 5000f, 5000f);

        hash.Place(near, near.X, near.Y);
        hash.Place(far, far.X, far.Y);

        Assert.That(Query(hash, 0f, 0f, 120f), Is.EqualTo(new[] { near }));
    }

    [Test]
    public void Suche_FindetAuchUeberZellgrenzenUndImNegativen()
    {
        var hash  = new SpatialHash<Thing>(100f);
        var left  = new Thing("left", -10f, -10f);
        var right = new Thing("right", 10f, 10f);

        hash.Place(left, left.X, left.Y);
        hash.Place(right, right.X, right.Y);

        Assert.That(Query(hash, 0f, 0f, 20f), Is.EquivalentTo(new[] { left, right }));
    }

    [Test]
    public void Verschieben_AendertDieZelle()
    {
        var hash  = new SpatialHash<Thing>(100f);
        var thing = new Thing("thing", 0f, 0f);

        hash.Place(thing, 0f, 0f);
        hash.Place(thing, 1000f, 1000f);

        Assert.Multiple(() =>
        {
            Assert.That(Query(hash, 0f, 0f, 50f), Is.Empty);
            Assert.That(Query(hash, 1000f, 1000f, 50f), Is.EqualTo(new[] { thing }));
            Assert.That(hash.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void Entfernen_NimmtEsAusDerSuche()
    {
        var hash  = new SpatialHash<Thing>(100f);
        var thing = new Thing("thing", 0f, 0f);

        hash.Place(thing, 0f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(hash.Remove(thing), Is.True);
            Assert.That(hash.Remove(thing), Is.False);
            Assert.That(Query(hash, 0f, 0f, 50f), Is.Empty);
            Assert.That(hash.Count, Is.Zero);
        });
    }

    [Test]
    public void Suche_LeertDieErgebnislisteVorher()
    {
        var hash    = new SpatialHash<Thing>(100f);
        var results = new List<Thing> { new("old", 0f, 0f) };

        hash.Query(0f, 0f, 50f, results);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Suche_LaesstNieEtwasAus_WasImKreisLiegt()
    {
        var random = new SeededRandom(5);
        var hash   = new SpatialHash<Thing>(256f);
        var things = new List<Thing>();

        for (var i = 0; i < 500; i++)
        {
            var thing = new Thing($"t{i}", random.NextRange(-3000f, 3000f), random.NextRange(-3000f, 3000f));

            things.Add(thing);
            hash.Place(thing, thing.X, thing.Y);
        }

        for (var move = 0; move < 200; move++)
        {
            var thing = things[random.NextInt(0, things.Count)];

            thing.X = random.NextRange(-3000f, 3000f);
            thing.Y = random.NextRange(-3000f, 3000f);

            hash.Place(thing, thing.X, thing.Y);
        }

        for (var query = 0; query < 100; query++)
        {
            var x      = random.NextRange(-3000f, 3000f);
            var y      = random.NextRange(-3000f, 3000f);
            var radius = random.NextRange(10f, 900f);

            var expected = things.Where(thing => MathF.Sqrt((thing.X - x) * (thing.X - x) + (thing.Y - y) * (thing.Y - y)) <= radius);
            var found    = Query(hash, x, y, radius);

            Assert.That(found, Is.SupersetOf(expected), $"Suche {query}");
            Assert.That(found.Distinct().Count(), Is.EqualTo(found.Count), "nichts doppelt");
        }
    }

    [Test]
    public void Suche_LiefertBeiKleinemRadiusNurEinenBruchteil()
    {
        var random = new SeededRandom(6);
        var hash   = new SpatialHash<Thing>(256f);

        for (var i = 0; i < 1000; i++)
        {
            var thing = new Thing($"t{i}", random.NextRange(-5000f, 5000f), random.NextRange(-5000f, 5000f));

            hash.Place(thing, thing.X, thing.Y);
        }

        Assert.That(Query(hash, 0f, 0f, 300f), Has.Count.LessThan(100));
    }

    [Test]
    public void ZellgroesseNull_WirdAbgelehnt()
        => Assert.That(() => new SpatialHash<Thing>(0f), Throws.InstanceOf<ArgumentOutOfRangeException>());
}
