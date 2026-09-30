using Hoellenspiralenspiel.Scripts.Core.Settings;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Settings;

[TestFixture]
public class SettingsSerializerTests
{
    [Test]
    public void OhneDateiGiltRandlosesVollbild()
    {
        var settings = new GameSettings();

        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Borderless));
        Assert.That(settings.Display.WindowSize, Is.EqualTo(new PixelSize(1600, 900)));
        Assert.That(settings.Version, Is.EqualTo(GameSettings.CurrentVersion));
    }

    [Test]
    public void EinstellungenUeberstehenSpeichernUndLaden()
    {
        var settings = new GameSettings { Display = { Mode = DisplayMode.Windowed, WindowWidth = 1920, WindowHeight = 1200 } };

        Assert.That(SettingsSerializer.TryDeserialize(SettingsSerializer.Serialize(settings), out var loaded), Is.True);
        Assert.That(loaded.Display.Mode, Is.EqualTo(DisplayMode.Windowed));
        Assert.That(loaded.Display.WindowSize, Is.EqualTo(new PixelSize(1920, 1200)));
    }

    [Test]
    public void DerModusStehtAlsNameInDerDatei()
        => Assert.That(SettingsSerializer.Serialize(new GameSettings { Display = { Mode = DisplayMode.Exclusive } }), Does.Contain("\"Exclusive\""));

    [Test]
    public void DieGroesseStehtNurEinmalInDerDatei()
    {
        var json = SettingsSerializer.Serialize(new GameSettings());

        Assert.That(json, Does.Contain("WindowWidth"));
        Assert.That(json, Does.Not.Contain("WindowSize"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("{ kaputt")]
    [TestCase("[1, 2, 3]")]
    public void EineUnlesbareDateiErgibtDieStandards(string json)
    {
        Assert.That(SettingsSerializer.TryDeserialize(json, out var settings), Is.False);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Borderless));
    }

    [Test]
    public void EinFehlenderAbschnittErgibtSeineStandards()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Version\": 1 }", out var settings), Is.True);
        Assert.That(settings.Display.WindowSize, Is.EqualTo(new PixelSize(1600, 900)));
    }

    [Test]
    public void EinLeererAbschnittWirdErsetzt()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Display\": null }", out var settings), Is.True);
        Assert.That(settings.Display, Is.Not.Null);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Borderless));
    }

    [Test]
    public void UnbekannteSchluesselStoerenNicht()
    {
        const string json = "{ \"Display\": { \"Mode\": \"Windowed\", \"Gamma\": 2.2 }, \"Audio\": { \"Master\": 0.5 } }";

        Assert.That(SettingsSerializer.TryDeserialize(json, out var settings), Is.True);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Windowed));
    }

    [TestCase("\"Fullscreen\"")]
    [TestCase("\"7\"")]
    [TestCase("2")]
    [TestCase("null")]
    public void EinUnbekannterModusFaelltAufRandlos(string mode)
    {
        Assert.That(SettingsSerializer.TryDeserialize($"{{ \"Display\": {{ \"Mode\": {mode}, \"WindowWidth\": 1280, \"WindowHeight\": 720 }} }}", out var settings), Is.True);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Borderless));
        Assert.That(settings.Display.WindowSize, Is.EqualTo(new PixelSize(1280, 720)));
    }

    [Test]
    public void DerModusIgnoriertGrossUndKleinschreibung()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Display\": { \"Mode\": \"windowed\" } }", out var settings), Is.True);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Windowed));
    }

    [TestCase(10, 10, 640, 360)]
    [TestCase(-5, 900, 640, 900)]
    [TestCase(99999, 99999, 7680, 4320)]
    public void VerbogeneGroessenRueckenInDenGueltigenBereich(int width, int height, int expectedWidth, int expectedHeight)
    {
        Assert.That(SettingsSerializer.TryDeserialize($"{{ \"Display\": {{ \"WindowWidth\": {width}, \"WindowHeight\": {height} }} }}", out var settings), Is.True);
        Assert.That(settings.Display.WindowSize, Is.EqualTo(new PixelSize(expectedWidth, expectedHeight)));
    }

    [Test]
    public void GleichesFensterErkenntSich()
    {
        var display = new DisplaySettings { Mode = DisplayMode.Windowed, WindowWidth = 1280, WindowHeight = 720 };

        Assert.That(display.SameWindowAs(display.Copy()), Is.True);
        Assert.That(display.SameWindowAs(new DisplaySettings { Mode = DisplayMode.Windowed, WindowWidth = 1280, WindowHeight = 721 }), Is.False);
        Assert.That(display.SameWindowAs(new DisplaySettings { Mode = DisplayMode.Exclusive, WindowWidth = 1280, WindowHeight = 720 }), Is.False);
        Assert.That(display.SameWindowAs(null), Is.False);
        Assert.That(display.SameWindowAs(new DisplaySettings { Mode = DisplayMode.Windowed, WindowWidth = 1280, WindowHeight = 720, VSync = false, MaxFps = 60 }), Is.True);
    }

    [Test]
    public void EineKopieIstUnabhaengig()
    {
        var original = new GameSettings { Display = { Mode = DisplayMode.Windowed, WindowWidth = 1280, WindowHeight = 720 } };
        var copy     = original.Copy();

        copy.Display.Mode        = DisplayMode.Exclusive;
        copy.Display.WindowWidth = 1920;

        Assert.That(original.Display.Mode, Is.EqualTo(DisplayMode.Windowed));
        Assert.That(original.Display.WindowWidth, Is.EqualTo(1280));
        Assert.That(copy.Display.WindowHeight, Is.EqualTo(720));
    }
}
