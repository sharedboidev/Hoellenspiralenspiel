using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

public enum DisplayMode
{
    Borderless,
    Exclusive,
    Windowed
}

//Wie grob die Pixel der PS1 sind. Grob kommt der PS1 mit 240 Zeilen am nächsten
public enum PixelGrain
{
    Coarse,
    Medium,
    Fine
}

//Einstellungen des Rechners, nicht des Charakters. Sie gelten für alle Plätze. Fehlt ein Abschnitt in der Datei, gilt sein Standard
public sealed class GameSettings
{
    public const int CurrentVersion = 1;

    public int             Version { get; set; } = CurrentVersion;
    public DisplaySettings Display { get; set; } = new();
    public LookSettings    Look    { get; set; } = new();
    public AudioSettings   Audio   { get; set; } = new();
    public InputSettings   Input   { get; set; } = new();

    public GameSettings Copy()
        => new() { Version = Version, Display = Display.Copy(), Look = Look.Copy(), Audio = Audio.Copy(), Input = Input.Copy() };
}

public sealed class DisplaySettings
{
    public DisplayMode Mode         { get; set; } = DisplayMode.Borderless;
    public int         WindowWidth  { get; set; } = 1600;
    public int         WindowHeight { get; set; } = 900;
    public bool        VSync        { get; set; } = true;

    //0 heißt ohne Grenze
    public int  MaxFps  { get; set; }
    public bool ShowFps { get; set; }

    //Die Größe gilt nur im Fenster. Im Vollbild läuft das Spiel in der Auflösung des Bildschirms
    [JsonIgnore]
    public PixelSize WindowSize => new(WindowWidth, WindowHeight);

    public DisplaySettings Copy()
        => new() { Mode = Mode, WindowWidth = WindowWidth, WindowHeight = WindowHeight, VSync = VSync, MaxFps = MaxFps, ShowFps = ShowFps };

    //Nur Modus und Größe fassen das Fenster an
    public bool SameWindowAs(DisplaySettings other)
        => other is not null && Mode == other.Mode && WindowWidth == other.WindowWidth && WindowHeight == other.WindowHeight;
}

public sealed class LookSettings
{
    public const float MinBrightness = 0.5f;
    public const float MaxBrightness = 1.5f;

    public PixelGrain Grain        { get; set; } = PixelGrain.Coarse;
    public bool       Dithering    { get; set; } = true;
    public bool       VertexWobble { get; set; } = true;
    public bool       RealShadows  { get; set; } = true;
    public float      Brightness   { get; set; } = 1f;

    public LookSettings Copy()
        => new() { Grain = Grain, Dithering = Dithering, VertexWobble = VertexWobble, RealShadows = RealShadows, Brightness = Brightness };

    public bool SameAs(LookSettings other)
        => other is not null && Grain == other.Grain && Dithering == other.Dithering && VertexWobble == other.VertexWobble && RealShadows == other.RealShadows
        && Brightness.Equals(other.Brightness);
}

//Anteile von 0 bis 1 je Bus. Wie laut das klingt, rechnet VolumeCurve
public sealed class AudioSettings
{
    public float Master  { get; set; } = 1f;
    public float Music   { get; set; } = 1f;
    public float Effects { get; set; } = 1f;

    public AudioSettings Copy()
        => new() { Master = Master, Music = Music, Effects = Effects };
}

//Nur was vom Standard abweicht. So erreichen neue Aktionen und geänderte Standardtasten den Spieler
public sealed class InputSettings
{
    public Dictionary<string, InputBinding> Bindings { get; set; } = new();

    public InputSettings Copy()
        => new() { Bindings = Bindings.ToDictionary(pair => pair.Key, pair => pair.Value) };
}
