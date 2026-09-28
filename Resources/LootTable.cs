using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Items;
using TranslatedTables = System.Collections.Generic.Dictionary<Hoellenspiralenspiel.Resources.LootTable, Hoellenspiralenspiel.Scripts.Core.Items.LootTableDefinition>;

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

    public LootTableDefinition ToDefinition()
        => ToDefinition(new TranslatedTables());

    //Bereits übersetzte Tabellen kommen aus dem Zwischenspeicher, sonst liefe eine Tabelle, die sich selbst enthält, endlos
    private LootTableDefinition ToDefinition(TranslatedTables translated)
    {
        if (translated.TryGetValue(this, out var known))
            return known;

        var definition = new LootTableDefinition { Id = TableId, Rolls = Rolls };

        translated[this] = definition;

        foreach (var entry in Entries)
        {
            if (entry is not null)
                definition.Entries.Add(Translate(entry, translated));
        }

        return definition;
    }

    private static LootEntryDefinition Translate(LootEntry entry, TranslatedTables translated)
        => new()
        {
            Kind        = GetKind(entry),
            ItemId      = entry.Item?.Id,
            NestedTable = entry.Type == LootEntry.EntryType.NestedTable ? entry.NestedTable?.ToDefinition(translated) : null,
            Weight      = entry.Weight,
            QuantityMin = entry.QuantityMin,
            QuantityMax = entry.QuantityMax
        };

    private static LootEntryKind GetKind(LootEntry entry)
        => entry.Type switch
        {
            LootEntry.EntryType.NestedTable => LootEntryKind.NestedTable,
            LootEntry.EntryType.Nothing     => LootEntryKind.Nothing,
            _                               => LootEntryKind.Item
        };
}
