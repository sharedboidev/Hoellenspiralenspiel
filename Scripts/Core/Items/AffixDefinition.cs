using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Bei "Adds X to Y" kommt X aus MinValue bis MaxValue und Y aus MinValueTo bis MaxValueTo
public sealed record AffixTierDefinition(int    Tier,
                                         int    MinItemLevel,
                                         int    Weight,
                                         float  MinValue,
                                         float  MaxValue,
                                         string NameAddition,
                                         float  MinValueTo = 0f,
                                         float  MaxValueTo = 0f)
{
    public bool HasRange => MaxValueTo > 0f;
}

public sealed record AffixDefinition(AffixType Type, CombatStat Stat, ModificationType Modification)
{
    public IReadOnlyList<ItemSlot>            AllowedSlots       { get; init; } = Array.Empty<ItemSlot>();
    public IReadOnlyList<WeaponType>          AllowedWeaponTypes { get; init; } = Array.Empty<WeaponType>();
    public IReadOnlyList<AffixTierDefinition> Tiers              { get; init; } = Array.Empty<AffixTierDefinition>();
    public bool                               AllowsFractions    { get; init; }
    public bool                               IsLocal            { get; init; }

    public bool CanAppearOn(ItemSlot slot)
    {
        foreach (var allowedSlot in AllowedSlots)
        {
            if (allowedSlot == slot)
                return true;
        }

        return false;
    }

    //Ohne Waffentypen gilt das Affix für alles im Slot, mit ihnen nur für Waffen dieser Typen
    public bool CanAppearOn(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!CanAppearOn(item.Slot))
            return false;

        if (AllowedWeaponTypes.Count == 0)
            return true;

        foreach (var weaponType in AllowedWeaponTypes)
        {
            if (item.Weapon?.WeaponType == weaponType)
                return true;
        }

        return false;
    }
}
