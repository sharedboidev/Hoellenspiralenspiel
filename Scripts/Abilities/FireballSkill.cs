using Godot;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class FireballSkill : BaseSkill
{
    private readonly double cooldown = 0.25d;

    public FireballSkill(Key triggerKey)
            : base(triggerKey, "res://Scenes/Spells/fireball.tscn") => SkillBarIcon.Cooldown = cooldown;
}