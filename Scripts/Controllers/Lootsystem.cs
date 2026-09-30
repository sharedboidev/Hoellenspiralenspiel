using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources;
using Hoellenspiralenspiel.Resources.Affixes;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class Lootsystem : Node
{
    private readonly Dictionary<string, LootTableDefinition> tables = new();
    private          LootRoller                              roller;

    [Export]
    public int MaximumAffixesPerItem { get; set; } = AffixRoller.DefaultMaxAffixesPerItem;

    [Export]
    public string LootTablesPath { get; set; } = "res://Resources/LootTables";

    [Export]
    public string AffixesPath { get; set; } = "res://Resources/Affixes";

    //Auch der Händler würfelt seine Ware mit diesen Affixen
    public AffixRoller AffixRoller { get; private set; }

    public override void _Ready()
    {
        LoadTables();

        AffixRoller = new AffixRoller(LoadAffixes(), MaximumAffixesPerItem);
        roller      = new LootRoller(ItemLibrary.Catalog, AffixRoller);
    }

    public IReadOnlyList<ItemInstance> GenerateLoot(Enemy enemy)
        => GenerateLoot(enemy.LootTableId, enemy.Level, enemy.LootRolls);

    public IReadOnlyList<ItemInstance> GenerateLoot(string tableId, int itemLevel = 1, int timesRolled = 1)
    {
        if (string.IsNullOrEmpty(tableId) || !tables.TryGetValue(tableId, out var table))
            return [];

        var loot = new List<ItemInstance>();

        for (var i = 0; i < timesRolled; i++)
            loot.AddRange(roller.Roll(table, GameRandom.Shared, itemLevel));

        return loot;
    }

    private void LoadTables()
    {
        foreach (var path in ResourceFiles.ListIn(LootTablesPath))
        {
            if (ResourceLoader.Load(path) is LootTable table && !string.IsNullOrEmpty(table.TableId))
                tables[table.TableId] = table.ToDefinition();
        }

        GD.Print($"Loaded {tables.Count} loot tables");
    }

    private List<AffixDefinition> LoadAffixes()
    {
        var affixes = new List<AffixDefinition>();

        foreach (var path in ResourceFiles.ListIn(AffixesPath))
        {
            if (ResourceLoader.Load(path) is Affix affix)
                affixes.Add(affix.ToDefinition());
        }

        GD.Print($"Loaded {affixes.Count} Affixes");

        return affixes;
    }
}
