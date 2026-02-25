using Godot;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

[Tool]
public partial class EquipmentSlot : PanelContainer
{
    private          Texture2D defaultTexture;
    [Export] private int       pxDimension = 64;
    private          int       slotHeight  = 1;
    private          int       slotWidth   = 1;

    [Export]
    public ItemType FittingItemType { get; private set; }

    [Export]
    public Texture2D DefaultTexture
    {
        get => defaultTexture;
        set
        {
            defaultTexture = value;
            SetSlotTexture();
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

    private void SetSlotTexture()
    {
        var textureNode = GetNode<TextureRect>("%Icon");
        var texture     = DefaultTexture ?? GD.Load<Texture2D>("res://icon.svg");

        textureNode.Texture = texture;
    }

    public void _on_texture_rect_gui_input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseEvent)
            return;

        GD.Print($"Clicked Slot {Name}");
    }

    public void _on_texture_rect_mouse_exited()
        => GD.Print($"Mouse exited Slot {Name}");

    public void _on_texture_rect_mouse_entered()
        => GD.Print($"Mouse entered Slot {Name}");
}