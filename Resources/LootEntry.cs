using Godot;

namespace Hoellenspiralenspiel.Resources;

[GlobalClass]
public partial class LootEntry : Resource
{
    public enum EntryType
    {
        Item,
        NestedTable,
        Nothing
    }

    [Export]
    public EntryType Type { get; set; } = EntryType.Item;

    [Export]
    public PackedScene ItemScene { get; set; }

    [Export]
    public LootTable NestedTable { get; set; }

    [Export]
    public float Weight { get; set; } = 1.0f;

    [Export]
    public int QuantityMin { get; set; } = 1;

    [Export]
    public int QuantityMax { get; set; } = 1;
}