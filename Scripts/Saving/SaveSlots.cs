using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;

namespace Hoellenspiralenspiel.Scripts.Saving;

//Drei feste Plätze für Charaktere, jeder mit eigener Datei
public static class SaveSlots
{
    public const int Count = 3;

    private const string DirectoryArgument = "--save-dir=";
    private const string LegacyFile        = "character.json";

    public static string Directory { get; private set; } = "user://saves";

    public static int Selected { get; private set; } = 1;

    //Der Name für einen Charakter, den es noch nicht gibt. Er entsteht erst im Spiel
    public static string PendingName { get; private set; }

    //Aufruf mit "-- --save-dir=user://saves/test" lässt die Plätze der echten Charaktere in Ruhe
    public static void UseDirectoryFromCommandLine()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(DirectoryArgument) && argument.Length > DirectoryArgument.Length)
                Directory = argument[DirectoryArgument.Length..].TrimEnd('/');
        }
    }

    public static string GetPath(int slot)
        => $"{Directory}/slot{Mathf.Clamp(slot, 1, Count)}.json";

    public static void Select(int slot, string newName = null)
    {
        Selected    = Mathf.Clamp(slot, 1, Count);
        PendingName = newName;

        SaveGameStore.FilePath = GetPath(Selected);
    }

    public static string TakePendingName()
    {
        var name = PendingName;

        PendingName = null;

        return name;
    }

    public static SaveGame Read(int slot)
        => SaveGameStore.Load(GetPath(slot));

    public static void Delete(int slot)
        => SaveGameStore.Delete(GetPath(slot));

    //Bis M7 gab es einen einzigen Charakter. Er zieht auf den ersten Platz, wenn dieser frei ist
    public static void AdoptLegacySave()
    {
        var legacyPath = $"{Directory}/{LegacyFile}";

        if (!FileAccess.FileExists(legacyPath) || FileAccess.FileExists(GetPath(1)))
            return;

        var error = DirAccess.RenameAbsolute(legacyPath, GetPath(1));

        if (error != Error.Ok)
            GD.PushWarning($"Der Spielstand {legacyPath} lässt sich nicht auf den ersten Platz legen: {error}");
    }
}
