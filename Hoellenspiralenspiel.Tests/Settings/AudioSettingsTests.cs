using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Settings;

[TestFixture]
public class AudioSettingsTests
{
    private static readonly string[] KnownBuses = ["Master", "Music", "Effects"];

    [Test]
    public void OhneDateiIstAllesVollLaut()
    {
        var audio = new GameSettings().Audio;

        Assert.That(audio.Master, Is.EqualTo(1f));
        Assert.That(audio.Music, Is.EqualTo(1f));
        Assert.That(audio.Effects, Is.EqualTo(1f));
    }

    [Test]
    public void LautstaerkenUeberstehenSpeichernUndLaden()
    {
        var settings = new GameSettings { Audio = { Master = 0.8f, Music = 0.25f, Effects = 0f } };

        Assert.That(SettingsSerializer.TryDeserialize(SettingsSerializer.Serialize(settings), out var loaded), Is.True);
        Assert.That(loaded.Audio.Master, Is.EqualTo(0.8f));
        Assert.That(loaded.Audio.Music, Is.EqualTo(0.25f));
        Assert.That(loaded.Audio.Effects, Is.EqualTo(0f));
    }

    [Test]
    public void EineDateiOhneTonBehaeltDieAnzeige()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Display\": { \"Mode\": \"Windowed\" } }", out var settings), Is.True);
        Assert.That(settings.Display.Mode, Is.EqualTo(DisplayMode.Windowed));
        Assert.That(settings.Audio.Music, Is.EqualTo(1f));
    }

    [Test]
    public void VerbogeneLautstaerkenRueckenZwischenNullUndEins()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Audio\": { \"Master\": 3.5, \"Music\": -1, \"Effects\": 0.5 } }", out var settings), Is.True);
        Assert.That(settings.Audio.Master, Is.EqualTo(1f));
        Assert.That(settings.Audio.Music, Is.EqualTo(0f));
        Assert.That(settings.Audio.Effects, Is.EqualTo(0.5f));
    }

    [Test]
    public void EinLeererTonabschnittWirdErsetzt()
    {
        Assert.That(SettingsSerializer.TryDeserialize("{ \"Audio\": null }", out var settings), Is.True);
        Assert.That(settings.Audio.Effects, Is.EqualTo(1f));
    }

    [Test]
    public void EineKopieTeiltDenTonNicht()
    {
        var original = new GameSettings();
        var copy     = original.Copy();

        copy.Audio.Music = 0.1f;

        Assert.That(original.Audio.Music, Is.EqualTo(1f));
    }

    [TestCase(0f)]
    [TestCase(-0.5f)]
    [TestCase(float.NaN)]
    public void NullIstStumm(float share)
    {
        Assert.That(VolumeCurve.IsMuted(share), Is.True);
        Assert.That(VolumeCurve.ToDecibels(share), Is.EqualTo(float.NegativeInfinity));
    }

    [Test]
    public void VollIstNullDezibel()
        => Assert.That(VolumeCurve.ToDecibels(1f), Is.EqualTo(0f).Within(1e-5));

    [Test]
    public void DieMitteIstRundZwoelfDezibelLeiser()
        => Assert.That(VolumeCurve.ToDecibels(0.5f), Is.EqualTo(-12.04f).Within(0.01));

    [Test]
    public void MehrAlsVollBleibtVoll()
        => Assert.That(VolumeCurve.ToDecibels(1.7f), Is.EqualTo(0f).Within(1e-5));

    [Test]
    public void LauterGestelltIstNieLeiser()
    {
        var previous = float.NegativeInfinity;

        for (var share = 0.01f; share <= 1f; share += 0.01f)
        {
            var decibels = VolumeCurve.ToDecibels(share);

            Assert.That(decibels, Is.GreaterThan(previous));

            previous = decibels;
        }
    }

    //Ein Spieler ohne Bus spielt auf Master, und die Regler für Musik und Effekte wirken nicht auf ihn
    [Test]
    public void JederTonInEinerSzeneHatEinenBekanntenBus()
    {
        var root   = FindRepositoryRoot();
        var scenes = Directory.GetFiles(Path.Combine(root, "Scenes"), "*.tscn", SearchOption.AllDirectories);
        var found  = 0;

        foreach (var scene in scenes)
        {
            var lines = File.ReadAllLines(scene);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!Regex.IsMatch(lines[i], @"^\[node [^\]]*type=""AudioStreamPlayer(2D|3D)?"""))
                    continue;

                found++;

                var properties = lines.Skip(i + 1).TakeWhile(line => !line.StartsWith('[')).ToList();
                var bus        = properties.Select(line => Regex.Match(line, @"^bus = &""([^""]+)""")).FirstOrDefault(match => match.Success)?.Groups[1].Value;

                Assert.That(bus, Is.AnyOf(KnownBuses.Skip(1).ToArray()), $"{Path.GetRelativePath(root, scene)}: {lines[i]}");
            }
        }

        Assert.That(found, Is.GreaterThan(0));
    }

    [Test]
    public void DasBuslayoutKenntAlleBusse()
    {
        var layout = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "default_bus_layout.tres"));

        foreach (var bus in KnownBuses.Skip(1))
            Assert.That(layout, Does.Contain($"name = &\"{bus}\""));

        Assert.That(Regex.Matches(layout, @"volume_db = (?!0\.0\b)").Count, Is.Zero, "Grundpegel bleiben bei 0 dB, die Lautstärke kommt aus den Einstellungen");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Das Godot-Projekt liegt nicht über dem Testordner.");
    }
}
