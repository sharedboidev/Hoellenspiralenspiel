using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Inventory : PanelContainer
{
    public delegate void EquippingItemEventHandler(InventorySlot fromSlot);

    private GridContainer itemGrid;
    private bool          slotsGenerated;
    private BaseTooltip   Tooltip     => GetTree().CurrentScene.GetNode<ItemTooltip>("%" + nameof(ItemTooltip));
    private MouseObject   MouseObject => GetNode<MouseObject>(nameof(MouseObject));

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
        => BuildInventory();

    public void SetItem(BaseItem item)
    {
        var freeSlot = GetNextFreeSlotOrDefaultFor(item);

        freeSlot?.SetItem(item);
    }

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

        for (var i = 0; i < AmountSlots; i++)
        {
            var inventorySlot = SlotScene.Instantiate<InventorySlot>();

            inventorySlot.Inventory       =  this;
            inventorySlot.SlotEmptied     += InventorySlotOnSlotEmptied;
            inventorySlot.MouseMoving     += InventorySlotOnMouseMoving;
            inventorySlot.WithdrawingItem += InventorySlotOnWithdrawingItem;
            inventorySlot.EquippingItem   += InventorySlotOnEquippingItem;

            ItemGrid.AddChild(inventorySlot);
        }

        slotsGenerated = true;
    }

    private void InventorySlotOnEquippingItem(InventorySlot fromSlot)
        => EquippingItem?.Invoke(fromSlot);

    private void InventorySlotOnWithdrawingItem(BaseItem withdrawnitem)
        => MouseObject.Show(withdrawnitem);

    private void InventorySlotOnSlotEmptied(InventorySlot inventoryslot)
        => Tooltip.Hide();

    private void InventorySlotOnMouseMoving(MousemovementDirection mousemovementdirection, InventorySlot inventoryslot)
    {
        switch (mousemovementdirection)
        {
            case MousemovementDirection.Entered:
                if (inventoryslot.ContainedItem == null)
                    return;

                Tooltip.Show(inventoryslot);

                break;
            case MousemovementDirection.Left:
                if (inventoryslot.ContainedItem == null)

                    return;

                Tooltip.Hide();

                break;
        }
    }

    public InventorySlot GetNextFreeSlotOrDefaultFor(BaseItem incomingItem)
    {
        var slotsWithSpace = ItemGrid.GetAllChildren<InventorySlot>()
                                     .Where(slot => slot.HasSpaceFor(incomingItem))
                                     .ToList();

        foreach (var nextSlot in slotsWithSpace)
        {
            if (nextSlot.ContainedItem is null)
                return nextSlot;

            if (incomingItem is ConsumableItem incomingConsumable && nextSlot.ContainedItem is ConsumableItem { IsStackable: true } containedConsumable && containedConsumable.CanFit(incomingConsumable))
                return nextSlot;
        }

        return null;
    }
}