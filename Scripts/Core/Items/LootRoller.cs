using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class LootRoller
{
    //Schutz gegen Tabellen, die sich direkt oder über Umwege selbst enthalten
    public const int MaxNestingDepth = 8;

    private readonly AffixRoller  affixRoller;
    private readonly IItemCatalog catalog;

    public LootRoller(IItemCatalog catalog, AffixRoller affixRoller)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(affixRoller);

        this.catalog     = catalog;
        this.affixRoller = affixRoller;
    }

    public List<ItemInstance> Roll(LootTableDefinition table, IRandomSource random, int itemLevel = 1)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(random);

        var drops = new List<ItemInstance>();

        Roll(table, random, itemLevel, 0, drops);

        return drops;
    }

    private void Roll(LootTableDefinition table, IRandomSource random, int itemLevel, int nestingDepth, List<ItemInstance> drops)
    {
        var totalWeight = 0f;

        foreach (var entry in table.Entries)
            totalWeight += Math.Max(0f, entry.Weight);

        if (totalWeight <= 0f)
            return;

        for (var roll = 0; roll < table.Rolls; roll++)
        {
            var entry = PickEntry(table, random.NextFloat() * totalWeight);

            AddDropsOf(entry, random, itemLevel, nestingDepth, drops);
        }
    }

    private static LootEntryDefinition PickEntry(LootTableDefinition table, float luckyNumber)
    {
        var cumulativeWeight = 0f;

        foreach (var entry in table.Entries)
        {
            cumulativeWeight += Math.Max(0f, entry.Weight);

            if (luckyNumber < cumulativeWeight)
                return entry;
        }

        return table.Entries[^1];
    }

    private void AddDropsOf(LootEntryDefinition entry, IRandomSource random, int itemLevel, int nestingDepth, List<ItemInstance> drops)
    {
        switch (entry.Kind)
        {
            case LootEntryKind.Nothing:
                return;

            case LootEntryKind.NestedTable:
                if (entry.NestedTable is not null && nestingDepth < MaxNestingDepth)
                    Roll(entry.NestedTable, random, itemLevel, nestingDepth + 1, drops);

                return;

            default:
                var definition = catalog.Find(entry.ItemId);

                if (definition is null)
                    return;

                var item = new ItemInstance(definition, itemLevel, RollQuantity(entry, definition, random));

                affixRoller.RollAffixesFor(item, random);

                drops.Add(item);

                return;
        }
    }

    private static int RollQuantity(LootEntryDefinition entry, ItemDefinition definition, IRandomSource random)
    {
        if (!definition.IsStackable)
            return 1;

        var low  = Math.Min(entry.QuantityMin, entry.QuantityMax);
        var high = Math.Max(entry.QuantityMin, entry.QuantityMax);

        return Math.Clamp(random.NextInt(low, high + 1), 1, definition.MaxStackSize);
    }
}
