using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;

namespace Hoellenspiralenspiel.Scripts.Saving;

public static class SaveGameStore
{
    public const string DefaultPath = "user://saves/character.json";

    private const string TemporarySuffix = ".tmp";
    private const string BrokenSuffix    = ".broken";

    private const string FileArgument = "--save-file=";

    public static string FilePath { get; set; } = DefaultPath;

    public static bool Exists => FileAccess.FileExists(FilePath);

    //Aufruf mit "-- --save-file=user://saves/test.json" spielt mit einem anderen Spielstand
    public static void UseFileFromCommandLine()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(FileArgument) && argument.Length > FileArgument.Length)
                FilePath = argument[FileArgument.Length..];
        }
    }

    public static SaveGame Load()
    {
        if (!Exists)
            return null;

        using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);

        if (file is null)
        {
            GD.PushWarning($"Der Spielstand {FilePath} lässt sich nicht öffnen: {FileAccess.GetOpenError()}");

            return null;
        }

        var json = file.GetAsText();

        file.Close();

        if (SaveGameSerializer.TryDeserialize(json, out var save))
            return save;

        SetBrokenFileAside();

        return null;
    }

    public static bool Save(SaveGame save)
    {
        var directory = FilePath.GetBaseDir();
        var error     = DirAccess.MakeDirRecursiveAbsolute(directory);

        if (error != Error.Ok)
        {
            GD.PushWarning($"Der Ordner {directory} lässt sich nicht anlegen: {error}");

            return false;
        }

        //Erst in eine zweite Datei schreiben, damit ein Absturz mitten im Schreiben den alten Spielstand nicht zerstört
        var temporaryPath = FilePath + TemporarySuffix;

        using (var file = FileAccess.Open(temporaryPath, FileAccess.ModeFlags.Write))
        {
            if (file is null)
            {
                GD.PushWarning($"Der Spielstand {temporaryPath} lässt sich nicht schreiben: {FileAccess.GetOpenError()}");

                return false;
            }

            file.StoreString(SaveGameSerializer.Serialize(save));
        }

        error = DirAccess.RenameAbsolute(temporaryPath, FilePath);

        if (error != Error.Ok)
            GD.PushWarning($"Der Spielstand {FilePath} lässt sich nicht ersetzen: {error}");

        return error == Error.Ok;
    }

    public static void Delete()
    {
        if (Exists)
            DirAccess.RemoveAbsolute(FilePath);
    }

    private static void SetBrokenFileAside()
    {
        var brokenPath = FilePath + BrokenSuffix;

        GD.PushWarning($"Der Spielstand {FilePath} ist nicht lesbar. Er liegt jetzt unter {brokenPath}, das Spiel beginnt mit einem neuen Charakter.");

        DirAccess.RenameAbsolute(FilePath, brokenPath);
    }
}
