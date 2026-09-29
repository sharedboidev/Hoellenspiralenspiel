using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public readonly record struct Aim3D(Vector3 Point, Unit3D Target = null)
{
    public bool HasTarget => GodotObject.IsInstanceValid(Target) && Target.IsTargetable;

    public Vector3 CurrentPoint => HasTarget ? Target.GlobalPosition : Point;
}
