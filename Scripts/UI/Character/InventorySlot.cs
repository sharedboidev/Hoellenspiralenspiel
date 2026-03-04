using Godot;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class InventorySlot
        : PanelContainer
{
    public delegate void SlotEmptiedEventHandler(InventorySlot inventorySlot);

    public delegate void WithdrawingItemEventHandler(BaseItem withdrawnItem);

    private Label     stacksizeDisplay;
    public  Inventory Inventory           { get; set; }
    public  Vector2   InventoryCoordinate { get; set; }
    public  bool      IsOccupied          { get; set; }

    public InventoryItem                     ContainedInventoryItem { get; set; }
    public Vector2                           TooltipAnchorPoint     => GlobalPosition;
    public event WithdrawingItemEventHandler WithdrawingItem;
    public event SlotEmptiedEventHandler     SlotEmptied;

    public override void _Ready()
        => stacksizeDisplay = GetNode<Label>("%StacksizeDisplay");

    public void Reset()
    {
        IsOccupied             = false;
        ContainedInventoryItem = null;
    }

    public bool SetItem(InventoryItem incomingItem)
    {
        if (incomingItem?.ContainedItem is null || !HasSpaceFor((BaseItem)incomingItem.ContainedItem))
            return false;

        if (ContainedInventoryItem?.ContainedItem is ConsumableItem containedConsumable && incomingItem.ContainedItem is ConsumableItem incomingConsumable)
        {
            var couldAdd = containedConsumable.TryAddToStack(incomingConsumable.StacksizeCurrent);

            UpdateAndShowStacksize(containedConsumable);

            return couldAdd;
        }

        incomingItem.Init((BaseItem)incomingItem.ContainedItem, CustomMinimumSize);
        incomingItem.TreeExited += ItemOnTreeExited;
        ContainedInventoryItem  =  incomingItem;

        if (incomingItem.ContainedItem is not ConsumableItem consumable)
            return false;

        HandleConsumable(consumable);

        return true;
    }

    private void HandleConsumable(ConsumableItem consumable)
    {
        UpdateAndShowStacksize(consumable);

        consumable.OnStacksizeReduced += ConsumableOnStacksizeReduced;
    }

    private void UpdateAndShowStacksize(ConsumableItem consumable)
    {
        stacksizeDisplay.Text    = $"x{consumable.StacksizeCurrent:N0}";
        stacksizeDisplay.Visible = true;
    }

    private void ConsumableOnStacksizeReduced(int newstacksize, int oldstacksize)
    {
        if (ContainedInventoryItem?.ContainedItem is not ConsumableItem consumable)
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

        if (ContainedInventoryItem?.ContainedItem is BaseItem item)
            item.TreeExited -= ItemOnTreeExited;

        if (ContainedInventoryItem?.ContainedItem is ConsumableItem consumableItem)
            consumableItem.OnStacksizeReduced -= ConsumableOnStacksizeReduced;

        var itemAboutToBeReturned = ContainedInventoryItem?.ContainedItem;
        ContainedInventoryItem?.QueueFree();
        ContainedInventoryItem = null;

        SlotEmptied?.Invoke(this);

        return (BaseItem)itemAboutToBeReturned;
    }

    public bool HasSpaceFor(BaseItem item)
    {
        if (item is ConsumableItem newConsumable && ContainedInventoryItem?.ContainedItem is ConsumableItem containedConsumable)
            return containedConsumable.CanFit(newConsumable);

        return !IsOccupied;
    }

    private void ItemOnTreeExited()
        => stacksizeDisplay.Visible = false;

    public void _on_item_image_gui_input(InputEvent inputEvent)
    {
        var mouseObject = Inventory.GetNode<MouseObject>(nameof(MouseObject));

        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedInventoryItem is not null && !mouseObject.HasItem:
                WithdrawItem();

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedInventoryItem is null && mouseObject.HasItem:
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

    private void PutItemIntoSlot(MouseObject mouseObject)
    {
        var item = mouseObject.RetrieveItem();
        ContainedInventoryItem.ContainedItem = item;

        SetItem(ContainedInventoryItem);

        //MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }

    private void WithdrawItem()
    {
        var item = RetrieveItem();
        WithdrawingItem?.Invoke(item);
    }

    private void SwapItems(MouseObject mouseObject)
    {
        var slotItem = RetrieveItem();

        PutItemIntoSlot(mouseObject);

        mouseObject.Show(slotItem);
    }

    private void MergeItems(MouseObject mouseObject)
    {
        var item = mouseObject.RetrieveItem();
        ContainedInventoryItem.ContainedItem = item;

        var couldSet = SetItem(ContainedInventoryItem);

        if (!couldSet)
            mouseObject.Show(item);
    }
}