using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Inventory : PanelContainer
{
    private GridContainer itemGrid;
    private bool          slotsGenerated;
    private BaseTooltip   Tooltip => GetTree().CurrentScene.GetNode<AbilityTooltip>("%" + nameof(AbilityTooltip));

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
            var instance = slotScene.Instantiate<InventorySlot>();
            ItemGrid.AddChild(instance);

            instance.SlotEmptied += InstanceOnSlotEmptied;
            instance.MouseMoving += InstanceOnMouseMoving;
        }

        slotsGenerated = true;
    }

    private void InstanceOnSlotEmptied(InventorySlot inventoryslot) => Tooltip.Hide();

    private void InstanceOnMouseMoving(MousemovementDirection mousemovementdirection, InventorySlot inventoryslot)
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
            var healthPotionScene = ResourceLoader.Load<PackedScene>("res://Scenes/Items/Consumables/health_potion.tscn");

            var rng = new Random();

            var potionInstance = healthPotionScene.Instantiate<HealthPotion>();
            potionInstance.StacksizeCurrent = rng.Next(1, 6);

            var freeSlots = ItemGrid.GetAllChildren<InventorySlot>()
                                    .Where(slot => slot.HasSpace)
                                    .ToList();

            if (!freeSlots.Any())
                return;

            foreach (var freeSlot in freeSlots)
            {
                if (freeSlot.ContainedItem is not null && potionInstance.StacksizeCurrent > potionInstance.StacksizeMax - ((ConsumableItem)freeSlot.ContainedItem).StacksizeCurrent)
                    continue;

                freeSlot.SetItem(potionInstance);
                return;
            }
        }
    }
}