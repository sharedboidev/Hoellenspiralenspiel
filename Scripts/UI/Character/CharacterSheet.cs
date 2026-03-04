using Godot;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class CharacterSheet : Control
{
    [Export] private EquipmentPanel equipmentPanel;
    [Export] private Inventory      inventory;
    [Export] private Player2D       player;
    private          Statdisplay    statdisplay;
    [Export] private int            viewportMarginHeightPx;
    [Export] private int            viewportMarginWidthPx;

    public override void _Ready()
    {
        SetPositionRelativeToViewport();

        statdisplay = GetNode<Statdisplay>(nameof(Statdisplay));
        statdisplay.Render(player);

        inventory.EquippingItem += OnEquippingItem;

        GetNode<EquipmentPanel>("%" + nameof(EquipmentPanel)).EquipmentChanged += OnEquipmentChanged;
        GetNode<StatdisplayButton>(nameof(StatdisplayButton)).Pressed          += OnPressed;

        SetVisible(false);
    }

    private void OnPressed(bool isToggledOpen)
        => statdisplay.Visible = isToggledOpen;

    private void OnEquipmentChanged(object formerlyEqipped, object newlyEquipped)
    {
        if (player is null)
            return;

        if (formerlyEqipped is BaseItem item)
            player.UnequipItem(item);

        RerenderStatdisplay();
    }

    private void RerenderStatdisplay()
        => statdisplay.Render(player);

    private void OnEquippingItem(InventorySlot fromslot)
    {
        if (fromslot.ContainedInventoryItem?.ContainedItem is not BaseItem item)
            return;

        if (!item.CanBeEquipedBy(player))
            return;

        var retrievedItem        = inventory.RetrieveItem(fromslot);
        var formerlyEquippedItem = equipmentPanel.EquipIntoFittingSlot(retrievedItem);
        
        if(formerlyEquippedItem is null)
            return;

        var inventoryItem = inventory.CreateInventoryItem();
        inventoryItem.ContainedItem = formerlyEquippedItem;

        fromslot.SetItem(inventoryItem);
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

    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}