using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class InventorySlot
        : PanelContainer,
          ITooltipObjectContainer
{
    public delegate void EquippingItemEventHandler(InventorySlot fromSlot);

    public delegate void MouseMovementEventHandler(MousemovementDirection mousemovementDirection, InventorySlot inventorySlot);

    public delegate void SlotEmptiedEventHandler(InventorySlot inventorySlot);

    public delegate void WithdrawingItemEventHandler(BaseItem withdrawnItem);

    private TextureRect                      icon;
    private Label                            stacksizeDisplay;
    public  Inventory                        Inventory          { get; set; }
    public  ITooltipObject                   ContainedItem      { get; set; }
    public  Vector2                          TooltipAnchorPoint => GlobalPosition;
    public event MouseMovementEventHandler   MouseMoving;
    public event WithdrawingItemEventHandler WithdrawingItem;
    public event EquippingItemEventHandler   EquippingItem;
    public event SlotEmptiedEventHandler     SlotEmptied;

    public override void _Ready()
    {
        icon             = GetNode<TextureRect>("%Icon");
        stacksizeDisplay = GetNode<Label>("%StacksizeDisplay");
    }

    public bool SetItem(BaseItem incomingItem)
    {
        if (incomingItem is null || !HasSpaceFor(incomingItem))
            return false;

        if (ContainedItem is ConsumableItem containedConsumable && incomingItem is ConsumableItem incomingConsumable)
        {
            var couldAdd = containedConsumable.TryAddToStack(incomingConsumable.StacksizeCurrent);

            UpdateAndShowStacksize(containedConsumable);

            return couldAdd;
        }

        ContainedItem = incomingItem;
        icon.Texture  = incomingItem.Icon.Texture;

        incomingItem.TreeExited += ItemOnTreeExited;

        if (incomingItem is not ConsumableItem consumable)
            return false;

        UpdateAndShowStacksize(consumable);

        consumable.OnStacksizeReduced += ConsumableOnStacksizeReduced;

        return true;
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

    public bool HasSpaceFor(BaseItem item)
    {
        if (item is ConsumableItem newConsumable && ContainedItem is ConsumableItem containedConsumable)
            return containedConsumable.CanFit(newConsumable);

        return ContainedItem is null;
    }

    private void ItemOnTreeExited()
        => stacksizeDisplay.Visible = false;

    public void _on_item_image_gui_input(InputEvent inputEvent)
    {
        var mouseObject = Inventory.GetNode<MouseObject>(nameof(MouseObject));

        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } when ContainedItem is ConsumableItem consumable:
                ConsumeItem(consumable);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } when ContainedItem is not null:
                EquipItem();

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedItem is not null && !mouseObject.HasItem:
                WithdrawItem();

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedItem is null && mouseObject.HasItem:
                PutItemIntoSlot(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when mouseObject.HasItem && !HasSpaceFor((BaseItem)mouseObject.ContainedItem):
                SwapItems(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when mouseObject.HasItem && HasSpaceFor((BaseItem)mouseObject.ContainedItem):
                MergeItems(mouseObject);

                break;
        }
    }

    private void ConsumeItem(ConsumableItem consumable)
    {
        var player = GetTree().CurrentScene.GetNode<Player2D>("%Player 2D");

        consumable.GetConsumedBy(player);
    }

    private void PutItemIntoSlot(MouseObject mouseObject)
    {
        var item = mouseObject.RetrieveItem();

        SetItem(item);

        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }

    private void WithdrawItem()
    {
        var item = RetrieveItem();
        WithdrawingItem?.Invoke(item);
    }

    private void EquipItem()
    {
        EquippingItem?.Invoke(this);
        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }

    private void SwapItems(MouseObject mouseObject)
    {
        var mousItem = mouseObject.RetrieveItem();
        var slotItem = RetrieveItem();

        SetItem(mousItem);
        mouseObject.Show(slotItem);
        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }

    private void MergeItems(MouseObject mouseObject)
    {
        var item     = mouseObject.RetrieveItem();
        var couldSet = SetItem(item);

        if (!couldSet)
            mouseObject.Show(item);
    }

    public void _on_mouse_entered()
        => MouseMoving?.Invoke(MousemovementDirection.Entered, this);

    public void _on_mouse_exited()
        => MouseMoving?.Invoke(MousemovementDirection.Left, this);
}