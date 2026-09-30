using System.Text.Json.Serialization;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

public enum DisplayMode
{
    Borderless,
    Exclusive,
    Windowed
}

//Einstellungen des Rechners, nicht des Charakters. Sie gelten für alle Plätze.
//Ton und Tasten kommen als eigene Abschnitte dazu. Fehlt ein Abschnitt in der Datei, gilt sein Standard
public sealed class GameSettings
{
    public const int CurrentVersion = 1;

    public int             Version { get; set; } = CurrentVersion;
    public DisplaySettings Display { get; set; } = new();
    public AudioSettings   Audio   { get; set; } = new();

    public GameSettings Copy()
        => new() { Version = Version, Display = Display.Copy(), Audio = Audio.Copy() };
}

public sealed class DisplaySettings
{
    public DisplayMode Mode         { get; set; } = DisplayMode.Borderless;
    public int         WindowWidth  { get; set; } = 1600;
    public int         WindowHeight { get; set; } = 900;

    //Die Größe gilt nur im Fenster. Im Vollbild läuft das Spiel in der Auflösung des Bildschirms
    [JsonIgnore]
    public PixelSize WindowSize => new(WindowWidth, WindowHeight);

    public DisplaySettings Copy()
        => new() { Mode = Mode, WindowWidth = WindowWidth, WindowHeight = WindowHeight };

    public bool SameAs(DisplaySettings other)
        => other is not null && Mode == other.Mode && WindowWidth == other.WindowWidth && WindowHeight == other.WindowHeight;
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
