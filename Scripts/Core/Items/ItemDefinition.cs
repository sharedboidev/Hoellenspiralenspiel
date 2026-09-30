using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed record ItemDefinition
{
    private static readonly IReadOnlyDictionary<Requirement, int> NoRequirements = new Dictionary<Requirement, int>();

    private ItemDefinition(string id, string name, ItemKind kind, ItemSlot slot)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Eine Item-Basis braucht eine Id.", nameof(id));

        Id   = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Kind = kind;
        Slot = slot;
    }

    public string   Id   { get; }
    public string   Name { get; }
    public ItemKind Kind { get; }
    public ItemSlot Slot { get; }

    public int Width        { get; init; } = 1;
    public int Height       { get; init; } = 1;
    public int MaxStackSize { get; init; } = 1;

    //Grundpreis beim Händler für ein Stück ohne Affixe. 0 heißt unverkäuflich
    public int Price { get; init; }

    public IReadOnlyDictionary<Requirement, int> Requirements { get; init; } = NoRequirements;

    public WeaponStats      Weapon     { get; private init; }
    public float            Armor      { get; private init; }
    public ConsumableEffect Consumable { get; private init; }
    public GuardStats       Guard      { get; init; } = GuardStats.None;

    public bool IsStackable  => MaxStackSize > 1;
    public bool IsEquippable => Kind != ItemKind.Consumable;
    public bool IsTwoHanded  => Weapon?.WieldStrategy == WieldStrategy.TwoHand;
    public bool IsShield     => Kind == ItemKind.Armor && Slot == ItemSlot.Offhand;

    public static ItemDefinition ForWeapon(string id, string name, ItemSlot slot, WeaponStats weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);

        return new ItemDefinition(id, name, ItemKind.Weapon, slot) { Weapon = weapon };
    }

    public static ItemDefinition ForArmor(string id, string name, ItemSlot slot, float armor)
        => new(id, name, ItemKind.Armor, slot) { Armor = armor };

    public static ItemDefinition ForConsumable(string id, string name, ConsumableEffect effect, int maxStackSize)
    {
        ArgumentNullException.ThrowIfNull(effect);

        return new ItemDefinition(id, name, ItemKind.Consumable, ItemSlot.Consumable)
        {
            Consumable   = effect,
            MaxStackSize = Math.Max(1, maxStackSize)
        };
    }
}
