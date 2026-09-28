using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class NearestPickerTests
{
    private static List<float> Pick(float[] candidates, float maxDistance, int count)
    {
        var result = new List<float>();

        NearestPicker.Pick(candidates, distance => distance * distance, maxDistance, count, result);

        return result;
    }

    [Test]
    public void WaehltDieNaechstenInAufsteigenderReihenfolge()
        => Assert.That(Pick([500, 100, 300, 200, 400], 1000, 2), Is.EqualTo(new[] { 100f, 200f }));

    [Test]
    public void LaesstKandidatenAusserhalbDerReichweiteAus()
        => Assert.That(Pick([700, 100, 650], 600, 3), Is.EqualTo(new[] { 100f }));

    [Test]
    public void DerRandDerReichweiteZaehltNochDazu()
        => Assert.That(Pick([600], 600, 1), Is.EqualTo(new[] { 600f }));

    [Test]
    public void LiefertWenigerWennEsNichtGenugGibt()
        => Assert.That(Pick([100], 1000, 3), Is.EqualTo(new[] { 100f }));

    [Test]
    public void OhneKandidaten_BleibtDasErgebnisLeer()
        => Assert.That(Pick([], 1000, 3), Is.Empty);

    [Test]
    public void OhneAnzahl_BleibtDasErgebnisLeer()
        => Assert.That(Pick([100, 200], 1000, 0), Is.Empty);

    [Test]
    public void NegativerAbstand_SchliesstDenKandidatenAus()
    {
        var result = new List<string>();

        NearestPicker.Pick(["getroffen", "frei"], name => name == "getroffen" ? -1f : 100f, 1000, 2, result);

        Assert.That(result, Is.EqualTo(new[] { "frei" }));
    }

    [Test]
    public void LeertDasErgebnisVorDemFuellen()
    {
        var result = new List<float> { 42f };

        NearestPicker.Pick([100f], distance => distance * distance, 1000, 1, result);

        Assert.That(result, Is.EqualTo(new[] { 100f }));
    }

    [Test]
    public void BeiGleichemAbstand_GewinntDerFruehereKandidat()
    {
        var result = new List<string>();

        NearestPicker.Pick(["erster", "zweiter", "dritter"], _ => 100f, 1000, 2, result);

        Assert.That(result, Is.EqualTo(new[] { "erster", "zweiter" }));
    }
}
