using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

public readonly record struct SkillAim(Vector3 Point, BaseUnit Target = null)
{
    public bool HasTarget => GodotObject.IsInstanceValid(Target) && Target.IsTargetable;

    public Vector3 CurrentPoint => HasTarget ? Target.GlobalPosition : Point;
}
