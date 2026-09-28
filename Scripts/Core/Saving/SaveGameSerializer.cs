using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoellenspiralenspiel.Scripts.Core.Saving;

public static class SaveGameSerializer
{
    //Enums stehen als Namen in der Datei, damit ein Spielstand neue Enum-Werte in der Mitte übersteht
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    public static string Serialize(SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(save);

        return JsonSerializer.Serialize(save, Options);
    }

    public static bool TryDeserialize(string json, out SaveGame save)
    {
        save = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            save = JsonSerializer.Deserialize<SaveGame>(json, Options);
        }
        catch (JsonException)
        {
            return false;
        }

        return save is { Character: not null } && save.Version is > 0 and <= SaveGame.CurrentVersion;
    }
}
