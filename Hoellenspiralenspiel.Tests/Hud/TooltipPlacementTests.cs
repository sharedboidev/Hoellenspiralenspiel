using Hoellenspiralenspiel.Scripts.Core.Hud;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Hud;

[TestFixture]
public class TooltipPlacementTests
{
    private const float ScreenWidth  = 2560;
    private const float ScreenHeight = 1440;
    private const float Gap          = 8;

    private static readonly ScreenBox ItemInTheMiddle = new(1200, 700, 64, 192);

    [Test]
    public void Place_MitPlatzOben_StehtMittigUeberDemElement()
        => Assert.That(TooltipPlacement.Place(ItemInTheMiddle, 500, 300, ScreenWidth, ScreenHeight), Is.EqualTo(new ScreenBox(982, 400, 500, 300)));

    [Test]
    public void Place_OhnePlatzOben_StehtDarunter()
    {
        var item = new ScreenBox(1200, 100, 64, 64);

        Assert.That(TooltipPlacement.Place(item, 500, 300, ScreenWidth, ScreenHeight), Is.EqualTo(new ScreenBox(982, 164, 500, 300)));
    }

    [Test]
    public void Place_AmRand_BleibtImBild()
    {
        var left  = TooltipPlacement.Place(new ScreenBox(10, 700, 64, 64), 500, 300, ScreenWidth, ScreenHeight);
        var right = TooltipPlacement.Place(new ScreenBox(2500, 700, 64, 64), 500, 300, ScreenWidth, ScreenHeight);

        Assert.Multiple(() =>
        {
            Assert.That(left.X, Is.Zero);
            Assert.That(right.Right, Is.EqualTo(ScreenWidth));
        });
    }

    [Test]
    public void MitBegleiter_StehtDerBegleiterLinksUndSchliesstObenBuendigAb()
    {
        var (main, companion) = TooltipPlacement.PlaceWithCompanion(ItemInTheMiddle, 500, 300, 500, 360, ScreenWidth, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(main, Is.EqualTo(new ScreenBox(982, 340, 500, 300)));
            Assert.That(companion.Right, Is.EqualTo(main.X - Gap));
            Assert.That(companion.Y, Is.EqualTo(main.Y));
            Assert.That(companion.Bottom, Is.EqualTo(ItemInTheMiddle.Y));
        });
    }

    [Test]
    public void EinKleinererBegleiter_SchliesstObenBuendigAb()
    {
        var (main, companion) = TooltipPlacement.PlaceWithCompanion(ItemInTheMiddle, 500, 360, 500, 200, ScreenWidth, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(main.Y, Is.EqualTo(ItemInTheMiddle.Y - 360));
            Assert.That(companion.Y, Is.EqualTo(main.Y));
        });
    }

    [Test]
    public void UnterDemElement_SchliessenBeideObenBuendigAb()
    {
        var item = new ScreenBox(1200, 100, 64, 64);

        var (main, companion) = TooltipPlacement.PlaceWithCompanion(item, 500, 300, 500, 200, ScreenWidth, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(main.Y, Is.EqualTo(item.Bottom));
            Assert.That(companion.Y, Is.EqualTo(item.Bottom));
        });
    }

    [Test]
    public void AmUnterenRand_BleibenBeideImBildUndObenBuendig()
    {
        var item = new ScreenBox(1200, 100, 64, 64);

        var (main, companion) = TooltipPlacement.PlaceWithCompanion(item, 500, 300, 500, 600, ScreenWidth, 700, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(companion.Y, Is.EqualTo(main.Y));
            Assert.That(companion.Bottom, Is.LessThanOrEqualTo(700));
            Assert.That(main.Bottom, Is.LessThanOrEqualTo(700));
        });
    }

    [Test]
    public void EinHoehererBegleiter_ZiehtBeideUnterDasElement()
    {
        var item = new ScreenBox(1200, 336, 64, 64);

        var (main, companion) = TooltipPlacement.PlaceWithCompanion(item, 500, 270, 500, 500, ScreenWidth, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(main.Y, Is.EqualTo(item.Bottom));
            Assert.That(companion.Y, Is.EqualTo(item.Bottom));
            Assert.That(companion.Right, Is.LessThanOrEqualTo(main.X));
        });
    }

    [Test]
    public void OhnePlatzLinks_RueckenBeideNachRechts()
    {
        var item = new ScreenBox(40, 700, 64, 64);

        var (main, companion) = TooltipPlacement.PlaceWithCompanion(item, 500, 300, 500, 300, ScreenWidth, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(companion.X, Is.Zero);
            Assert.That(main.X, Is.EqualTo(500 + Gap));
            Assert.That(main.Y, Is.EqualTo(400));
            Assert.That(companion.Bottom, Is.EqualTo(item.Y));
        });
    }

    [Test]
    public void OhnePlatzLinks_UndSchmalesBild_BleibtDerTooltipImBild()
    {
        var (main, companion) = TooltipPlacement.PlaceWithCompanion(new ScreenBox(40, 700, 64, 64), 500, 300, 500, 300, 800, ScreenHeight, Gap);

        Assert.Multiple(() =>
        {
            Assert.That(companion.X, Is.Zero);
            Assert.That(main.Right, Is.EqualTo(800));
        });
    }

    [Test]
    public void KeinerDerBeiden_UeberdecktDasElement()
    {
        for (var y = 0f; y <= ScreenHeight - 64; y += 32)
        {
            for (var x = 0f; x <= ScreenWidth - 64; x += 128)
            {
                var item = new ScreenBox(x, y, 64, 64);

                var (main, companion) = TooltipPlacement.PlaceWithCompanion(item, 500, 270, 500, 480, ScreenWidth, ScreenHeight, Gap);

                Assert.That(Overlaps(main, item) || Overlaps(companion, item), Is.False, $"Element bei {x}/{y}");
            }
        }
    }

    private static bool Overlaps(ScreenBox a, ScreenBox b)
        => a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
}
