using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class CharacterItems
{
    private readonly Func<Requirement, int> getCharacterValue;

    public CharacterItems(int inventoryWidth, int inventoryHeight, Func<Requirement, int> getCharacterValue)
    {
        ArgumentNullException.ThrowIfNull(getCharacterValue);

        this.getCharacterValue = getCharacterValue;

        Inventory = new InventoryGrid(inventoryWidth, inventoryHeight);
    }

    public InventoryGrid Inventory { get; }

    public Equipment Equipment { get; } = new();

    public ItemInstance HeldItem { get; private set; }

    //Schalter für ein späteres Talent, das Zweihandwaffen einhändig führen lässt
    public bool AllowsOffhandWithTwoHander { get; set; }

    public event Action               Changed;
    public event Action<ItemInstance> Dropped;

    public IReadOnlyList<Requirement> GetUnmetRequirements(ItemInstance item)
        => ItemRequirements.GetUnmet(item.Definition, getCharacterValue);

    public bool CanEquip(ItemInstance item)
        => item is not null &&
           item.Definition.IsEquippable &&
           ItemRequirements.AreMet(item.Definition, getCharacterValue) &&
           !IsBlockedByTwoHander(item);

    public bool PickUp(ItemInstance item)
    {
        if (item is null)
            return false;

        var stackBefore = item.StackSize;
        var wasTaken    = Store(item);

        if (wasTaken || item.StackSize != stackBefore)
            RaiseChanged();

        return wasTaken;
    }

    public bool TakeFromInventory(ItemInstance item)
    {
        if (HeldItem is not null || !Inventory.Remove(item))
            return false;

        HeldItem = item;

        RaiseChanged();

        return true;
    }

    public bool PlaceHeldAt(GridCell cell)
    {
        if (HeldItem is null)
            return false;

        var target  = Inventory.ClampIntoGrid(HeldItem, cell);
        var covered = Inventory.GetItemsUnder(HeldItem, target);

        if (covered.Count > 1)
            return false;

        if (covered.Count == 0)
        {
            Inventory.TryPlace(HeldItem, target);

            HeldItem = null;
        }
        else if (covered[0].CanStackWith(HeldItem) && !covered[0].IsStackFull)
        {
            MoveStack(HeldItem, covered[0]);

            if (HeldItem.StackSize == 0)
                HeldItem = null;
        }
        else
        {
            var swapped = covered[0];

            Inventory.Remove(swapped);
            Inventory.TryPlace(HeldItem, target);

            HeldItem = swapped;
        }

        RaiseChanged();

        return true;
    }

    public bool EquipFromInventory(ItemInstance item)
    {
        if (!Inventory.Contains(item) || !CanEquip(item))
            return false;

        var formerCell = Inventory.GetPositionOf(item);

        Inventory.Remove(item);

        if (!TryClearOffhandFor(item))
        {
            Inventory.TryPlace(item, formerCell);

            return false;
        }

        var formerItem = Equipment.Put(item);

        if (formerItem is not null && !Inventory.TryPlace(formerItem, formerCell))
            StoreOrDrop(formerItem);

        RaiseChanged();

        return true;
    }

    public bool TakeFromEquipment(ItemSlot place)
    {
        if (HeldItem is not null)
            return false;

        var item = Equipment.Take(place);

        if (item is null)
            return false;

        HeldItem = item;

        RaiseChanged();

        return true;
    }

    public bool PlaceHeldInEquipment(ItemSlot place)
    {
        if (!Equipment.CanHold(place, HeldItem) || !CanEquip(HeldItem) || !TryClearOffhandFor(HeldItem))
            return false;

        HeldItem = Equipment.Put(HeldItem);

        RaiseChanged();

        return true;
    }

    public ConsumableEffect Consume(ItemInstance item)
    {
        if (item?.Definition.Consumable is null || !Inventory.Contains(item) || item.StackSize <= 0)
            return null;

        item.StackSize--;

        if (item.StackSize == 0)
            Inventory.Remove(item);

        RaiseChanged();

        return item.Definition.Consumable;
    }

    public bool DropHeld()
    {
        if (HeldItem is null)
            return false;

        var item = HeldItem;

        HeldItem = null;

        Dropped?.Invoke(item);

        RaiseChanged();

        return true;
    }

    public void Restore(IEnumerable<(ItemInstance Item, GridCell Cell)> inventory, IEnumerable<ItemInstance> equipment, IEnumerable<ItemInstance> unplaced)
    {
        Equipment.Clear();
        Inventory.Clear();

        HeldItem = null;

        var homeless = new List<ItemInstance>();

        foreach (var (item, cell) in inventory)
        {
            if (!Inventory.TryPlace(item, cell))
                homeless.Add(item);
        }

        foreach (var item in equipment)
        {
            var formerItem = Equipment.Put(item);

            if (formerItem is not null)
                homeless.Add(formerItem);
        }

        homeless.AddRange(unplaced);

        foreach (var item in homeless)
            StoreOrDrop(item);

        RaiseChanged();
    }

    private bool IsBlockedByTwoHander(ItemInstance item)
        => !AllowsOffhandWithTwoHander &&
           Equipment.GetPlaceFor(item.Definition.Slot) == Equipment.OffhandPlace &&
           Equipment.MainHand?.Definition.IsTwoHanded == true;

    private bool TryClearOffhandFor(ItemInstance item)
    {
        var needsBothHands = !AllowsOffhandWithTwoHander &&
                             item.Definition.IsTwoHanded &&
                             Equipment.GetPlaceFor(item.Definition.Slot) == Equipment.MainHandPlace;

        var offhandItem = Equipment.Offhand;

        if (!needsBothHands || offhandItem is null)
            return true;

        if (Inventory.FindFreeCellFor(offhandItem) is null)
            return false;

        Equipment.Take(Equipment.OffhandPlace);

        return Inventory.TryAdd(offhandItem);
    }

    private void StoreOrDrop(ItemInstance item)
    {
        if (!Store(item))
            Dropped?.Invoke(item);
    }

    private bool Store(ItemInstance item)
    {
        if (!item.Definition.IsStackable)
            return Inventory.TryAdd(item);

        foreach (var existing in Inventory.GetItemsInReadingOrder())
        {
            if (existing.CanStackWith(item))
                MoveStack(item, existing);

            if (item.StackSize == 0)
                return true;
        }

        return Inventory.TryAdd(item);
    }

    private static void MoveStack(ItemInstance from, ItemInstance to)
    {
        var amount = Math.Min(from.StackSize, to.FreeStackSpace);

        to.StackSize   += amount;
        from.StackSize -= amount;
    }

    private void RaiseChanged()
        => Changed?.Invoke();
}
