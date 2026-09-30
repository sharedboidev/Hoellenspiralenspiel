using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Settings;

[TestFixture]
public class LookAndInputSettingsTests
{
    private const long KeyA     = 65;
    private const long KeyB     = 66;
    private const long KeyQ     = 81;
    private const long KeyT     = 84;
    private const long KeySpace = 32;
    private const long KeyF3    = 4194334;
    private const long KeyF7    = 4194338;

    private static InputBinding Key(long code)
        => new(BindingKind.Key, code);

    private static InputBinding Mouse(long button)
        => new(BindingKind.Mouse, button);

    private static Dictionary<string, InputBinding> Defaults()
        => new()
        {
            ["move_left"]              = Key(KeyA),
            ["toggle_character_sheet"] = Key(KeyB),
            ["open_town_portal"]       = Key(KeyT),
            ["close_windows"]          = Key(KeySpace),
            ["skill_slot_1"]           = Mouse(1),
            ["skill_slot_2"]           = Mouse(2),
            ["skill_slot_3"]           = Key(KeyQ)
        };

    [Test]
    public void DerLookStartetWieBisher()
    {
        var settings = new GameSettings();

        Assert.That(settings.Look.Grain, Is.EqualTo(PixelGrain.Coarse));
        Assert.That(settings.Look.Dithering && settings.Look.VertexWobble && settings.Look.RealShadows, Is.True);
        Assert.That(settings.Look.Brightness, Is.EqualTo(1f));
        Assert.That(settings.Display.VSync, Is.True);
        Assert.That(settings.Display.MaxFps, Is.Zero);
        Assert.That(settings.Display.ShowFps, Is.False);
        Assert.That(settings.Input.Bindings, Is.Empty);
    }

    [Test]
    public void LookAnzeigeUndTastenUeberstehenSpeichernUndLaden()
    {
        var settings = new GameSettings
        {
            Display = { VSync = false, MaxFps = 144, ShowFps = true },
            Look    = { Grain = PixelGrain.Fine, Dithering = false, VertexWobble = false, RealShadows = false, Brightness = 1.25f },
            Input   = { Bindings = { ["skill_slot_3"] = Mouse(3), ["toggle_character_sheet"] = Key(73) } }
        };

        Assert.That(SettingsSerializer.TryDeserialize(SettingsSerializer.Serialize(settings), out var loaded), Is.True);
        Assert.That(loaded.Display.VSync, Is.False);
        Assert.That(loaded.Display.MaxFps, Is.EqualTo(144));
        Assert.That(loaded.Display.ShowFps, Is.True);
        Assert.That(loaded.Look.Grain, Is.EqualTo(PixelGrain.Fine));
        Assert.That(loaded.Look.Dithering || loaded.Look.VertexWobble || loaded.Look.RealShadows, Is.False);
        Assert.That(loaded.Look.Brightness, Is.EqualTo(1.25f));
        Assert.That(loaded.Input.Bindings["skill_slot_3"], Is.EqualTo(Mouse(3)));
        Assert.That(loaded.Input.Bindings["toggle_character_sheet"], Is.EqualTo(Key(73)));
    }

    [Test]
    public void VerbogeneWerteRueckenZurecht()
    {
        const string json = "{ \"Display\": { \"MaxFps\": -5 }, \"Look\": { \"Grain\": \"Ultra\", \"Brightness\": 9 }, "
                          + "\"Input\": { \"Bindings\": { \"move_left\": { \"Kind\": \"Key\", \"Code\": 0 }, \"\": { \"Kind\": \"Key\", \"Code\": 65 }, "
                          + "\"move_up\": { \"Kind\": \"Joystick\", \"Code\": 3 } } } }";

        Assert.That(SettingsSerializer.TryDeserialize(json, out var settings), Is.True);
        Assert.That(settings.Display.MaxFps, Is.Zero);
        Assert.That(settings.Look.Grain, Is.EqualTo(PixelGrain.Coarse));
        Assert.That(settings.Look.Brightness, Is.EqualTo(LookSettings.MaxBrightness));
        Assert.That(settings.Input.Bindings.ContainsKey("move_left"), Is.False);
        Assert.That(settings.Input.Bindings.ContainsKey(""), Is.False);
        Assert.That(settings.Input.Bindings["move_up"], Is.EqualTo(Key(3)), "Eine unbekannte Art fällt auf Taste");
    }

