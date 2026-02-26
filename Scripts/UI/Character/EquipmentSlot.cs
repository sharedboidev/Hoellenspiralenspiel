using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

[Tool]
public partial class EquipmentSlot : PanelContainer
{
    private          Texture2D      defaultTexture;
    private          BaseItem       equipedItem;
    [Export] private int            pxDimension = 64;
    private          int            slotHeight  = 1;
    private          int            slotWidth   = 1;

    [Export]
    public ItemType FittingItemType { get; private set; }

    [Export]
    public Texture2D DefaultTexture
    {
        get => defaultTexture;
        set
        {
            defaultTexture = value;
            SetDefaultTexture();
        }
    }

    [Export]
    public int SlotWidth
    {
        get => slotWidth;
        set
        {
            slotWidth = value;
            SetScaledSize();
        }
    }

    [Export]
    public int SlotHeight
    {
        get => slotHeight;
        set
        {
            slotHeight = value;
            SetScaledSize();
        }
    }

    public override void _Ready()
        => SetScaledSize();

    private void SetScaledSize()
    {
        var minSizeX = SlotWidth * pxDimension;
        var minSizeY = SlotHeight * pxDimension;
        var newSize  = new Vector2(minSizeX, minSizeY);

        CustomMinimumSize = newSize;
    }

    public BaseItem RetrieveItem()
    {
        var itemToRetrieve = equipedItem;
        equipedItem = null;

        SetDefaultTexture();

        return itemToRetrieve;
    }

    public void EquipItem(BaseItem item)
    {
        equipedItem = item;
        var textureNode = GetNode<TextureRect>("%Icon");
        textureNode.Texture = equipedItem.Icon.Texture;
    }

    private void SetDefaultTexture()
    {
        var textureNode = GetNode<TextureRect>("%Icon");
        var texture     = DefaultTexture ?? GD.Load<Texture2D>("res://icon.svg");

        textureNode.Texture = texture;
    }

    public void _on_texture_rect_gui_input(InputEvent inputEvent)
    {
        var mouseObject = ((EquipmentPanel)Owner).Inventory.GetNode<MouseObject>(nameof(MouseObject));

        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when equipedItem is not null && !mouseObject.HasItem:
                WithdrawItem(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when equipedItem is null && mouseObject.HasItem:
                PutItemIntoSlot(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when mouseObject.HasItem && equipedItem is not null:
                SwapItems(mouseObject);

                break;
        }
    }

    private void SwapItems(MouseObject mouseObject)
    {
        var mousItem = mouseObject.RetrieveItem();
        var slotItem = RetrieveItem();

        EquipItem(mousItem);
        mouseObject.Show(slotItem);
    }

    private void WithdrawItem(MouseObject mouseObject)
    {
        var item = RetrieveItem();

        mouseObject.Show(item);
    }

    private void PutItemIntoSlot(MouseObject mouseObject)
    {
        var item = mouseObject.RetrieveItem();

        EquipItem(item);
    }

    public void _on_texture_rect_mouse_exited()
        => GD.Print($"Mouse exited Slot {Name}");

    public void _on_texture_rect_mouse_entered()
        => GD.Print($"Mouse entered Slot {Name}");
}