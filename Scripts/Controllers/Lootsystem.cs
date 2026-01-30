using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Resources;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class Lootsystem : Node
{
    private Dictionary<string, LootTable> Tables { get; set; } = new();

    [Export]
    public string LootTablesPath { get; set; } = "res://Resources/LootTables/";

    public override void _Ready() => LoadAllTables();

    public BaseItem[] GenerateLoot(BaseEnemy enemy)
    {
        if(!enemy.WillDropLoot() || !Tables.TryGetValue(enemy.LootTableId, out var fittingTable))
            return [];

        var loot = fittingTable.RollLoot();

        return loot;
    }

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

        var fileName = GetFileName(directory);

        while (!string.IsNullOrWhiteSpace(fileName))
        {
            var fullPath = path + fileName;

            if (directory.CurrentIsDir())
            {
                if (fileName != "." && fileName != "..")
                    LoadTablesFromDirectory(fullPath + "/");
            }
            else if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
                LoadTableIntoDictionary(fullPath);

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    private void LoadTableIntoDictionary(string fullPath)
    {
        var table = GD.Load<LootTable>(fullPath);

        if (table != null && !string.IsNullOrEmpty(table.TableId))
        {
            Tables[table.TableId] = table;

            GD.Print($"Loaded loot table: {table.TableId}");
        }
    }

    private static string GetFileName(DirAccess directory)
    {
        directory.ListDirBegin();
        var fileName = directory.GetNext();
        return fileName;
    }
}