    [Test]
    public void EineKopieTeiltTastenUndLookNicht()
    {
        var original = new GameSettings { Input = { Bindings = { ["move_left"] = Key(KeyA) } } };
        var copy     = original.Copy();

        copy.Input.Bindings["move_left"] = Key(KeyB);
        copy.Look.Brightness             = 0.5f;

        Assert.That(original.Input.Bindings["move_left"], Is.EqualTo(Key(KeyA)));
        Assert.That(original.Look.Brightness, Is.EqualTo(1f));
    }

    [Test]
    public void GleicherLookErkenntSich()
    {
        var look = new LookSettings { Grain = PixelGrain.Medium, Brightness = 1.2f };

        Assert.That(look.SameAs(look.Copy()), Is.True);
        Assert.That(look.SameAs(new LookSettings { Grain = PixelGrain.Medium, Brightness = 1.3f }), Is.False);
        Assert.That(look.SameAs(new LookSettings { Grain = PixelGrain.Medium, Brightness = 1.2f, Dithering = false }), Is.False);
        Assert.That(look.SameAs(null), Is.False);
    }

    [TestCase(2000, PixelGrain.Coarse, 8)]
    [TestCase(2000, PixelGrain.Medium, 6)]
    [TestCase(2000, PixelGrain.Fine, 4)]
    [TestCase(1440, PixelGrain.Coarse, 6)]
    [TestCase(1080, PixelGrain.Coarse, 5)]
    [TestCase(1080, PixelGrain.Fine, 2)]
    [TestCase(720, PixelGrain.Coarse, 3)]
    [TestCase(720, PixelGrain.Medium, 2)]
    [TestCase(720, PixelGrain.Fine, 1)]
    [TestCase(1200, PixelGrain.Coarse, 5)]
    [TestCase(1200, PixelGrain.Medium, 3)]
    [TestCase(1200, PixelGrain.Fine, 2)]
    [TestCase(768, PixelGrain.Fine, 1)]
    [TestCase(64, PixelGrain.Coarse, 1)]
    [TestCase(64, PixelGrain.Fine, 1)]
    [TestCase(0, PixelGrain.Fine, 1)]
    public void EinPixelDerPs1DecktGanzeBildschirmpixel(int height, PixelGrain grain, int expected)
        => Assert.That(PixelGrid.CellSize(height, grain), Is.EqualTo(expected));

    [Test]
    public void AbDreiPixelnSindAlleStufenVerschieden()
    {
        for (var height = 600; height <= 4400; height += 3)
        {
            Assert.That(PixelGrid.CellSize(height, PixelGrain.Medium), Is.LessThan(PixelGrid.CellSize(height, PixelGrain.Coarse)), $"{height}");
            Assert.That(PixelGrid.CellSize(height, PixelGrain.Fine), Is.LessThan(PixelGrid.CellSize(height, PixelGrain.Medium)), $"{height}");
        }
    }

    [Test]
    public void FeinerHeisstNieGroesser()
    {
        for (var height = 200; height <= 4400; height += 7)
        {
            Assert.That(PixelGrid.CellSize(height, PixelGrain.Medium), Is.LessThanOrEqualTo(PixelGrid.CellSize(height, PixelGrain.Coarse)));
            Assert.That(PixelGrid.CellSize(height, PixelGrain.Fine), Is.LessThanOrEqualTo(PixelGrid.CellSize(height, PixelGrain.Medium)));
        }
    }

