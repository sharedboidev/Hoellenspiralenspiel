using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Tests.Items;

internal static class TestItems
{
    public static readonly ItemDefinition Sword = ItemDefinition.ForWeapon("sword", "Sword", ItemSlot.PhysicalWeapon, new WeaponStats(4, 9, 1.4f, 5, WeaponType.Sword, WieldStrategy.MainHand)) with
    {
        Width        = 1,
        Height       = 3,
        Guard        = new GuardStats(MeleeParry: 5),
        Requirements = new Dictionary<Requirement, int> { [Requirement.Strength] = 2 }
    };

    public static readonly ItemDefinition Staff = ItemDefinition.ForWeapon("staff", "Staff", ItemSlot.SpellWeapon, new WeaponStats(10, 14, 0.33f, 3, WeaponType.Staff, WieldStrategy.TwoHand)) with
    {
        Width  = 1,
        Height = 4,
        Guard  = new GuardStats(10, 5)
    };

    public static readonly ItemDefinition Bow = ItemDefinition.ForWeapon("bow",
                                                                         "Bow",
                                                                         ItemSlot.PhysicalWeapon,
                                                                         new WeaponStats(5, 11, 1.2f, 6, WeaponType.Bow, WieldStrategy.TwoHand)
                                                                         {
                                                                             Range           = 700,
                                                                             IsRanged        = true,
                                                                             ProjectileSpeed = 1400
                                                                         }) with
    {
        Width  = 1,
        Height = 3
    };

    public static readonly ItemDefinition Shield = ItemDefinition.ForArmor("shield", "Shield", ItemSlot.Offhand, 8) with
    {
        Width  = 2,
        Height = 2,
        Guard  = new GuardStats(15, 8)
    };

    public static readonly ItemDefinition Helmet = ItemDefinition.ForArmor("helmet", "Helmet", ItemSlot.Helmet, 10) with
    {
        Width  = 2,
        Height = 2
    };

    public static readonly ItemDefinition Potion = ItemDefinition.ForConsumable("potion", "Potion", new ConsumableEffect(ConsumableEffectKind.RestoreLife, 20), 5);

    public static readonly ItemDefinition Pebble = ItemDefinition.ForArmor("pebble", "Pebble", ItemSlot.Neck, 0);

    public static readonly TestCatalog Catalog = new(Sword, Staff, Bow, Shield, Helmet, Potion, Pebble);

    public static ItemInstance Create(ItemDefinition definition, int stackSize = 1, int itemLevel = 1)
        => new(definition, itemLevel, stackSize);

    public static ItemAffix Local(CombatStat stat, ModificationType modification, float value, AffixType type = AffixType.Prefix)
        => new(type, stat, modification, value, "Local", true);

    public static ItemAffix Global(CombatStat stat, ModificationType modification, float value, AffixType type = AffixType.Suffix)
        => new(type, stat, modification, value, "of Global", false);

    public static CharacterItems CreateCharacterItems(int width = 6, int height = 4, int strength = 10)
        => new(width, height, requirement => requirement == Requirement.Strength ? strength : 1);
}

internal sealed class TestCatalog : IItemCatalog
{
    private readonly Dictionary<string, ItemDefinition> definitions = new();

    public TestCatalog(params ItemDefinition[] items)
    {
        foreach (var item in items)
            definitions[item.Id] = item;
    }

    public ItemDefinition Find(string itemId)
        => itemId is null ? null : definitions.GetValueOrDefault(itemId);
}
