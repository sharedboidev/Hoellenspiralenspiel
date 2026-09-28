using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

//Wohin ein Skill zielt: auf einen Punkt in der Welt und, falls dort eine Einheit steht, auf diese Einheit
public readonly record struct SkillAim(Vector2 Point, BaseUnit Target = null)
{
    public bool HasTarget => GodotObject.IsInstanceValid(Target) && Target.IsTargetable;

    //Die Einheit bewegt sich, deshalb zählt ihre Position im Augenblick der Abfrage
    public Vector2 CurrentPoint => HasTarget ? Target.BodyCenter : Point;
}