    [Test]
    public void EineFreieTasteWirdEinfachBelegt()
    {
        var bindings = Defaults();

        Assert.That(BindingRules.Assign(bindings, "open_town_portal", Key(KeyF7)).Outcome, Is.EqualTo(BindingOutcome.Bound));
        Assert.That(bindings["open_town_portal"], Is.EqualTo(Key(KeyF7)));
    }

    [Test]
    public void BeiKonfliktTauschenDieAktionen()
    {
        var bindings = Defaults();
        var result   = BindingRules.Assign(bindings, "open_town_portal", Key(KeyB));

        Assert.That(result.Outcome, Is.EqualTo(BindingOutcome.Swapped));
        Assert.That(result.OtherAction, Is.EqualTo("toggle_character_sheet"));
        Assert.That(bindings["open_town_portal"], Is.EqualTo(Key(KeyB)));
        Assert.That(bindings["toggle_character_sheet"], Is.EqualTo(Key(KeyT)));
    }

    [Test]
    public void SkillPlaetzeTauschenAuchMaustasten()
    {
        var bindings = Defaults();

        Assert.That(BindingRules.Assign(bindings, "skill_slot_3", Mouse(1)).Outcome, Is.EqualTo(BindingOutcome.Swapped));
        Assert.That(bindings["skill_slot_3"], Is.EqualTo(Mouse(1)));
        Assert.That(bindings["skill_slot_1"], Is.EqualTo(Key(KeyQ)));
    }

    [Test]
    public void MaustastenNurFuerSkillPlaetze()
    {
        var bindings = Defaults();

        Assert.That(BindingRules.Assign(bindings, "toggle_character_sheet", Mouse(3)).Outcome, Is.EqualTo(BindingOutcome.MouseOnlyForSkills));
        Assert.That(bindings["toggle_character_sheet"], Is.EqualTo(Key(KeyB)));
    }

    [Test]
    public void EinTauschDerEineMaustasteAnDieFalscheAktionGaebeWirdAbgelehnt()
    {
        var bindings = Defaults();
        var result   = BindingRules.Assign(bindings, "skill_slot_1", Key(KeyB));

        Assert.That(result.Outcome, Is.EqualTo(BindingOutcome.SwapImpossible));
        Assert.That(result.OtherAction, Is.EqualTo("toggle_character_sheet"));
        Assert.That(bindings["skill_slot_1"], Is.EqualTo(Mouse(1)));
        Assert.That(bindings["toggle_character_sheet"], Is.EqualTo(Key(KeyB)));
    }

