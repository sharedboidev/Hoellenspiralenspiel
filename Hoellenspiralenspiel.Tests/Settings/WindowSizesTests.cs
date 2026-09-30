using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Settings;

[TestFixture]
public class WindowSizesTests
{
    [Test]
    public void AngebotenWirdNurWasAufDenBildschirmPasst()
    {
        var usable  = new PixelSize(1920, 1040);
        var offered = WindowSizes.Offer(usable);

        Assert.That(offered, Is.Not.Empty);
        Assert.That(offered.All(size => size.FitsInto(usable)), Is.True);
        Assert.That(offered, Does.Contain(new PixelSize(1600, 900)));
        Assert.That(offered, Does.Not.Contain(new PixelSize(1920, 1080)));
    }

    [Test]
    public void AufEinemGrossenBildschirmGibtEsAuch16Zu10()
    {
        var offered = WindowSizes.Offer(new PixelSize(3200, 1952));

        Assert.That(offered, Does.Contain(new PixelSize(2560, 1600)));
        Assert.That(offered, Does.Contain(new PixelSize(3200, 1800)));
        Assert.That(offered, Does.Not.Contain(new PixelSize(3840, 2160)));
    }

    [Test]
    public void DieGroessenSteigen()
    {
        var offered = WindowSizes.Offer(new PixelSize(3840, 2160));

        for (var i = 1; i < offered.Count; i++)
            Assert.That(offered[i].Width * offered[i].Height, Is.GreaterThanOrEqualTo(offered[i - 1].Width * offered[i - 1].Height));
    }

    [Test]
    public void PasstNichtsBleibtDieFlaecheSelbst()
        => Assert.That(WindowSizes.Offer(new PixelSize(1024, 600)), Is.EqualTo(new[] { new PixelSize(1024, 600) }));

    [TestCase(1600, 900, 1920, 1040, 1600, 900)]
    [TestCase(2560, 1440, 1920, 1040, 1920, 1040)]
    [TestCase(100, 100, 1920, 1040, 640, 360)]
    [TestCase(1600, 900, 500, 300, 640, 360)]
    public void EineGroesseWirdAufDieFlaecheBegrenzt(int width, int height, int usableWidth, int usableHeight, int expectedWidth, int expectedHeight)
        => Assert.That(WindowSizes.Fit(new PixelSize(width, height), new PixelSize(usableWidth, usableHeight)), Is.EqualTo(new PixelSize(expectedWidth, expectedHeight)));
}
