using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Spatial;

[TestFixture]
public class LabelStackerTests
{
    private const float Gap = 4f;

    private static LabelBox Box(float left, float top, float width = 100f, float height = 30f)
        => new(left, top, width, height);

    private static void AssertNoneTouch(IReadOnlyList<LabelBox> boxes)
    {
        for (var i = 0; i < boxes.Count; i++)
        {
            for (var k = i + 1; k < boxes.Count; k++)
                Assert.That(boxes[i].Touches(boxes[k], Gap), Is.False, $"Schild {i} berührt Schild {k}: {boxes[i]} und {boxes[k]}");
        }
    }

    [Test]
    public void FreierPlatz_BleibtWoErSeinSoll()
        => Assert.That(LabelStacker.Place(Box(0, 100), [Box(300, 100)], Gap), Is.EqualTo(Box(0, 100)));

    [Test]
    public void OhneAndereSchilder_BleibtWoErSeinSoll()
        => Assert.That(LabelStacker.Place(Box(0, 100), [], Gap), Is.EqualTo(Box(0, 100)));

    [Test]
    public void BelegterPlatz_WeichtNachObenAus()
        => Assert.That(LabelStacker.Place(Box(20, 110), [Box(0, 100)], Gap), Is.EqualTo(Box(20, 100 - Gap - 30)));

    [Test]
    public void WeichtNieZurSeiteAus()
    {
        var placed = LabelStacker.Place(Box(20, 110), [Box(0, 100), Box(50, 60), Box(-40, 30)], Gap);

        Assert.That(placed.Left, Is.EqualTo(20f));
    }

    [Test]
    public void SteigtUeberEinenGanzenStapel()
    {
        var stack  = new[] { Box(0, 100), Box(10, 66), Box(-10, 32) };
        var placed = LabelStacker.Place(Box(5, 105), stack, Gap);

        Assert.That(placed, Is.EqualTo(Box(5, 32 - Gap - 30)));
    }

    [Test]
    public void DieReihenfolgeDerAnderenSpieltKeineRolle()
    {
        var stack = new[] { Box(-10, 32), Box(0, 100), Box(10, 66) };

        Assert.That(LabelStacker.Place(Box(5, 105), stack, Gap), Is.EqualTo(LabelStacker.Place(Box(5, 105), stack.Reverse().ToArray(), Gap)));
    }

    [Test]
    public void NutztEineLueckeImStapel()
    {
        var stack  = new[] { Box(0, 100), Box(0, 0) };
        var placed = LabelStacker.Place(Box(0, 100), stack, Gap);

        Assert.That(placed, Is.EqualTo(Box(0, 66)));
    }

    [Test]
    public void HaeltDenAbstandEin()
    {
        var other  = Box(0, 100);
        var placed = LabelStacker.Place(Box(0, 100), [other], Gap);

        Assert.That(other.Top - placed.Bottom, Is.EqualTo(Gap).Within(0.001f));
    }

    [Test]
    public void NebeneinanderMitAbstand_GiltNichtAlsBeruehrt()
        => Assert.That(Box(0, 0).Touches(Box(104, 0), Gap), Is.False);

    [Test]
    public void NebeneinanderOhneAbstand_GiltAlsBeruehrt()
        => Assert.That(Box(0, 0).Touches(Box(102, 0), Gap), Is.True);

    [Test]
    public void SchilderMitVerschiedenenMassen_BeruehrenSichNie()
    {
        var random = new SeededRandom(17);
        var placed = new List<LabelBox>();

        for (var i = 0; i < 60; i++)
        {
            var wanted = new LabelBox(random.NextRange(0, 600), random.NextRange(0, 300), random.NextRange(60, 260), random.NextRange(24, 56));

            placed.Add(LabelStacker.Place(wanted, placed, Gap));
        }

        AssertNoneTouch(placed);
    }

    [Test]
    public void KrummeZahlen_EndenTrotzdem()
    {
        var random = new SeededRandom(23);
        var placed = new List<LabelBox>();

        for (var i = 0; i < 200; i++)
        {
            var wanted = new LabelBox(random.NextRange(0, 300) + 0.37f, random.NextRange(0, 100) + 0.61f, 117.3f, 29.7f);

            placed.Add(LabelStacker.Place(wanted, placed, 3.3f));
        }

        Assert.That(placed, Has.Count.EqualTo(200));
    }

    [Test]
    public void AlleAufEinmal_BeruehrenSichNie()
    {
        var random = new SeededRandom(5);
        var wanted = new List<LabelBox>();

        for (var i = 0; i < 40; i++)
            wanted.Add(new LabelBox(random.NextRange(0, 400), random.NextRange(0, 200), random.NextRange(60, 200), 30f));

        AssertNoneTouch(LabelStacker.PlaceAll(wanted, Gap));
    }

    [Test]
    public void AlleAufEinmal_DasUntersteBleibtUnten()
    {
        var placed = LabelStacker.PlaceAll([Box(0, 90), Box(0, 100), Box(0, 95)], Gap);

        Assert.That(placed[1], Is.EqualTo(Box(0, 100)));
        Assert.That(placed[2], Is.EqualTo(Box(0, 66)));
        Assert.That(placed[0], Is.EqualTo(Box(0, 32)));
    }

    [Test]
    public void AlleAufEinmal_ErgebnisInDerReihenfolgeDerEingabe()
    {
        var wanted = new[] { Box(0, 10), Box(500, 300), Box(1000, 150) };

        Assert.That(LabelStacker.PlaceAll(wanted, Gap), Is.EqualTo(wanted));
    }

    [Test]
    public void AlleAufEinmal_OhneSchilderBleibtEsLeer()
        => Assert.That(LabelStacker.PlaceAll([], Gap), Is.Empty);
}
