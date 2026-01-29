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
    public string ItemId { get; set; } = string.Empty;

    [Export]
    public string ItemScenePath { get; set; } = string.Empty;

    public PackedScene ItemScene => ResourceLoader.Load<PackedScene>(ItemScenePath);

    [Export]
    public LootTable NestedTable { get; set; }

    [Export]
    public float Weight { get; set; } = 1.0f;

    [Export]
    public int QuantityMin { get; set; } = 1;

    [Export]
    public int QuantityMax { get; set; } = 1;
}