using Godot;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class InventorySlot : PanelContainer
{
    private Label    stacksizeDisplay;
    public  BaseItem Item     { get; private set; }
    public  bool     HasSpace => Item is null or ConsumableItem { IsFull: false };

    public override void _Ready() => stacksizeDisplay = GetNode<Label>("%StacksizeDisplay");

    public void SetItem(BaseItem item)
    {
        if (!HasSpace)
            return;

        if (Item is ConsumableItem containedConsumable && item is ConsumableItem consumableItem)
            containedConsumable.TryAddToStack(consumableItem.StacksizeCurrent);
        else
        {
            Item = item;
            //Image.Texture = item.Icon.Texture;

            Item.TreeExited += ItemOnTreeExited;

            if (Item is not ConsumableItem consumable)
                return;

            UpdateAndShowStacksize(consumable);

            consumable.OnStacksizeReduced += ConsumableOnStacksizeReduced;
        }
    }

    private void UpdateAndShowStacksize(ConsumableItem consumable)
    {
        stacksizeDisplay.Text    = $"x{consumable.StacksizeCurrent:N}";
        stacksizeDisplay.Visible = true;
    }

    private void ConsumableOnStacksizeReduced(int newstacksize, int oldstacksize)
    {
        if (Item is not ConsumableItem consumable)
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
        //Image.Texture            = null;

        Item.TreeExited -= ItemOnTreeExited;

        if (Item is ConsumableItem consumableItem)
            consumableItem.OnStacksizeReduced -= ConsumableOnStacksizeReduced;

        var itemAboutToBeReturned = Item;
        Item = null;

        return itemAboutToBeReturned;
    }

    private void ItemOnTreeExited() => stacksizeDisplay.Visible = false;

    public void _on_item_image_gui_input(InputEvent inputEvent)
    {
        if (Item is null)
            return;

        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } &&
            Item is ConsumableItem consumable)
        {
            var player = GetTree().CurrentScene.GetNode<Player2D>("Player 2D");

            consumable.GetConsumedBy(player);
        }
    }
}