    [TestCase(KeyF3)]
    [TestCase(BindingRules.Escape)]
    public void TastenZumTestenUndEscapeSindGesperrt(long code)
    {
        var bindings = Defaults();

        Assert.That(BindingRules.Assign(bindings, "skill_slot_3", Key(code)).Outcome, Is.EqualTo(BindingOutcome.Reserved));
        Assert.That(bindings["skill_slot_3"], Is.EqualTo(Key(KeyQ)));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void DasMausradIstGesperrt(long button)
        => Assert.That(BindingRules.Assign(Defaults(), "skill_slot_3", Mouse(button)).Outcome, Is.EqualTo(BindingOutcome.Reserved));

    [Test]
    public void DasPausenmenueLaesstSichNichtUmbelegen()
        => Assert.That(BindingRules.Check(BindingRules.PauseAction, Key(KeyF7)), Is.EqualTo(BindingOutcome.NotRebindable));

    [Test]
    public void DieselbeTasteNochmalAendertNichts()
        => Assert.That(BindingRules.Assign(Defaults(), "move_left", Key(KeyA)).Outcome, Is.EqualTo(BindingOutcome.Unchanged));

    [Test]
    public void GespeichertWirdNurDieAbweichung()
    {
        var defaults  = Defaults();
        var effective = Defaults();

        BindingRules.Assign(effective, "open_town_portal", Key(KeyB));

        var overrides = BindingRules.Overrides(defaults, effective);

        Assert.That(overrides.Keys, Is.EquivalentTo(new[] { "open_town_portal", "toggle_character_sheet" }));
        Assert.That(BindingRules.Resolve(defaults, overrides), Is.EquivalentTo(effective));
    }

    [Test]
    public void OhneAbweichungGiltDerStandard()
        => Assert.That(BindingRules.Resolve(Defaults(), new Dictionary<string, InputBinding>()), Is.EquivalentTo(Defaults()));

    [Test]
    public void UnbekannteUndUngueltigeAbweichungenFallenWeg()
    {
        var overrides = new Dictionary<string, InputBinding>
        {
            ["removed_action"]         = Key(KeyF7),
            ["toggle_character_sheet"] = Mouse(1),
            ["skill_slot_3"]           = Key(KeyF3)
        };

        Assert.That(BindingRules.Resolve(Defaults(), overrides), Is.EquivalentTo(Defaults()));
    }

    //Aus der Prüfung vom 30.09.2026: Beim Nachspielen als Tausch ging Skill 1 auf B verloren
    [Test]
    public void EineFreieStandardtasteBleibtNachDemLadenBeimSkill()
    {
        var defaults  = Defaults();
        var effective = Defaults();

        Assert.That(BindingRules.Assign(effective, "toggle_character_sheet", Key(75)).Outcome, Is.EqualTo(BindingOutcome.Bound));
        Assert.That(BindingRules.Assign(effective, "skill_slot_1", Key(KeyB)).Outcome, Is.EqualTo(BindingOutcome.Bound));

        Assert.That(BindingRules.Resolve(defaults, BindingRules.Overrides(defaults, effective)), Is.EquivalentTo(effective));
    }

    [Test]
    public void JedeFolgeVonBelegungenUebersteht()
    {
        var random   = new System.Random(4242);
        var actions  = Defaults().Keys.ToArray();
        InputBinding[] pool = [Key(KeyA), Key(KeyB), Key(KeyQ), Key(KeyT), Key(KeySpace), Key(73), Key(75), Key(77), Mouse(1), Mouse(2), Mouse(3), Mouse(8), Key(KeyF3)];

        for (var run = 0; run < 500; run++)
        {
            var defaults  = Defaults();
            var effective = Defaults();

            for (var step = 0; step < 15; step++)
            {
                BindingRules.Assign(effective, actions[random.Next(actions.Length)], pool[random.Next(pool.Length)]);

                var restored = BindingRules.Resolve(defaults, BindingRules.Overrides(defaults, effective));

                Assert.That(restored, Is.EquivalentTo(effective), $"Lauf {run}, Schritt {step}");
                Assert.That(effective.Values, Is.Unique);
            }
        }
    }

    [Test]
    public void ZweiAbweichungenAufDerselbenTasteGeltenNichtBeide()
    {
        var overrides = new Dictionary<string, InputBinding> { ["open_town_portal"] = Key(75), ["toggle_character_sheet"] = Key(75) };
        var effective = BindingRules.Resolve(Defaults(), overrides);

        Assert.That(effective["open_town_portal"], Is.EqualTo(Key(75)));
        Assert.That(effective["toggle_character_sheet"], Is.EqualTo(Key(KeyB)));
        Assert.That(effective.Values, Is.Unique);
    }

    [Test]
    public void EineNeueStandardtasteKollidiertNichtMitEinerAbweichung()
    {
        var defaults = Defaults();

        defaults["skill_slot_3"]     = Key(KeyT);
        defaults["open_town_portal"] = Key(KeyQ);

        var effective = BindingRules.Resolve(defaults, new Dictionary<string, InputBinding> { ["open_town_portal"] = Key(KeyT) });

        Assert.That(effective["open_town_portal"], Is.EqualTo(Key(KeyT)));
        Assert.That(effective["skill_slot_3"], Is.EqualTo(Key(KeyQ)));
        Assert.That(effective.Values, Is.Unique);
    }
}
