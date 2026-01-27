using Godot;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public abstract class BaseSkill
{
    private readonly PackedScene scene;
    private readonly Key         triggerKey;

    public BaseSkill(Key    triggerKey,
                     string skillSceneResourceName)
    {
        this.triggerKey = triggerKey;
        scene           = ResourceLoader.Load<PackedScene>(skillSceneResourceName);
        SkillBarIcon    = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn").Instantiate<UI.CooldownSkill>();
    }

    public UI.CooldownSkill SkillBarIcon { get; }

    public bool CanUse()
        => !SkillBarIcon.OnCooldown;

    public T CreateVisual<T>()
            where T : Node
        => (T)scene.Instantiate();

    public bool TriggeredBy(Key key)
        => triggerKey == key;
}