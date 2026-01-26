using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Inventory : PanelContainer
{
    private GridContainer itemGrid;

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

    public override void _Ready()
    {
        var slotScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/inventory_slot.tscn");

        for (int i = 0; i < AmountSlots; i++)
        {
            var instance = slotScene.Instantiate<InventorySlot>();
            ItemGrid.AddChild(instance);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Tab"))
        {
            Visible = !Visible;
        }
    }
}