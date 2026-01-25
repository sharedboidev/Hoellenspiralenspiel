using Godot;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class FireballSkill : BaseSkill
{
    public FireballSkill(Key triggerKey)
            : base(triggerKey,"res://Scenes/Spells/fireball.tscn") 
    { }
}