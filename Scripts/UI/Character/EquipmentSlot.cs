using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

[Tool]
public partial class EquipmentSlot : PanelContainer,
                                     ITooltipObjectContainer
{
    public delegate void MouseMovementEventHandler(MousemovementDirection mousemovementDirection, EquipmentSlot equipmentSlot);

    private          Texture2D defaultTexture;
    [Export] private int       pxDimension = 64;
    private          int       slotHeight  = 1;
    private          int       slotWidth   = 1;
    public           bool      IsEmpty => ContainedItem is null;

    [Export]
    public ItemSlot FittingItemSlot { get; private set; }

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

    public event MouseMovementEventHandler MouseMoving;

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
        var itemToRetrieve = ContainedItem;
        ContainedItem = null;

        SetDefaultTexture();

        return (BaseItem)itemToRetrieve;
    }

    public void EquipItem(BaseItem item)
    {
        ContainedItem = item;
        var textureNode = GetNode<TextureRect>("%Icon");
        textureNode.Texture = ((BaseItem)ContainedItem).Icon.Texture;
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
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedItem is not null && !mouseObject.HasItem:
                WithdrawItem(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when ContainedItem is null && mouseObject.HasItem && ((BaseItem)mouseObject.ContainedItem).ItemSlot == FittingItemSlot:
                PutItemIntoSlot(mouseObject);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } when mouseObject.HasItem && ContainedItem is not null && ((BaseItem)mouseObject.ContainedItem).ItemSlot == FittingItemSlot:
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
        
        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
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

        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }

    public void _on_texture_rect_mouse_exited()
        => MouseMoving?.Invoke(MousemovementDirection.Left, this);

    public void _on_texture_rect_mouse_entered()
        => MouseMoving?.Invoke(MousemovementDirection.Entered, this);

    public ITooltipObject ContainedItem      { get; set; }
    public Vector2        TooltipAnchorPoint => GlobalPosition;
}