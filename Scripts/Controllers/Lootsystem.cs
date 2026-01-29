using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Resources;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class Lootsystem : Node
{
    private Dictionary<string, LootTable> Tables { get; set; } = new();

    [Export]
    public string LootTablesPath { get; set; } = "res://Resources/LootTables/";

    private void LoadAllTables()
    {
        LoadTablesFromDirectory(LootTablesPath);

        GD.Print($"Loaded {Tables.Count} loot tables");
    }

    private void LoadTablesFromDirectory(string path)
    {
        using var directory = DirAccess.Open(path);

        if (directory == null)
        {
            GD.PushError($"Failed to open directory: {path}");
            return;
        }

        directory.ListDirBegin();
        var fileName = directory.GetNext();

        while (!string.IsNullOrWhiteSpace(fileName))
        {
            var fullPath = path + fileName;

            if (directory.CurrentIsDir())
            {
                if (fileName != "." && fileName != "..")
                    LoadTablesFromDirectory(fullPath + "/");
            }
            else if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
            {
                var table = GD.Load<LootTable>(fullPath);

                if (table != null && !string.IsNullOrEmpty(table.TableId))
                {
                    Tables[table.TableId] = table;

                    GD.Print($"Loaded loot table: {table.TableId}");
                }
            }

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }
}