using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items.Consumables;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Inventory : PanelContainer
{
    private GridContainer itemGrid;

    [Export]
    public int AmountSlots { get; set; } = 30;

    private bool slotsGenerated;

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
        if(slotsGenerated)
            return;

        var slotScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/inventory_slot.tscn");

        for (var i = 0; i < AmountSlots; i++)
        {
            var instance = slotScene.Instantiate<InventorySlot>();
            ItemGrid.AddChild(instance);
        }

        slotsGenerated = true;
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

            var potionInstance = healthPotionScene.Instantiate<HealthPotion>();
            var rng            = new Random();
            potionInstance.StacksizeCurrent = rng.Next(1, 6);

            var freeSlot = ItemGrid.GetAllChildren<InventorySlot>()
                                   .FirstOrDefault(slot => slot.HasSpace);

            if (freeSlot is null)
                return;

            freeSlot.SetItem(potionInstance);
        }
    }
}