using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.Items.Weapons.Staffs;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Inventory : PanelContainer
{
    private GridContainer itemGrid;
    private bool          slotsGenerated;
    private BaseTooltip   Tooltip     => GetTree().CurrentScene.GetNode<AbilityTooltip>("%" + nameof(AbilityTooltip));
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
        if (Input.IsActionJustPressed("Tab"))
        {
            BuildInventory();

            Visible = !Visible;
        }
        else if (Input.IsActionJustPressed("+"))
        {
            var rng      = new Random();
            var myNumber = rng.Next(1, 6);

            if (myNumber % 2 == 0)
            {
                var healthPotionScene = ResourceLoader.Load<PackedScene>("res://Scenes/Items/Consumables/health_potion.tscn");

                var potionInstance = healthPotionScene.Instantiate<HealthPotion>();
                potionInstance.StacksizeCurrent = myNumber;

                var freeSlot = GetNextFreeSlotOrDefaultFor(potionInstance);
                freeSlot?.SetItem(potionInstance);
            }
            else
            {
                var staffScene    = ResourceLoader.Load<PackedScene>("res://Scenes/Items/Weapons/Staffs/staff.tscn");
                var staffInstance = staffScene.Instantiate<Staff>();

                var freeSlot = GetNextFreeSlotOrDefaultFor(staffInstance);
                freeSlot?.SetItem(staffInstance);
            }
        }
    }

    public InventorySlot GetNextFreeSlotOrDefaultFor(BaseItem incomingItem)
    {
        var slotsWithSpace = ItemGrid.GetAllChildren<InventorySlot>()
                                     .Where(slot => slot.HasSpace)
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