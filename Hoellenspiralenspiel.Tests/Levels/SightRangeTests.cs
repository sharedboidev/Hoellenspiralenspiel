using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class SightRangeTests
{
    private const float Tolerance = 0.0001f;

    [Test]
    public void DieSichtReichtAufDasVielfacheDesLichtradius()
    {
        var sight = SightRange.From(8f, 1.2f, 1.5f);

        Assert.That(sight.RadiusMeters, Is.EqualTo(9.6f).Within(Tolerance));
        Assert.That(sight.EdgeMeters, Is.EqualTo(1.5f).Within(Tolerance));
        Assert.That(sight.IsLimited, Is.True);
    }

    [Test]
    public void DieSichtWaechstMitDemLichtradius()
    {
        var sight = SightRange.From(12f, 1.2f, 1.5f);

        Assert.That(sight.RadiusMeters, Is.EqualTo(14.4f).Within(Tolerance));
    }

    [TestCase(0f, 1f)]
    [TestCase(5f, 1f)]
    [TestCase(8.1f, 1f)]
    [TestCase(8.85f, 0.5f)]
    [TestCase(9.6f, 0f)]
    [TestCase(30f, 0f)]
    public void GegnerBlendenAmRandEin(float distance, float expected)
        => Assert.That(SightRange.From(8f, 1.2f, 1.5f).GetVisibility(distance), Is.EqualTo(expected).Within(Tolerance));

    [Test]
    public void WerNaeherKommtIstNieSchlechterZuSehen()
    {
        var sight    = SightRange.From(8f, 1.2f, 1.5f);
        var previous = 0f;

        for (var distance = 12f; distance >= 0f; distance -= 0.05f)
        {
            var visibility = sight.GetVisibility(distance);

            Assert.That(visibility, Is.GreaterThanOrEqualTo(previous));

            previous = visibility;
        }

        Assert.That(previous, Is.EqualTo(1f));
    }

    [TestCase(9.5f, 0f, true)]
    [TestCase(9.6f, 0f, false)]
    [TestCase(10f, 0.5f, true)]
    [TestCase(10.1f, 0.5f, false)]
    public void DerRandDesKoerpersZaehlt(float distance, float bodyRadius, bool expected)
        => Assert.That(SightRange.From(8f, 1.2f, 1.5f).Reaches(distance, bodyRadius), Is.EqualTo(expected));

    [TestCase(8f, 0f)]
    [TestCase(0f, 1.2f)]
    [TestCase(8f, -1f)]
    public void OhneFaktorOderLichtGibtEsKeineGrenze(float lightRadius, float factor)
    {
        var sight = SightRange.From(lightRadius, factor, 1.5f);

        Assert.That(sight, Is.EqualTo(SightRange.Unlimited));
        Assert.That(sight.IsLimited, Is.False);
        Assert.That(sight.GetVisibility(500f), Is.EqualTo(1f));
        Assert.That(sight.Reaches(500f), Is.True);
    }

    [Test]
    public void OhneRandIstDieGrenzeHart()
    {
        var sight = SightRange.From(8f, 1.2f, 0f);

        Assert.That(sight.GetVisibility(9.59f), Is.EqualTo(1f));
        Assert.That(sight.GetVisibility(9.6f), Is.EqualTo(0f));
    }

    [Test]
    public void DerRandIstNieBreiterAlsDieSicht()
    {
        var sight = SightRange.From(2f, 1f, 5f);

        Assert.That(sight.EdgeMeters, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(sight.GetVisibility(0f), Is.EqualTo(1f));
    }

    [Test]
    public void EinNegativerRandZaehltAlsKeiner()
        => Assert.That(SightRange.From(8f, 1.2f, -3f).EdgeMeters, Is.EqualTo(0f));
}
