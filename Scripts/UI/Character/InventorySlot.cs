using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class InventorySlot : PanelContainer
{
    public delegate void ClickedEventHandler(InventorySlot slot);

    public GridCell Cell { get; set; }

    public event ClickedEventHandler Clicked;

    public void _on_item_image_gui_input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            Clicked?.Invoke(this);
    }
}
