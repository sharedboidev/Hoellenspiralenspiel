using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed record AffixTierDefinition(int Tier, int MinItemLevel, int Weight, float MinValue, float MaxValue, string NameAddition);

public sealed record AffixDefinition(AffixType Type, CombatStat Stat, ModificationType Modification)
{
    public IReadOnlyList<ItemSlot>            AllowedSlots    { get; init; } = Array.Empty<ItemSlot>();
    public IReadOnlyList<AffixTierDefinition> Tiers           { get; init; } = Array.Empty<AffixTierDefinition>();
    public bool                               AllowsFractions { get; init; }
    public bool                               IsLocal         { get; init; }

    public bool CanAppearOn(ItemSlot slot)
    {
        foreach (var allowedSlot in AllowedSlots)
        {
            if (allowedSlot == slot)
                return true;
        }

        return false;
    }
}
