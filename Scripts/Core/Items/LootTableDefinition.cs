using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public enum LootEntryKind
{
    Item,
    NestedTable,
    Nothing
}

public sealed class LootEntryDefinition
{
    public LootEntryKind       Kind        { get; init; }
    public string              ItemId      { get; init; }
    public LootTableDefinition NestedTable { get; init; }
    public float               Weight      { get; init; } = 1f;
    public int                 QuantityMin { get; init; } = 1;
    public int                 QuantityMax { get; init; } = 1;
}

//Eine Klasse statt eines Records, damit sich Tabellen gegenseitig enthalten können
public sealed class LootTableDefinition
{
    public string                    Id      { get; init; } = string.Empty;
    public int                       Rolls   { get; init; } = 1;
    public List<LootEntryDefinition> Entries { get; }       = new();
}
