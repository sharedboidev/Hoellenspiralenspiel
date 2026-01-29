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
    [Export]
    public string TableId { get; set; } = string.Empty;

    [Export]
    public int Rolls { get; set; } = 1;

    [Export]
    public Array<LootEntry> Entries { get; set; } = new();

    private float TotalLootWeight => Entries.Sum(e => e.Weight);

    public BaseItem[] RollLoot()
    {
        var rng   = new Random();
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

                if(lootEntry.Type == LootEntry.EntryType.Nothing)
                    break;

                var itemInstance = lootEntry.ItemScene.Instantiate<BaseItem>();

                if (itemInstance is ConsumableItem consumableItem)
                    consumableItem.StacksizeCurrent = rng.Next(lootEntry.QuantityMin, lootEntry.QuantityMax + 1);

                drops.Add(itemInstance);

                break;
            }
        }

        return drops.ToArray();
    }
}