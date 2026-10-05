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

    //Leer heißt jede Waffe im Slot
    [Export]
    public Array<WeaponType> AffectableWeaponTypes { get; set; } = new();

    [Export]
    public bool AllowFractions { get; set; }

    [Export]
    public bool IsInherentMod { get; set; }

    [Export]
    public Array<AffixTier> Tiers { get; set; } = new();

    //Ein hybrider Affix trägt einen zweiten Stat, etwa "+# to Armour, +# to maximum Life". Seine Werte stehen in jeder Stufe unter Hybrid
    [ExportGroup("Hybrid")]
    [Export]
    public bool IsHybrid { get; set; }

    [Export]
    public CombatStat HybridCombatStat { get; set; }

    [Export]
    public ModificationType HybridModificationType { get; set; }

    [Export]
    public bool HybridIsInherentMod { get; set; }

    public abstract AffixType Type { get; }

    public AffixDefinition ToDefinition()
        => new(Type, AffectedCombatStat, ModificationType)
        {
            AllowedSlots       = AffectableItemTypes.ToArray(),
            AllowedWeaponTypes = AffectableWeaponTypes.ToArray(),
            AllowsFractions    = AllowFractions,
            IsLocal            = IsInherentMod,
            Hybrid             = IsHybrid ? new AffixHybridDefinition(HybridCombatStat, HybridModificationType, HybridIsInherentMod) : null,
            Tiers              = Tiers.Where(tier => tier is not null).Select(tier => tier.ToDefinition()).ToArray()
        };
}
