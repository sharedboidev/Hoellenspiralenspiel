using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class CharacterItems
{
    public const int DefaultStashWidth  = 14;
    public const int DefaultStashHeight = 10;

    private readonly Func<Requirement, int> getCharacterValue;

    public CharacterItems(int                    inventoryWidth,
                          int                    inventoryHeight,
                          Func<Requirement, int> getCharacterValue,
                          int                    stashWidth  = DefaultStashWidth,
                          int                    stashHeight = DefaultStashHeight)
    {
        ArgumentNullException.ThrowIfNull(getCharacterValue);

        this.getCharacterValue = getCharacterValue;

        Inventory = new InventoryGrid(inventoryWidth, inventoryHeight);
        Stash     = new InventoryGrid(stashWidth, stashHeight);
    }

    public InventoryGrid Inventory { get; }

    public InventoryGrid Stash { get; }

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

    public bool HasRoomFor(ItemInstance item)
    {
        if (item is null)
            return false;

        if (Inventory.FindFreeCellFor(item) is not null)
            return true;

        var freeStackSpace = 0;

        foreach (var existing in Inventory.GetItemsInReadingOrder())
        {
            if (existing.CanStackWith(item))
                freeStackSpace += existing.FreeStackSpace;
        }

        return item.Definition.IsStackable && freeStackSpace >= item.StackSize;
    }

    public bool PickUp(ItemInstance item)
    {
        if (item is null)
            return false;

        var stackBefore = item.StackSize;
        var wasTaken    = Store(Inventory, item);

        if (wasTaken || item.StackSize != stackBefore)
            RaiseChanged();

        return wasTaken;
    }

    public bool TakeFromInventory(ItemInstance item)
        => TakeFrom(Inventory, item);

    public bool TakeFrom(InventoryGrid grid, ItemInstance item)
    {
        if (HeldItem is not null || !Owns(grid) || !grid.Remove(item))
            return false;

        HeldItem = item;

        RaiseChanged();

        return true;
    }

    public bool PlaceHeldAt(GridCell cell)
        => PlaceHeldAt(Inventory, cell);

    public bool PlaceHeldAt(InventoryGrid grid, GridCell cell)
    {
        if (HeldItem is null || !Owns(grid))
            return false;

        var target  = grid.ClampIntoGrid(HeldItem, cell);
        var covered = grid.GetItemsUnder(HeldItem, target);

        if (covered.Count > 1)
            return false;

        if (covered.Count == 0)
        {
            grid.TryPlace(HeldItem, target);

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

            grid.Remove(swapped);
            grid.TryPlace(HeldItem, target);

            HeldItem = swapped;
        }

        RaiseChanged();

        return true;
    }

    //Der schnelle Weg zwischen Inventar und Truhe. Von einem Stapel wandert, was drüben Platz findet
    public bool Transfer(ItemInstance item, InventoryGrid from, InventoryGrid to)
    {
        if (item is null || from == to || !Owns(from) || !Owns(to) || !from.Contains(item))
            return false;

        var stackBefore = item.StackSize;

        if (Store(to, item))
            from.Remove(item);
        else if (item.StackSize == stackBefore)
            return false;

        RaiseChanged();

        return true;
    }

    //Wer ein Fenster schließt, soll das Item an der Maus nicht aus Versehen fallen lassen
    public bool ReturnHeld()
        => StowHeld(Inventory);

    public bool StowHeld(InventoryGrid grid)
    {
        if (HeldItem is null)
            return true;

        if (!Owns(grid))
            return false;

        var stackBefore = HeldItem.StackSize;

        if (Store(grid, HeldItem))
            HeldItem = null;
        else if (HeldItem.StackSize == stackBefore)
            return false;

        RaiseChanged();

        return HeldItem is null;
    }

    //Gibt ein Item aus Inventar oder Hand ganz ab, etwa beim Verkauf
    public bool Release(ItemInstance item)
    {
        if (item is null)
            return false;

        if (HeldItem == item)
            HeldItem = null;
        else if (!Inventory.Remove(item))
            return false;

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

    public int CountInInventory(string baseId)
        => Inventory.GetItemsInReadingOrder().Where(item => item.Definition.Id == baseId).Sum(item => item.StackSize);

    //Der kleinste Stapel zuerst, so wird am ehesten ein Platz im Inventar frei
    public ItemInstance FindStackToConsume(string baseId)
        => Inventory.GetItemsInReadingOrder()
                    .Where(item => item.Definition.Id == baseId && item.Definition.Consumable is not null)
                    .MinBy(item => item.StackSize);

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

    public void Restore(IEnumerable<(ItemInstance Item, GridCell Cell)> inventory,
                        IEnumerable<ItemInstance>                       equipment,
                        IEnumerable<ItemInstance>                       unplaced,
                        IEnumerable<(ItemInstance Item, GridCell Cell)> stash = null)
    {
        Equipment.Clear();
        Inventory.Clear();
        Stash.Clear();

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

        //Die Truhe hat keinen Boden. Was dort keinen Platz mehr findet, wandert ins Inventar
        foreach (var (item, cell) in stash ?? [])
        {
            if (!Stash.TryPlace(item, cell) && !Stash.TryAdd(item))
                homeless.Add(item);
        }

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

    private bool Owns(InventoryGrid grid)
        => grid is not null && (grid == Inventory || grid == Stash);

    private void StoreOrDrop(ItemInstance item)
    {
        if (!Store(Inventory, item))
            Dropped?.Invoke(item);
    }

    private static bool Store(InventoryGrid grid, ItemInstance item)
    {
        if (!item.Definition.IsStackable)
            return grid.TryAdd(item);

        foreach (var existing in grid.GetItemsInReadingOrder())
        {
            if (existing.CanStackWith(item))
                MoveStack(item, existing);

            if (item.StackSize == 0)
                return true;
        }

        return grid.TryAdd(item);
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
