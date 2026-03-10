using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Inventory : PanelContainer
{
    public delegate void EquippingItemEventHandler(InventorySlot fromSlot);

    private PackedScene                                          inventoryItemScene = GD.Load<PackedScene>("res://Scenes/UI/inventory_item.tscn");
    private GridContainer                                        itemGrid;
    private Godot.Collections.Dictionary<Vector2, bool>          occupationMatrix = new();
    private Godot.Collections.Dictionary<Vector2, InventorySlot> slotMap          = new();
    private bool                                                 slotsGenerated;
    private Vector2                                              slotSize;
    private BaseTooltip                                          Tooltip     => GetTree().CurrentScene.GetNode<ItemTooltip>("%" + nameof(ItemTooltip));
    private MouseObject                                          MouseObject => GetNode<MouseObject>(nameof(MouseObject));

    [Export]
    public PackedScene SlotScene { get; set; }

    [Export]
    public int AmountSlots { get; set; } = 30;

    public GridContainer ItemGrid
    {
        get
        {
            itemGrid ??= GetNode<GridContainer>("%ItemGrid");

            return itemGrid;
        }
    }

    public event EquippingItemEventHandler EquippingItem;

    public override void _Ready()
    {
        BuildInventory();
        GetInventorySlotSize();
    }

    public BaseItem RetrieveItem(InventorySlot slot)
    {
        var rootCoord     = slot.InventoryCoordinate;
        var itemDimension = ((BaseItem)slot.ContainedInventoryItem.ContainedItem).SlotSize;

        InventoryItem retrievedItem = null;

        for (var w = 0; w < itemDimension.X; w++)
        {
            for (var h = 0; h < itemDimension.Y; h++)
            {
                var nextSlotCoord = rootCoord + new Vector2(w, h);
                var nextSlot      = slotMap[nextSlotCoord];

                if (w == 0 && h == 0)
                    retrievedItem = nextSlot.RetrieveItem();
                else
                    nextSlot.Reset();

                FreeOccupation(nextSlot);
            }
        }

        var actualItem = (BaseItem)retrievedItem?.ContainedItem;
        retrievedItem?.QueueFree();

        return actualItem;
    }

    public InventoryItem CreateInventoryItem()
        => inventoryItemScene.Instantiate<InventoryItem>();

    private void GetInventorySlotSize()
        => slotSize = this.GetAllChildren<InventorySlot>()[0].Size;

    public bool SetItem(BaseItem item)
    {
        var freeSlot = GetNextFreeSlotOrDefaultFor(item);

        if (freeSlot is null)
            return false;

        PutInventoryItemIntoInventory(item, freeSlot);

        return true;
    }

    private void PutInventoryItemIntoInventory(BaseItem item, InventorySlot freeSlot)
    {
        var inventoryItem = ConfigureInventoryItem(item, freeSlot);

        HandleSlotOccupation(freeSlot, inventoryItem);
        AddToOverlay(inventoryItem);
    }

    private void AddToOverlay(InventoryItem inventoryItem) => GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<Control>("OverlayLayer").AddChild(inventoryItem);

    private void HandleSlotOccupation(InventorySlot freeSlot, InventoryItem inventoryItem)
    {
        var occupiedSlotsCoordinates = FindOccupiedCoordinates(freeSlot, inventoryItem);

        foreach (var slotsCoordinate in occupiedSlotsCoordinates)
            SetSlotOccupied(inventoryItem, slotsCoordinate);
    }

    private Vector2[] FindOccupiedCoordinates(InventorySlot freeSlot, InventoryItem inventoryItem)
    {
        var occupiedSlots = new List<InventorySlot>();

        for (var w = 0; w < inventoryItem.SlotWidth; w++)
        for (var h = 0; h < inventoryItem.SlotHeight; h++)
            occupiedSlots.Add(slotMap[freeSlot.InventoryCoordinate + new Vector2(w, h)]);

        return occupiedSlots.Select(slot => slot.InventoryCoordinate).ToArray();
    }

    private void SetSlotOccupied(InventoryItem inventoryItem, Vector2 slotsCoordinate)
    {
        slotMap[slotsCoordinate].SetItem(inventoryItem);
        slotMap[slotsCoordinate].IsOccupied = true;
        occupationMatrix[slotsCoordinate]   = true;
    }

    private InventoryItem ConfigureInventoryItem(BaseItem item, InventorySlot freeSlot)
    {
        var inventoryItem = CreateInventoryItem();
        inventoryItem.Init(item, freeSlot.CustomMinimumSize);
        inventoryItem.RootSlot = freeSlot;
        inventoryItem.Position = freeSlot.InventoryCoordinate * freeSlot.CustomMinimumSize;

        SubscribeToUseractions(inventoryItem);

        return inventoryItem;
    }

    private void SubscribeToUseractions(InventoryItem inventoryItem)
    {
        inventoryItem.MouseMoving     += InventorySlotOnMouseMoving;
        inventoryItem.WasRightClicked += InventorySlotOnEquippingItem;
        inventoryItem.ItemConsumed    += InventoryItemOnItemConsumed;
        inventoryItem.SwappingItem    += InventoryItemOnSwappingItem;
        inventoryItem.WithdrawingItem += InventoryItemOnWithdrawingItem;
        inventoryItem.MergingItem     += InventoryItemOnMergingItem;
    }

    private void InventoryItemOnMergingItem(InventorySlot intoSlot)
    {
        var itemToMergeIntoSlot = MouseObject.RetrieveItem();

        var inventoryItem = CreateInventoryItem();
        inventoryItem.ContainedItem = itemToMergeIntoSlot;

        var couldSet = intoSlot.SetItem(inventoryItem);

        if (!couldSet)
            MouseObject.Show(itemToMergeIntoSlot);
    }

    private void InventoryItemOnSwappingItem(InventorySlot fromrootslot)
    {
        var itemFromInventory   = RetrieveItem(fromrootslot);
        var itemFromMouseObject = MouseObject.RetrieveItem();

        var fits = FitsIntoSlot(itemFromMouseObject, fromrootslot.InventoryCoordinate);

        if (fits)
        {
            PutInventoryItemIntoInventory(itemFromMouseObject, fromrootslot);
            MouseObject.Show(itemFromInventory);
        }
        else
        {
            PutInventoryItemIntoInventory(itemFromInventory, fromrootslot);
            MouseObject.Show(itemFromMouseObject);
        }
    }

    private void InventoryItemOnWithdrawingItem(InventorySlot fromrootslot)
    {
        var retrievedItem = RetrieveItem(fromrootslot);

        MouseObject.Show(retrievedItem);
    }

    private void InventoryItemOnItemConsumed(InventorySlot fromrootslot) => FreeOccupation(fromrootslot);

    private void FreeOccupation(InventorySlot fromrootslot) => occupationMatrix[fromrootslot.InventoryCoordinate] = false;

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent)
            return;

        switch (mouseEvent.ButtonIndex)
        {
            case MouseButton.Left:
                TryDropItem(mouseEvent);

                break;
            case MouseButton.Right: break;
        }
    }

    private void TryDropItem(InputEventMouseButton mouseEvent)
    {
        if (CheckClickedOutsideInventory(mouseEvent))
            MouseObject.DropItem();
    }

    private bool CheckClickedOutsideInventory(InputEventMouseButton mouseEvent)
    {
        var parent        = GetParent<VBoxContainer>();
        var parentSIze    = parent.Size;
        var prentPosition = parent.GlobalPosition;

        return mouseEvent.GlobalPosition.X < prentPosition.X || mouseEvent.GlobalPosition.Y < prentPosition.Y || mouseEvent.GlobalPosition.Y > prentPosition.Y + parentSIze.Y || mouseEvent.GlobalPosition.X > prentPosition.X + parentSIze.X;
    }

    private void BuildInventory()
    {
        if (slotsGenerated)
            return;

        var gridWidth = ItemGrid.Columns;
        var column    = 0;
        var row       = 0;

        for (var i = 0; i < AmountSlots; i++)
        {
            var inventorySlot = CreateInventorySlot(column, row);

            ItemGrid.AddChild(inventorySlot);
            slotMap.Add(inventorySlot.InventoryCoordinate, inventorySlot);
            occupationMatrix.Add(inventorySlot.InventoryCoordinate, false);

            column++;

            if ((i + 1) % gridWidth == 0)
            {
                row++;
                column = 0;
            }
        }

        slotsGenerated = true;
    }

    private InventorySlot CreateInventorySlot(int column, int row)
    {
        var inventorySlot = SlotScene.Instantiate<InventorySlot>();

        inventorySlot.Inventory           =  this;
        inventorySlot.InventoryCoordinate =  new Vector2(column, row);
        inventorySlot.SlotEmptied         += InventorySlotOnSlotEmptied;
        inventorySlot.PuttingItemIntoSlot += InventorySlotOnPuttingItemIntoSlot;
        return inventorySlot;
    }

    private void InventorySlotOnPuttingItemIntoSlot(InventorySlot slottoputitemin)
    {
        var itemToPutIntoInventory = MouseObject.RetrieveItem();

        var fits = FitsIntoSlot(itemToPutIntoInventory, slottoputitemin.InventoryCoordinate);

        if (fits)
            PutInventoryItemIntoInventory(itemToPutIntoInventory, slottoputitemin);
        else
            MouseObject.Show(itemToPutIntoInventory);
    }

    private void InventorySlotOnEquippingItem(InventorySlot fromSlot)
        => EquippingItem?.Invoke(fromSlot);

    private void InventorySlotOnSlotEmptied(InventorySlot inventoryslot)
        => Tooltip.Hide();

    private void InventorySlotOnMouseMoving(MousemovementDirection mousemovementdirection, InventoryItem inventoryItem)
    {
        switch (mousemovementdirection)
        {
            case MousemovementDirection.Entered:
                if (inventoryItem?.ContainedItem == null)
                    return;

                Tooltip.Show(inventoryItem);

                break;
            case MousemovementDirection.Left:
                if (inventoryItem.ContainedItem == null)

                    return;

                Tooltip.Hide();

                break;
        }
    }

    public InventorySlot GetNextFreeSlotOrDefaultFor(BaseItem incomingItem)
    {
        foreach (var slotIsOccupied in occupationMatrix.Where(isOccupied => !isOccupied.Value))
        {
            var currentSlot = slotIsOccupied.Key;

            var fits = FitsIntoSlot(incomingItem, currentSlot);

            if (fits)
                return slotMap[currentSlot];
        }

        return null;
    }

    private bool FitsIntoSlot(BaseItem incomingItem, Vector2 currentSlot)
    {
        var adjacentSlotsAreOccupied = false;

        for (var w = 0; w < incomingItem.SlotSize.X; w++)
        {
            for (var h = 0; h < incomingItem.SlotSize.Y; h++)
            {
                if (h == 0 && w == 0)
                    continue;

                var nextSlotKey = currentSlot + new Vector2(w, h);

                if (nextSlotKey == currentSlot)
                    continue;

                var isOutOfBounds = !occupationMatrix.ContainsKey(nextSlotKey);

                if (isOutOfBounds)
                {
                    adjacentSlotsAreOccupied = true;

                    continue;
                }

                var nextSlotIsOccupied = occupationMatrix[nextSlotKey];
                adjacentSlotsAreOccupied |= nextSlotIsOccupied;
            }
        }

        return !adjacentSlotsAreOccupied;
    }
}