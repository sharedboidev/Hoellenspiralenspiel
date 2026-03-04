using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Character;

namespace Hoellenspiralenspiel.Scripts.UI;

[Tool]
public partial class InventoryItem : PanelContainer,
                                     ITooltipObjectContainer,
                                     INotifyPropertyChanged
{
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
}