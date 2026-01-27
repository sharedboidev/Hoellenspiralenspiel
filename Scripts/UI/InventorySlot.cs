using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class InventorySlot : PanelContainer,
                                     ITooltipObjectContainer
{
    public delegate void MouseMovementEventHandler(MousemovementDirection mousemovementDirection, InventorySlot inventorySlot);
    public delegate void SlotEmptiedEventHandler(InventorySlot inventorySlot);

    private TextureRect                    icon;
    private Label                          stacksizeDisplay;
    public  bool                           HasSpace           => ContainedItem is null or ConsumableItem { IsFull: false };
    public  ITooltipObject                 ContainedItem      { get; set; }
    public  Vector2                        TooltipAnchorPoint => GlobalPosition;
    public event MouseMovementEventHandler MouseMoving;
    public event SlotEmptiedEventHandler   SlotEmptied;

    public override void _Ready()
    {
        icon             = GetNode<TextureRect>("%Icon");
        stacksizeDisplay = GetNode<Label>("%StacksizeDisplay");
    }

    public void SetItem(BaseItem item)
    {
        if (!HasSpace)
            return;

        if (ContainedItem is ConsumableItem containedConsumable && item is ConsumableItem consumableItem)
        {
            containedConsumable.TryAddToStack(consumableItem.StacksizeCurrent);

            UpdateAndShowStacksize(containedConsumable);
        }
        else
        {
            ContainedItem = item;
            icon.Texture  = item.Icon.Texture;

            item.TreeExited += ItemOnTreeExited;

            if (item is not ConsumableItem consumable)
                return;

            UpdateAndShowStacksize(consumable);

            consumable.OnStacksizeReduced += ConsumableOnStacksizeReduced;
        }
    }

    private void UpdateAndShowStacksize(ConsumableItem consumable)
    {
        stacksizeDisplay.Text    = $"x{consumable.StacksizeCurrent:N0}";
        stacksizeDisplay.Visible = true;
    }

    private void ConsumableOnStacksizeReduced(int newstacksize, int oldstacksize)
    {
        if (ContainedItem is not ConsumableItem consumable)
            return;

        if (newstacksize == 0)
        {
            var itemToBeFreed = RetrieveItem();
            itemToBeFreed.QueueFree();

            return;
        }

        UpdateAndShowStacksize(consumable);
    }

    public BaseItem RetrieveItem()
    {
        stacksizeDisplay.Visible = false;
        icon.Texture             = null;

        ((BaseItem)ContainedItem).TreeExited -= ItemOnTreeExited;

        if (ContainedItem is ConsumableItem consumableItem)
            consumableItem.OnStacksizeReduced -= ConsumableOnStacksizeReduced;

        var itemAboutToBeReturned = ContainedItem;
        ContainedItem = null;

        SlotEmptied?.Invoke(this);

        return (BaseItem)itemAboutToBeReturned;
    }

    private void ItemOnTreeExited() => stacksizeDisplay.Visible = false;

    public void _on_item_image_gui_input(InputEvent inputEvent)
    {
        if (ContainedItem is null)
            return;

        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } &&
            ContainedItem is ConsumableItem consumable)
        {
            var player = GetTree().CurrentScene.GetNode<Player2D>("Player 2D");

            consumable.GetConsumedBy(player);
        }
    }

    public void _on_mouse_entered() => MouseMoving?.Invoke(MousemovementDirection.Entered, this);

    public void _on_mouse_exited() => MouseMoving?.Invoke(MousemovementDirection.Left, this);
}