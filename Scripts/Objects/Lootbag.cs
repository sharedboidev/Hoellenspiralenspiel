using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.Objects;

public partial class Lootbag : PanelContainer
{
    public delegate void LootClickedEventHandler();

    public List<BaseItem>                ContainedItems { get; set; } = new();
    public event LootClickedEventHandler LootClicked;

    public void _on_gui_input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            LootClicked?.Invoke();
    }
}