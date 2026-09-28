using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

public readonly record struct SkillAim(Vector2 Point, BaseUnit Target = null)
{
    public bool HasTarget => GodotObject.IsInstanceValid(Target) && Target.IsTargetable;

    public Vector2 CurrentPoint => HasTarget ? Target.BodyCenter : Point;
}
