using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;

namespace Hoellenspiralenspiel.Resources;

[GlobalClass]
public partial class LootTable : Resource
{
    //Schutz gegen Tabellen, die sich direkt oder über Umwege selbst enthalten
    private const int MaxNestingDepth = 8;

    [Export]
    public string TableId { get; set; } = string.Empty;

    [Export]
    public int Rolls { get; set; } = 1;

    [Export]
    public Array<LootEntry> Entries { get; set; } = new();

    private float TotalLootWeight => Entries.Sum(e => e.Weight);

    public BaseItem[] RollLoot()
        => RollLoot(new Random(), 0);

    private BaseItem[] RollLoot(Random rng, int nestingDepth)
    {
        var drops = new List<BaseItem>();

        for (int i = 0; i < Rolls; i++)
        {
            var randomNumber     = rng.Next(1, (int)TotalLootWeight+1);
            var cumulativeWeight = 0f;

            foreach (var lootEntry in Entries)
            {
                cumulativeWeight += lootEntry.Weight;

                if (!(cumulativeWeight >= randomNumber))
                    continue;

                drops.AddRange(CreateDropsOf(lootEntry, rng, nestingDepth));

                break;
            }
        }

        return drops.ToArray();
    }

    private static BaseItem[] CreateDropsOf(LootEntry lootEntry, Random rng, int nestingDepth)
    {
        switch (lootEntry.Type)
        {
            case LootEntry.EntryType.Nothing:
                return [];

            case LootEntry.EntryType.NestedTable:
                if (lootEntry.NestedTable is null || nestingDepth >= MaxNestingDepth)
                    return [];

                return lootEntry.NestedTable.RollLoot(rng, nestingDepth + 1);

            default:
                if (lootEntry.ItemScene is null)
                    return [];

                var itemInstance = lootEntry.ItemScene.Instantiate<BaseItem>();

                if (itemInstance is ConsumableItem consumableItem)
                    consumableItem.StacksizeCurrent = rng.Next(lootEntry.QuantityMin, lootEntry.QuantityMax + 1);

                return [itemInstance];
        }
    }
}
