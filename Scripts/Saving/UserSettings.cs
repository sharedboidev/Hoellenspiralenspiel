using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;

namespace Hoellenspiralenspiel.Scripts.Saving;

//Einstellungen des Rechners für alle Charaktere. Hängt als Autoload vor jeder Szene im Baum,
//damit Fenster, Ton und Tasten schon stimmen, bevor Hauptmenü oder Skill-Leiste sie lesen
public partial class UserSettings : Node
{
    private const string HeadlessDisplayServer = "headless";

    public static UserSettings Instance { get; private set; }

    private bool hasAppliedDisplay;

    public GameSettings Current { get; private set; } = new();

    public event Action Changed;

    public override void _EnterTree()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;

        SettingsStore.UseFileFromCommandLine();

        Current = SettingsStore.Load();

        ApplyDisplay(Current.Display);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    //Ändert eine Kopie, speichert sie und wendet sie an
    public void Change(Action<GameSettings> change)
    {
        var next = Current.Copy();

        change(next);

        SettingsSerializer.Repair(next);

        Current = next;

        SettingsStore.Save(Current);
        ApplyDisplay(Current.Display);

        Changed?.Invoke();
    }

    //Ohne Fenster gibt es nichts anzuwenden
    public async void ApplyDisplay(DisplaySettings display)
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
