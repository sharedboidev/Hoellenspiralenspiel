using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Objects;

public partial class Lootbag : PanelContainer
{
    private static readonly PackedScene Scene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");

    private CharacterItems collector;

    public ItemInstance ContainedItem { get; private set; }

    public static Lootbag Drop(Node parent, Vector2 globalPosition, ItemInstance item, CharacterItems collector)
    {
        var lootbag = Scene.Instantiate<Lootbag>();

        lootbag.ContainedItem  = item;
        lootbag.collector      = collector;
        lootbag.GlobalPosition = globalPosition;

        parent.AddChild(lootbag);

        return lootbag;
    }

    public void _on_gui_input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            Collect();
    }

    public void Collect()
    {
        if (collector.PickUp(ContainedItem))
            QueueFree();
        else
            BounceAndFlip();
    }

    public void BounceAndFlip()
    {
        var startPos = GlobalPosition;
        var startRot = Rotation;
        //Der Tween hängt am Beutel und endet mit ihm, sonst liefe er nach dem Aufheben ins Leere
        var tween = CreateTween();

        tween.SetParallel();
        tween.TweenProperty(this, "position", startPos + Vector2.Up * 50, 0.1f);
        tween.TweenProperty(this, "position", startPos, 0.2f).SetDelay(0.1f);
        tween.TweenProperty(this, "rotation", startRot + Mathf.Tau, 0.2f);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => Rotation = startRot));
    }
}
