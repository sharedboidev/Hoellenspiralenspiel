using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Affixes;

[GlobalClass]
public abstract partial class Affix : Resource
{
    [Export]
    public CombatStat AffectedCombatStat { get; set; }

    [Export]
    public ModificationType ModificationType { get; set; }

    [Export]
    public Array<ItemSlot> AffectableItemTypes { get; set; } = new();

    [Export]
    public bool AllowFractions { get; set; }

    [Export]
    public bool IsInherentMod { get; set; }

    [Export]
    public Array<AffixTier> Tiers { get; set; } = new();

    public abstract AffixType Type { get; }

    public AffixDefinition ToDefinition()
        => new(Type, AffectedCombatStat, ModificationType)
        {
            AllowedSlots    = AffectableItemTypes.ToArray(),
            AllowsFractions = AllowFractions,
            IsLocal         = IsInherentMod,
            Tiers           = Tiers.Where(tier => tier is not null).Select(tier => tier.ToDefinition()).ToArray()
        };
}
