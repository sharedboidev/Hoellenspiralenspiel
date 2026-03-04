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

    public void BounceAndFlip()
    {
        var startPos = GlobalPosition;
        var startRot = Rotation;
        var tween    = GetTree().CreateTween();

        tween.SetParallel();
        tween.TweenProperty(this, "position", startPos + Vector2.Up * 50, 0.1f);
        tween.TweenProperty(this, "position", startPos, 0.2f).SetDelay(0.1f);
        tween.TweenProperty(this, "rotation", startRot + Mathf.Tau, 0.2f);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => Rotation = startRot));
    }
}