using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;

namespace Hoellenspiralenspiel.Scripts.Saving;

public static class SettingsStore
{
    public const string DefaultPath = "user://settings.json";

    private const string TemporarySuffix = ".tmp";
    private const string BrokenSuffix    = ".broken";

    private const string FileArgument = "--settings-file=";

    public static string FilePath { get; set; } = DefaultPath;

    //Prüfläufe starten mit "-- --settings-file=user://test_settings.json", sonst träfen sie die Einstellungen des Spielers
    public static void UseFileFromCommandLine()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(FileArgument) && argument.Length > FileArgument.Length)
                FilePath = argument[FileArgument.Length..];
        }
    }

    public static GameSettings Load()
    {
        if (!FileAccess.FileExists(FilePath))
            return new GameSettings();

        using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);

        if (file is null)
        {
            GD.PushWarning($"Die Einstellungen {FilePath} lassen sich nicht öffnen: {FileAccess.GetOpenError()}");

            return new GameSettings();
        }

        var json = file.GetAsText();

        file.Close();

        if (!SettingsSerializer.TryDeserialize(json, out var settings))
            SetBrokenFileAside();

        return settings;
    }

    public static bool Save(GameSettings settings)
    {
        var directory = FilePath.GetBaseDir();
        var error     = DirAccess.MakeDirRecursiveAbsolute(directory);

        if (error != Error.Ok)
        {
            GD.PushWarning($"Der Ordner {directory} lässt sich nicht anlegen: {error}");

            return false;
        }

        var temporaryPath = FilePath + TemporarySuffix;

        using (var file = FileAccess.Open(temporaryPath, FileAccess.ModeFlags.Write))
        {
            if (file is null)
            {
                GD.PushWarning($"Die Einstellungen {temporaryPath} lassen sich nicht schreiben: {FileAccess.GetOpenError()}");

                return false;
            }

            file.StoreString(SettingsSerializer.Serialize(settings));
        }

        error = DirAccess.RenameAbsolute(temporaryPath, FilePath);

        if (error != Error.Ok)
            GD.PushWarning($"Die Einstellungen {FilePath} lassen sich nicht ersetzen: {error}");

        return error == Error.Ok;
    }

    private static void SetBrokenFileAside()
    {
        var brokenPath = FilePath + BrokenSuffix;

        GD.PushWarning($"Die Einstellungen {FilePath} sind nicht lesbar. Sie liegen jetzt unter {brokenPath}, es gelten die Standards.");

        DirAccess.RenameAbsolute(FilePath, brokenPath);
    }
}
