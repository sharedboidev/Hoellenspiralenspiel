using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Saving;

//Einstellungen des Rechners für alle Charaktere. Hängt als Autoload vor jeder Szene im Baum,
//damit Fenster, Ton und Tasten schon stimmen, bevor Hauptmenü oder Skill-Leiste sie lesen
public partial class UserSettings : Node
{
    private const string HeadlessDisplayServer = "headless";

    //Außerhalb des Fensters kennt Godot Titelleiste und Rahmen nicht. So viel Platz halten die Größen zur Auswahl dann frei
    private static readonly Vector2I EstimatedDecoration = new(16, 64);

    public static UserSettings Instance { get; private set; }

    private bool hasAppliedDisplay;

    public GameSettings Current { get; private set; } = new();

    //Die Belegung, die gerade gilt: der Standard mit den Abweichungen des Spielers darüber
    public IReadOnlyDictionary<string, InputBinding> Bindings { get; private set; } = new Dictionary<string, InputBinding>();

    public event Action Changed;

    public override void _EnterTree()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;

        SettingsStore.UseFileFromCommandLine();

        Current = SettingsStore.Load();

        Apply(null);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    //Ändert eine Kopie, speichert sie und wendet sie an
    public void Change(Action<GameSettings> change)
        => Update(change, true);

    //Wirkt sofort, speichert aber nicht. So hört und sieht man einen Regler schon beim Ziehen, Save schreibt dann, was gilt
    public void Preview(Action<GameSettings> change)
        => Update(change, false);

    public void Save()
        => SettingsStore.Save(Current);

    //Belegt eine Aktion nach den Regeln des Kerns und speichert nur, was angenommen wurde
    public BindingResult Rebind(string action, InputBinding binding)
    {
        var bindings = Bindings.ToDictionary(pair => pair.Key, pair => pair.Value);
        var result   = BindingRules.Assign(bindings, action, binding);

        if (result.Outcome is BindingOutcome.Bound or BindingOutcome.Swapped)
            Change(settings => settings.Input.Bindings = BindingRules.Overrides(KeyBindings.Defaults, bindings));

        return result;
    }

    public void ResetBindings()
        => Change(settings => settings.Input.Bindings = new Dictionary<string, InputBinding>());

    //Wie groß ein Fenster samt Titelleiste und Rahmen höchstens werden kann
    public PixelSize GetWindowRoom()
    {
        if (DisplayServer.GetName() == HeadlessDisplayServer)
            return WindowSizes.Maximum;

        var usable     = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        var decoration = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed ? DisplayServer.WindowGetSizeWithDecorations() - DisplayServer.WindowGetSize() : EstimatedDecoration;
        var room       = usable.Size - decoration;

        return new PixelSize(room.X, room.Y);
    }

    private void Update(Action<GameSettings> change, bool isSaved)
    {
        var previous = Current;
        var next     = Current.Copy();

        change(next);

        SettingsSerializer.Repair(next);

        Current = next;

        if (isSaved)
            Save();

        Apply(previous);

        Changed?.Invoke();
    }

    //Das Fenster fasst sie nur an, wenn sich Modus oder Größe ändern, sonst spränge ein verschobenes Fenster in die Mitte
    private void Apply(GameSettings previous)
    {
        if (previous is null || !Current.Display.SameWindowAs(previous.Display))
            ApplyWindow(Current.Display);

        ApplyFrames(Current.Display);
        ApplyAudio(Current.Audio);

        if (previous is null || !IsSame(previous.Input.Bindings, Current.Input.Bindings))
            ApplyBindings();
    }

    private void ApplyBindings()
    {
        Bindings = BindingRules.Resolve(KeyBindings.Defaults, Current.Input.Bindings);

        KeyBindings.Apply(Bindings);
    }

    private static bool IsSame(IReadOnlyDictionary<string, InputBinding> first, IReadOnlyDictionary<string, InputBinding> second)
        => first.Count == second.Count && first.All(pair => second.TryGetValue(pair.Key, out var other) && other == pair.Value);

    private static void ApplyFrames(DisplaySettings display)
    {
        Engine.MaxFps = display.MaxFps;

        if (DisplayServer.GetName() == HeadlessDisplayServer)
            return;

        var vsync = display.VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled;

        if (DisplayServer.WindowGetVsyncMode() != vsync)
            DisplayServer.WindowSetVsyncMode(vsync);
    }

    private static void ApplyAudio(AudioSettings audio)
    {
        SetBus(AudioBuses.Master, audio.Master);
        SetBus(AudioBuses.Music, audio.Music);
        SetBus(AudioBuses.Effects, audio.Effects);
    }

    private static void SetBus(StringName name, float share)
    {
        var index = AudioServer.GetBusIndex(name);

        if (index < 0)
        {
            GD.PushWarning($"Den Bus {name} gibt es nicht. Er gehört in default_bus_layout.tres.");

            return;
        }

        AudioServer.SetBusMute(index, VolumeCurve.IsMuted(share));

        if (!VolumeCurve.IsMuted(share))
            AudioServer.SetBusVolumeDb(index, VolumeCurve.ToDecibels(share));
    }

    //Ohne Fenster gibt es nichts anzuwenden
    private async void ApplyWindow(DisplaySettings display)
    {
        if (DisplayServer.GetName() == HeadlessDisplayServer)
            return;

        var mode    = ToWindowMode(display.Mode);
        var isStart = !hasAppliedDisplay;

        hasAppliedDisplay = true;

        if (display.Mode != DisplayMode.Windowed)
        {
            if (DisplayServer.WindowGetMode() != mode)
                DisplayServer.WindowSetMode(mode);

            return;
        }

        //Beim Start meldet Godot ein fast bildschirmfüllendes Fenster. Eine Größe von dort aus landet einige Pixel daneben,
        //vom Vollbild aus stimmt sie. Aus dem Vollbild heraus überschreibt Windows eine Größe, die im selben Frame gesetzt wird
        if (isStart && DisplayServer.WindowGetMode() == mode)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        if (DisplayServer.WindowGetMode() != mode)
        {
            DisplayServer.WindowSetMode(mode);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        //Größe und Lage gelten für die Fläche im Fenster. Titelleiste und Rahmen müssen mit auf den Bildschirm
        var usable     = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        var decoration = DisplayServer.WindowGetSizeWithDecorations() - DisplayServer.WindowGetSize();
        var inset      = DisplayServer.WindowGetPosition() - DisplayServer.WindowGetPositionWithDecorations();
        var room       = usable.Size - decoration;
        var size       = WindowSizes.Fit(display.WindowSize, new PixelSize(room.X, room.Y));
        var pixels     = new Vector2I(size.Width, size.Height);

        DisplayServer.WindowSetSize(pixels);
        DisplayServer.WindowSetPosition(usable.Position + (usable.Size - pixels - decoration) / 2 + inset);
    }

    private static DisplayServer.WindowMode ToWindowMode(DisplayMode mode)
        => mode switch
        {
            DisplayMode.Exclusive => DisplayServer.WindowMode.ExclusiveFullscreen,
            DisplayMode.Windowed  => DisplayServer.WindowMode.Windowed,
            _                     => DisplayServer.WindowMode.Fullscreen
        };
}
