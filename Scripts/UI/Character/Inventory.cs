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
                //occupationMatrix[nextSlotCoord] = false;
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

        var inventoryItem = CreateInventoryItem();
        inventoryItem.MouseMoving     += InventorySlotOnMouseMoving;
        inventoryItem.WasRightClicked += InventorySlotOnEquippingItem;
        inventoryItem.ItemConsumed    += InventoryItemOnItemConsumed;
        inventoryItem.Init(item, freeSlot.CustomMinimumSize);
        inventoryItem.RootSlot = freeSlot;
        inventoryItem.Position = freeSlot.Position;

        //freeSlot.SetItem(inventoryItem);

        var occupiedSlots = new List<InventorySlot>();

        for (var w = 0; w < inventoryItem.SlotWidth; w++)
        for (var h = 0; h < inventoryItem.SlotHeight; h++)
            occupiedSlots.Add(slotMap[freeSlot.InventoryCoordinate + new Vector2(w, h)]);

        var occupiedSlotsCoordinates = occupiedSlots.Select(slot => slot.InventoryCoordinate).ToArray();
        inventoryItem.OccupiedSlotCoordinates = occupiedSlotsCoordinates;

        foreach (var slotsCoordinate in occupiedSlotsCoordinates)
        {
            slotMap[slotsCoordinate].SetItem(inventoryItem);
            slotMap[slotsCoordinate].IsOccupied = true;
            occupationMatrix[slotsCoordinate]   = true;
        }

        GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<Control>("OverlayLayer").AddChild(inventoryItem);

        return true;
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
            var inventorySlot = SlotScene.Instantiate<InventorySlot>();

            inventorySlot.Inventory           =  this;
            inventorySlot.InventoryCoordinate =  new Vector2(column, row);
            inventorySlot.SlotEmptied         += InventorySlotOnSlotEmptied;
            inventorySlot.WithdrawingItem     += InventorySlotOnWithdrawingItem;

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

    private void InventorySlotOnEquippingItem(InventorySlot fromSlot)
        => EquippingItem?.Invoke(fromSlot);

    private void InventorySlotOnWithdrawingItem(InventoryItem withdrawnitem)
        => MouseObject.Show((BaseItem)withdrawnitem?.ContainedItem);

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
            var adjacentSlotsAreOccupied = false;

            for (var w = 0; w < incomingItem.SlotSize.X; w++)
            {
                for (var h = 0; h < incomingItem.SlotSize.Y; h++)
                {
                    if (h == 0 && w == 0)
                        continue;

                    var nextSlotKey = slotIsOccupied.Key + new Vector2(w, h);

                    if (nextSlotKey == slotIsOccupied.Key)
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

            if (!adjacentSlotsAreOccupied)
                return slotMap[slotIsOccupied.Key];
        }

        return null;
    }
}