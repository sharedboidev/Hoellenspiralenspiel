using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class AreaSettingsTests
{
    [Test]
    public void OhneAusbreitung_HatDieFlaecheSofortIhrenRadius()
        => Assert.That(new AreaSettings(300).GetRadiusAfter(0), Is.EqualTo(300f));

    [Test]
    public void MitAusbreitung_WaechstDerRadiusGleichmaessig()
    {
        var area = new AreaSettings(400, 0.2f);

        Assert.Multiple(() =>
        {
            Assert.That(area.GetRadiusAfter(0), Is.Zero);
            Assert.That(area.GetRadiusAfter(0.05), Is.EqualTo(100f).Within(0.01f));
            Assert.That(area.GetRadiusAfter(0.1), Is.EqualTo(200f).Within(0.01f));
            Assert.That(area.GetRadiusAfter(0.2), Is.EqualTo(400f));
            Assert.That(area.GetRadiusAfter(5), Is.EqualTo(400f));
        });
    }

    [Test]
    public void VorBeginn_HatDieFlaecheKeinenRadius()
        => Assert.That(new AreaSettings(300).GetRadiusAfter(-0.1), Is.Zero);

    [Test]
    public void Contains_IstEinKreisAufDemBoden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AreaSettings.Contains(100, 0, 100), Is.True, "rechts am Rand");
            Assert.That(AreaSettings.Contains(101, 0, 100), Is.False, "rechts knapp außerhalb");
            Assert.That(AreaSettings.Contains(0, 100, 100), Is.True, "vorn am Rand");
            Assert.That(AreaSettings.Contains(0, 101, 100), Is.False, "vorn knapp außerhalb");
            Assert.That(AreaSettings.Contains(0, -100, 100), Is.True, "hinten am Rand");
            Assert.That(AreaSettings.Contains(60, 80, 100), Is.True, "schräg am Rand");
            Assert.That(AreaSettings.Contains(80, 70, 100), Is.False, "schräg außerhalb");
        });
    }

    [Test]
    public void Contains_OhneRadius_TrifftNichts()
        => Assert.That(AreaSettings.Contains(0, 0, 0), Is.False);
}
