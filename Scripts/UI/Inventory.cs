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
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseEvent)
        {
            var clickedOutside = mouseEvent.GlobalPosition.X < GlobalPosition.X
                                 || mouseEvent.GlobalPosition.Y < GlobalPosition.Y
                                 || mouseEvent.GlobalPosition.Y > GlobalPosition.Y + Size.Y
                                 || mouseEvent.GlobalPosition.X > GlobalPosition.X + Size.X;

            GD.Print($"Clicked outside Invetory?: {clickedOutside}");
        }
    }

    private void BuildInventory()
    {
        if (slotsGenerated)
            return;

        var slotScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/inventory_slot.tscn");

        for (var i = 0; i < AmountSlots; i++)
        {
            var inventorySlot = slotScene.Instantiate<InventorySlot>();

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

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("B"))
            Visible = !Visible;
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