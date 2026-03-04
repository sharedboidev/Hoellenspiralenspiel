using Godot;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Objects;
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

        if (formerlyEquippedItem is null)
            return;

        var couldSetItem = inventory.SetItem(formerlyEquippedItem);

        if (!couldSetItem)
            DropItem(formerlyEquippedItem);
    }

    public void DropItem(BaseItem item)
    {
        if (item is null)
            return;

        var playerPosition = GetTree().CurrentScene.GetNode<Player2D>("%Player 2D").GlobalPosition;
        var dropAtPosition = playerPosition;

        InstantiateLootbag(dropAtPosition, item);

        GD.Print($"{item?.Name ?? "Nothing"} dropped by Player.");
    }

    private void InstantiateLootbag(Vector2 atPosition, BaseItem loot)
    {
        var lootbagInstance = GD.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn").Instantiate<Lootbag>();
        lootbagInstance.GlobalPosition =  atPosition;
        lootbagInstance.ContainedItem  =  loot;
        lootbagInstance.LootClicked    += LootbagInstanceOnLootClicked;

        GetTree().CurrentScene.GetNode<Node2D>("Environment").AddChild(lootbagInstance);
    }

    private void LootbagInstanceOnLootClicked(Lootbag sender, BaseItem lootedItem)
    {
        GD.Print($"{lootedItem?.Name ?? "Nothing"} looted.");

        var couldLootItem = inventory.SetItem(lootedItem);

        if (couldLootItem)
            sender?.QueueFree();
        else
            sender.BounceAndFlip();
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