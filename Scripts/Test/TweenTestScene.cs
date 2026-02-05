using Godot;

namespace Hoellenspiralenspiel.Scripts.Test;

public partial class TweenTestScene : PanelContainer
{
    public override void _Ready()
    {
        var tween = GetTree().CreateTween()
                             .SetParallel();

        tween.TweenProperty(this, "position", Position + Vector2.Right * 1000, 2);
        tween.TweenProperty(this, "scale", Scale * 3f, 2);
        tween.TweenProperty(this, "rotation_degrees", 1080, 1f);

        base._Ready();
    }
}