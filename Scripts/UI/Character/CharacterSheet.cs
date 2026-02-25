using Godot;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class CharacterSheet : Control
{
    [Export] private PanelContainer equipmentPanel;
    [Export] private Player2D       player;
    [Export] private int            viewportMarginHeightPx;
    [Export] private int            viewportMarginWidthPx;

    public override void _Ready()
    {
        SetPositionRelativeToViewport();

        GetNode<Inventory>("%" + nameof(Inventory)).EquippingItem += OnEquippingItem;
    }

    private void OnEquippingItem(InventorySlot fromslot)
    {
        if (fromslot.ContainedItem is not BaseItem item)
            return;

        if (!item.CanBeEquipedBy(player))
            return;

        var retrievedItem = fromslot.RetrieveItem();
    }

    private void SetPositionRelativeToViewport()
    {
        var viewportSize  = GetViewportRect().Size;
        var sheetsize     = equipmentPanel.Size;
        var sheetPosition = new Vector2(viewportSize.X - sheetsize.X - viewportMarginWidthPx, viewportMarginHeightPx);

        Position = sheetPosition;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("B"))
            ToggleVisibility();
    }

    private void ToggleVisibility()
        => Visible = !Visible;

    // foreach (var inventorySlot in ItemGrid.GetAllChildren<InventorySlot>())
    //     inventorySlot.SetVisible(!inventorySlot.IsVisible());
    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}