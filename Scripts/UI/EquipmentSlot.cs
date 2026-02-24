using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

[Tool]
public partial class EquipmentSlot : PanelContainer
{
    [Export] private int pxDimension = 64;
    private          int slotHeight  = 1;
    private          int slotWidth   = 1;

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

    public override void _Ready() => SetScaledSize();

    private void SetScaledSize()
    {
        var minSizeX = SlotWidth * pxDimension;
        var minSizeY = SlotHeight * pxDimension;
        var newSize  = new Vector2(minSizeX, minSizeY);

        CustomMinimumSize = newSize;
    }

    public void _on_mouse_exited() => GD.Print($"Mouse entered Slot {Name}");

    public void _on_mouse_entered() => GD.Print($"Mouse exited Slot {Name}");
}