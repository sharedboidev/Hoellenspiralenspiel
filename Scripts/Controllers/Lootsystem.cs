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

    //Bis Gegner ein Level haben, fällt alles mit diesem Itemlevel
    [Export]
    public int ItemLevel { get; set; } = 1;

    public override void _Ready()
    {
        LoadTables();

        roller = new LootRoller(ItemLibrary.Catalog, new AffixRoller(LoadAffixes(), MaximumAffixesPerItem));
    }

    public IReadOnlyList<ItemInstance> GenerateLoot(BaseEnemy enemy)
        => GenerateLoot(enemy.LootTableId);

    public IReadOnlyList<ItemInstance> GenerateLoot(string tableId)
    {
        if (string.IsNullOrEmpty(tableId) || !tables.TryGetValue(tableId, out var table))
            return [];

        return roller.Roll(table, GameRandom.Shared, ItemLevel);
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
