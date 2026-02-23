using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Resources.Affixes;

[GlobalClass]
public abstract partial class Affix : Resource
{
    [Export]
    public CombatStat AffectedCombatStat { get; set; }

    [Export]
    public ModificationType ModificationType { get; set; }

    [Export]
    public Array<ItemType> AffectableItemTypes { get; set; } = new();

    [Export]
    public bool AllowFractions { get; set; }

    [Export]
    public Array<AffixTier> Tiers { get; set; } = new();
}