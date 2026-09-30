using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Die Reihenfolge der Enums ist keine Anzeigereihenfolge, deshalb stehen die Ränge hier ausdrücklich
public static class ItemTypeOrder
{
    private static readonly WeaponType[] WeaponTypes =
    [
        WeaponType.Sword, WeaponType.Axe, WeaponType.Flail, WeaponType.Dagger, WeaponType.Bow, WeaponType.Staff, WeaponType.Wand
    ];

    private static readonly ItemSlot[] ArmorSlots =
    [
        ItemSlot.Helmet, ItemSlot.Shoulders, ItemSlot.Back, ItemSlot.Torso, ItemSlot.Wrists, ItemSlot.Hands, ItemSlot.Belt, ItemSlot.Legs, ItemSlot.Feet,
        ItemSlot.Neck, ItemSlot.Ring1, ItemSlot.Ring2, ItemSlot.Ring3, ItemSlot.Ring4
    ];

    private static readonly ConsumableEffectKind[] ConsumableKinds = [ConsumableEffectKind.RestoreLife, ConsumableEffectKind.RestoreMana];

    private static readonly ItemRarity[] Rarities = [ItemRarity.Rare, ItemRarity.Magic, ItemRarity.Normal];

    private static readonly int OtherWeaponsRank     = WeaponTypes.Length;
    private static readonly int ShieldRank           = OtherWeaponsRank + 1;
    private static readonly int FirstArmorRank       = ShieldRank + 1;
    private static readonly int FirstConsumableRank  = FirstArmorRank + ArmorSlots.Length;
    private static readonly int OtherConsumablesRank = FirstConsumableRank + ConsumableKinds.Length;
    private static readonly int UnknownRank          = OtherConsumablesRank + 1;

    public static int GetRank(ItemDefinition definition)
        => definition switch
        {
            null                          => UnknownRank,
            { Kind: ItemKind.Weapon }     => RankIn(WeaponTypes, definition.Weapon.WeaponType, 0, OtherWeaponsRank),
            { IsShield: true }            => ShieldRank,
            { Kind: ItemKind.Armor }      => RankIn(ArmorSlots, definition.Slot, FirstArmorRank, UnknownRank),
            { Kind: ItemKind.Consumable } => RankIn(ConsumableKinds, definition.Consumable.Kind, FirstConsumableRank, OtherConsumablesRank),
            _                             => UnknownRank
        };

    public static List<ItemInstance> Sort(IEnumerable<ItemInstance> items)
        => (items ?? [])
           .Where(item => item is not null)
           .OrderBy(item => GetRank(item.Definition))
           .ThenBy(item => item.Definition.Name, StringComparer.Ordinal)
           .ThenBy(item => item.Definition.Id, StringComparer.Ordinal)
           .ThenBy(item => RankIn(Rarities, item.Rarity, 0, Rarities.Length))
           .ToList();

    private static int RankIn<T>(T[] table, T value, int firstRank, int missingRank)
    {
        var index = Array.IndexOf(table, value);

        return index < 0 ? missingRank : firstRank + index;
    }
}
