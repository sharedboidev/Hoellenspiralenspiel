using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Inventory : PanelContainer
{
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

    public override void _Ready() => BuildInventory();

    public void SetItem(BaseItem item)
    {
        var freeSlot = GetNextFreeSlotOrDefaultFor(item);

        freeSlot?.SetItem(item);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseEvent)
            return;

        var clickedOutside = CheckClickedOutsideInventory(mouseEvent);

        if (clickedOutside)
            MouseObject.DropItem();
    }

    private bool CheckClickedOutsideInventory(InputEventMouseButton mouseEvent)
        => mouseEvent.GlobalPosition.X < GlobalPosition.X
           || mouseEvent.GlobalPosition.Y < GlobalPosition.Y
           || mouseEvent.GlobalPosition.Y > GlobalPosition.Y + Size.Y
           || mouseEvent.GlobalPosition.X > GlobalPosition.X + Size.X;

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

            ItemGrid.AddChild(inventorySlot);
        }

        slotsGenerated = true;
    }

    private void InventorySlotOnWithdrawingItem(BaseItem withdrawnitem) => MouseObject.Show(withdrawnitem);

    private void InventorySlotOnSlotEmptied(InventorySlot inventoryslot) => Tooltip.Hide();

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

            if (incomingItem is ConsumableItem incomingConsumable
                && nextSlot.ContainedItem is ConsumableItem { IsStackable: true } containedConsumable
                && containedConsumable.CanFit(incomingConsumable))
                return nextSlot;
        }

        return null;
    }
}