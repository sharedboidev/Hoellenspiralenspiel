using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class CharacterSheet : Control
{
    [Export]
    public int ViewportMarginWidthPx { get; set; }

    [Export]
    public int ViewportMarginHeightPx { get; set; }

    public override void _Ready() => SetPositionRelativeToViewport();

    private void SetPositionRelativeToViewport()
    {
        var viewportSize  = GetViewportRect().Size;
        var sheetsize     = GetNode<PanelContainer>("%EquipmentPanel").Size;
        var sheetPosition = new Vector2(viewportSize.X - sheetsize.X - ViewportMarginWidthPx, ViewportMarginHeightPx);

        Position = sheetPosition;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("B"))
            ToggleVisibility();
    }

    private void ToggleVisibility() => Visible = !Visible;

    // foreach (var inventorySlot in ItemGrid.GetAllChildren<InventorySlot>())
    //     inventorySlot.SetVisible(!inventorySlot.IsVisible());
    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}