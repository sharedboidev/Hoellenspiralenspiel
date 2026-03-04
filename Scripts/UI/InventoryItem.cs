using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

[Tool]
public partial class InventoryItem
        : PanelContainer,
          ITooltipObjectContainer,
          INotifyPropertyChanged
{
    public delegate void MouseMovementEventHandler(MousemovementDirection mousemovementDirection, InventoryItem inventoryItem);

    public delegate void WasRightClickedEventHandler(InventorySlot fromRootSlot);
    public delegate void ItemConsumedEventHandler(InventorySlot    fromRootSlot);

    public event WasRightClickedEventHandler WasRightClicked;

    private          Texture2D   defaultTexture;
    private          TextureRect icon;
    [Export] private int         pxDimension = 64;
    private          int         slotHeight  = 1;
    private          int         slotWidth   = 1;

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

    public InventorySlot                     RootSlot                { get; set; }
    public Vector2[]                         OccupiedSlotCoordinates { get; set; } = [];
    public event PropertyChangedEventHandler PropertyChanged;
    public ITooltipObject                    ContainedItem      { get; set; }
    public Vector2                           TooltipAnchorPoint => GlobalPosition;

    public event MouseMovementEventHandler MouseMoving;
    public event ItemConsumedEventHandler  ItemConsumed;

    public override void _Ready()
        => SetScaledSize();

    public void Init(BaseItem item, Vector2 inventorySlotSize)
    {
        icon          = GetNode<TextureRect>("%Icon");
        icon.Texture  = item.Icon.Texture;
        ContainedItem = item;
        pxDimension   = (int)(inventorySlotSize.X + inventorySlotSize.Y) / 2;
        SlotWidth     = (int)item.SlotSize.X;
        SlotHeight    = (int)item.SlotSize.Y;

        SetScaledSize();
    }

    private void SetScaledSize()
    {
        var minSizeX = SlotWidth * pxDimension;
        var minSizeY = SlotHeight * pxDimension;
        var newSize  = new Vector2(minSizeX, minSizeY);

        CustomMinimumSize = newSize;
    }

    private void SetDefaultTexture()
    {
        var textureNode = GetNode<TextureRect>("%Icon");
        var texture     = DefaultTexture ?? GD.Load<Texture2D>("res://icon.svg");

        textureNode.Texture = texture;
    }

    public void _on_gui_input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } when ContainedItem is ConsumableItem consumable:
                ConsumeItem(consumable);

                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } when ContainedItem is not null:
                EquipItem();

                break;
        }
    }

    private void ConsumeItem(ConsumableItem consumable)
    {
        var player = GetTree().CurrentScene.GetNode<Player2D>("%Player 2D");

        consumable.GetConsumedBy(player);

        ItemConsumed?.Invoke(RootSlot);
    }

    private void EquipItem()
    {
        WasRightClicked?.Invoke(RootSlot);
        MouseMoving?.Invoke(MousemovementDirection.Entered, this);
    }
    
    public void _on_mouse_entered()
        => MouseMoving?.Invoke(MousemovementDirection.Entered, this);

    public void _on_mouse_exited()
        => MouseMoving?.Invoke(MousemovementDirection.Left, this);
}