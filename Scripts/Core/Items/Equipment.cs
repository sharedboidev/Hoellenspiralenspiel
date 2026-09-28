using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class Equipment
{
    public const ItemSlot MainHandPlace = ItemSlot.PhysicalWeapon;
    public const ItemSlot OffhandPlace  = ItemSlot.Offhand;

    private readonly Dictionary<ItemSlot, ItemInstance> items = new();

    public IReadOnlyDictionary<ItemSlot, ItemInstance> Items => items;

    public ItemInstance MainHand => Get(MainHandPlace);
    public ItemInstance Offhand  => Get(OffhandPlace);

    public event Action<ItemInstance> Equipped;
    public event Action<ItemInstance> Unequipped;

    //Physische Waffen und Zauberwaffen teilen sich die Haupthand
    public static ItemSlot GetPlaceFor(ItemSlot itemSlot)
        => itemSlot == ItemSlot.SpellWeapon ? MainHandPlace : itemSlot;

    public static bool CanHold(ItemSlot place, ItemInstance item)
        => item is not null && item.Definition.IsEquippable && item.Definition.Slot != ItemSlot.Undefined && GetPlaceFor(item.Definition.Slot) == GetPlaceFor(place);

    public ItemInstance Get(ItemSlot place)
        => items.GetValueOrDefault(GetPlaceFor(place));

    public ItemInstance Put(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var place  = GetPlaceFor(item.Definition.Slot);
        var former = Take(place);

        items[place] = item;

        Equipped?.Invoke(item);

        return former;
    }

    public ItemInstance Take(ItemSlot place)
    {
        if (!items.Remove(GetPlaceFor(place), out var item))
            return null;

        Unequipped?.Invoke(item);

        return item;
    }

    public void Clear()
    {
        foreach (var place in new List<ItemSlot>(items.Keys))
            Take(place);
    }
}
