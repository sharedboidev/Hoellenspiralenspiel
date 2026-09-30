using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

public static class SettingsSerializer
{
    //Ein unbekannter Name in der Datei, etwa aus einer späteren Fassung, fällt auf den Standard statt die ganze Datei zu verwerfen
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters    = { new LenientEnumConverter<DisplayMode>() }
    };

    public static string Serialize(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return JsonSerializer.Serialize(settings, Options);
    }

    //Liefert immer brauchbare Einstellungen. false heißt, die Datei war nicht lesbar und es gelten die Standards
    public static bool TryDeserialize(string json, out GameSettings settings)
    {
        settings = new GameSettings();

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            settings = JsonSerializer.Deserialize<GameSettings>(json, Options) ?? new GameSettings();
        }
        catch (JsonException)
        {
            settings = new GameSettings();

            return false;
        }

        Repair(settings);

        return true;
    }

    //Was von Hand in der Datei verbogen wurde, rückt auf gültige Werte
    public static void Repair(GameSettings settings)
    {
        settings.Version =   GameSettings.CurrentVersion;
        settings.Display ??= new DisplaySettings();

        if (!Enum.IsDefined(settings.Display.Mode))
            settings.Display.Mode = DisplayMode.Borderless;

        var size = WindowSizes.Fit(settings.Display.WindowSize, WindowSizes.Maximum);

        settings.Display.WindowWidth  = size.Width;
        settings.Display.WindowHeight = size.Height;

        settings.Audio         ??= new AudioSettings();
        settings.Audio.Master  =   ToShare(settings.Audio.Master);
        settings.Audio.Music   =   ToShare(settings.Audio.Music);
        settings.Audio.Effects =   ToShare(settings.Audio.Effects);
    }

    private static float ToShare(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 1f;

    private sealed class LenientEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.String && Enum.TryParse<T>(reader.GetString(), true, out var value) && Enum.IsDefined(value) ? value : default;

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
