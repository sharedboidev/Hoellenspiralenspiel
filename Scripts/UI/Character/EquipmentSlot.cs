using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

[Tool]
public partial class EquipmentSlot
        : PanelContainer,
          ITooltipObjectContainer
{
    public delegate void HoverChangedEventHandler(EquipmentSlot slot, bool isHovered);

    //Rand und Rahmen aus equipment_slot.tscn, 2 + 2 px pro Seite. Muss dazu passen, sonst wächst der Slot mit dem Item
    private const int FramePx = 8;

    private          Texture2D      defaultTexture;
    private          CharacterItems items;
    [Export] private int            pxDimension = 64;
    private          int            slotHeight  = 1;
    private          int            slotWidth   = 1;

    [Export]
    public Array<ItemSlot> FittingItemSlot { get; private set; }

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

    public ItemSlot Place => FittingItemSlot is { Count: > 0 } ? Equipment.GetPlaceFor(FittingItemSlot[0]) : ItemSlot.Undefined;

    public ItemInstance Item => items?.Equipment.Get(Place);

    public bool IsEmpty => Item is null;

    public ITooltipObject ContainedItem      => IsEmpty ? null : new ItemTooltipContent(Item, items.GetUnmetRequirements(Item));
    public Vector2        TooltipAnchorPoint => GlobalPosition;

    public event HoverChangedEventHandler HoverChanged;

    public override void _Ready()
        => SetScaledSize();

    public void Bind(CharacterItems owner)
        => items = owner;

    public void Refresh()
    {
        var item     = Item;
        var itemIcon = GetNode<TextureRect>("%ItemIcon");

        if (item is null)
        {
            SetDefaultTexture();
            itemIcon.Texture = null;
            return;
        }

        var (width, height) = EquippedIconSize.Get(item.Definition, SlotWidth, SlotHeight, pxDimension, FramePx);

        GetNode<TextureRect>("%Icon").Texture = null;
        itemIcon.Texture                      = ItemLibrary.GetIcon(item);
        itemIcon.CustomMinimumSize            = new Vector2(width, height);
    }

    private void SetScaledSize()
        => CustomMinimumSize = new Vector2(SlotWidth, SlotHeight) * pxDimension;

    private void SetDefaultTexture()
    {
        var textureNode = GetNode<TextureRect>("%Icon");
        var texture     = DefaultTexture ?? GD.Load<Texture2D>("res://icon.svg");

        textureNode.Texture = texture;
    }

    public void _on_texture_rect_gui_input(InputEvent inputEvent)
    {
        if (items is null || inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;

        if (items.HeldItem is null)
            items.TakeFromEquipment(Place);
        else
            items.PlaceHeldInEquipment(Place);
    }

    public void _on_texture_rect_mouse_exited()
        => HoverChanged?.Invoke(this, false);

    public void _on_texture_rect_mouse_entered()
        => HoverChanged?.Invoke(this, true);
}
