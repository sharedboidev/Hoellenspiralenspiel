using Godot;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.Objects;

public partial class Lootbag : PanelContainer
{
    public delegate void LootClickedEventHandler(Lootbag sender, BaseItem lootedItem);

    public BaseItem                      ContainedItem { get; set; }
    public event LootClickedEventHandler LootClicked;

    public void _on_gui_input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            LootClicked?.Invoke(this, ContainedItem);
    }
}