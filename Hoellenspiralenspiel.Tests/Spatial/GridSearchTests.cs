using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Spatial;

[TestFixture]
public class GridSearchTests
{
    private const float Spacing = 80f;

    private static float DistanceBetween(Spot left, Spot right)
        => MathF.Sqrt((left.X - right.X) * (left.X - right.X) + (left.Y - right.Y) * (left.Y - right.Y));

    private static List<Spot> DropMany(Spot center, int count, float minDistance = 60f, float maxDistance = 800f, List<Spot> lying = null)
    {
        var dropped = new List<Spot>();

        lying ??= new List<Spot>();

        for (var i = 0; i < count; i++)
        {
            var wasFound = GridSearch.TryFind(center, Spacing, minDistance, maxDistance, spot => lying.TrueForAll(other => DistanceBetween(spot, other) >= 60f), out var found);

            Assert.That(wasFound, Is.True, $"Platz {i} fehlt");

            dropped.Add(found);
            lying.Add(found);
        }

        return dropped;
    }

    [Test]
    public void LiefertDenNaechstenGitterpunkt()
    {
        GridSearch.TryFind(new Spot(410f, 165f), Spacing, 0f, 800f, _ => true, out var found);

        Assert.That(found, Is.EqualTo(new Spot(400f, 160f)));
    }

    [Test]
    public void DasGitterHaengtAnDerWelt()
    {
        foreach (var center in new[] { new Spot(13f, -7f), new Spot(-951f, 333f), new Spot(40f, 40f) })
        {
            foreach (var spot in DropMany(center, 20))
            {
                Assert.That(spot.X / Spacing, Is.EqualTo(MathF.Round(spot.X / Spacing)).Within(0.0001f));
                Assert.That(spot.Y / Spacing, Is.EqualTo(MathF.Round(spot.Y / Spacing)).Within(0.0001f));
            }
        }
    }

    [Test]
    public void HaeltAbstandZurMitte()
    {
        var center = new Spot(5f, 5f);

        foreach (var spot in DropMany(center, 30, 120f))
            Assert.That(DistanceBetween(spot, center), Is.GreaterThanOrEqualTo(120f));
    }

    [Test]
    public void BleibtImKreis()
    {
        var center = new Spot(5f, 5f);

        foreach (var spot in DropMany(center, 30, 60f, 300f))
            Assert.That(DistanceBetween(spot, center), Is.LessThanOrEqualTo(300f));
    }

    [Test]
    public void FuelltDenKreisVonInnenNachAussen()
    {
        var center   = new Spot(5f, 5f);
        var dropped  = DropMany(center, 40);
        var previous = 0f;

        foreach (var spot in dropped)
        {
            Assert.That(DistanceBetween(spot, center), Is.GreaterThanOrEqualTo(previous - 0.001f));

            previous = DistanceBetween(spot, center);
        }
    }

    [Test]
    public void ZweiWuerfeLandenNieAufDemselbenPlatz()
    {
        var dropped = DropMany(new Spot(5f, 5f), 70);

        for (var i = 0; i < dropped.Count; i++)
        {
            for (var k = i + 1; k < dropped.Count; k++)
                Assert.That(DistanceBetween(dropped[i], dropped[k]), Is.GreaterThanOrEqualTo(Spacing - 0.001f));
        }
    }

    [Test]
    public void EinBeutelNebenDemGitterSperrtDiePunkteInSeinerNaehe()
    {
        var stranger = new Spot(120f, 35f);
        var dropped  = DropMany(new Spot(5f, 5f), 40, lying: [stranger]);

        foreach (var spot in dropped)
            Assert.That(DistanceBetween(spot, stranger), Is.GreaterThanOrEqualTo(60f));
    }

    [Test]
    public void WerSichBewegtLegtInDieselbeFlucht()
    {
        var lying  = new List<Spot>();
        var first  = DropMany(new Spot(5f, 5f), 10, lying: lying);
        var second = DropMany(new Spot(133f, -48f), 10, lying: lying);

        foreach (var spot in second)
        {
            Assert.That(first, Has.None.EqualTo(spot));
            Assert.That(spot.X / Spacing, Is.EqualTo(MathF.Round(spot.X / Spacing)).Within(0.0001f));
        }
    }

    [Test]
    public void BeiGleichemAbstandIstDieReihenfolgeFest()
    {
        var dropped = DropMany(new Spot(0f, 0f), 4);

        Assert.That(dropped, Is.EqualTo(new[] { new Spot(-80f, 0f), new Spot(0f, -80f), new Spot(0f, 80f), new Spot(80f, 0f) }));
    }

    [Test]
    public void IstNichtsFrei_ScheitertDieSucheUndLiefertDieMitte()
    {
        var center   = new Spot(5f, 5f);
        var wasFound = GridSearch.TryFind(center, Spacing, 60f, 300f, _ => false, out var found);

        Assert.That(wasFound, Is.False);
        Assert.That(found, Is.EqualTo(center));
    }

    [Test]
    public void PrueftNurSoLangeBisEinPlatzFreiIst()
    {
        var checks = 0;

        GridSearch.TryFind(new Spot(5f, 5f), Spacing, 60f, 800f, _ => ++checks == 3, out _);

        Assert.That(checks, Is.EqualTo(3));
    }

    [Test]
    public void OhneAbstandDerPunkte_WirftEineAusnahme()
        => Assert.Throws<ArgumentOutOfRangeException>(() => GridSearch.TryFind(new Spot(0f, 0f), 0f, 0f, 100f, _ => true, out _));
}
