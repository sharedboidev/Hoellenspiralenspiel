using Godot;
using Godot.Collections;

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
}