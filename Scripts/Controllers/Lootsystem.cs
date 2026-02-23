using System;
using System.IO;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Resources.Affixes;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using LootTable = Hoellenspiralenspiel.Resources.LootTable;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class Lootsystem : Node
{
    private Dictionary<string, LootTable> Tables  { get; set; } = new();
    private Array<Affix>                  Affixes { get; set; } = new();

    [Export]
    public string LootTablesPath { get; set; } = "res://Resources/LootTables/";

    [Export]
    public string AffixesPath { get; set; } = "res://Resources/Affixes";

    public override void _Ready() => LoadAllTables();

    public BaseItem[] GenerateLoot(BaseEnemy enemy)
    {
        if (!Tables.TryGetValue(enemy.LootTableId, out var fittingTable))
            return [];

        var loot = fittingTable.RollLoot();

        RollModifierFor(loot);

        return loot;
    }

    private void RollModifierFor(BaseItem[] loot)
    {

    }

    private void LoadAllTables()
    {
        LoadLootTables();
        LoadAffixes();
    }

    private void LoadAffixes()
    {
        LoadResourcesFromDirectory(AffixesPath, LoadAffixIntoCollection);

        GD.Print($"Loaded {Affixes.Count} Affixes");
    }

    private void LoadLootTables()
    {
        LoadResourcesFromDirectory(LootTablesPath, LoadTableIntoDictionary);

        GD.Print($"Loaded {Tables.Count} loot tables");
    }

    private void LoadResourcesFromDirectory(string path, Action<string> loadAction)
    {
        using var directory = DirAccess.Open(path);
        var fileName = GetDirectoryFilename(path, directory);

        while (!string.IsNullOrWhiteSpace(fileName))
        {
            var fullPath =Path.Combine(path, fileName);

            if (directory.CurrentIsDir())
            {
                if (fileName != "." && fileName != "..")
                    LoadResourcesFromDirectory(fullPath + "/", loadAction);
            }
            else if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
                loadAction(fullPath);

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    private static string GetDirectoryFilename(string path, DirAccess directory)
    {
        if (directory == null)
            throw new IOException($"Failed to open directory: {path}");

        var fileName = GetFileName(directory);
        return fileName;
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

    private void LoadAffixIntoCollection(string fullPath)
    {
        var affix = GD.Load<Affix>(fullPath);

        if(affix is null)
            return;

        Affixes.Add(affix);

        GD.Print($"Loaded Affix: {affix.ResourceName}");
    }

    private static string GetFileName(DirAccess directory)
    {
        directory.ListDirBegin();
        var fileName = directory.GetNext();
        return fileName;
    }
}