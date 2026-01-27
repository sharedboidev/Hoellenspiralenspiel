using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : BaseUnit
{
    private readonly List<BaseSkill> skills = new()
    {
        new FireballSkill(Key.F)
    };
    [Export] public HBoxContainer SkillBar;
    private         AnimationTree AnimationTree { get; set; }

    public override void _Ready()
    {
        base._Ready();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        Movementspeed = 300;

        foreach (var skill in skills)
            SkillBar.AddChild(skill.SkillBarIcon);
    }

    public override void _PhysicsProcess(double delta)
    {
        MovementDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity          = MovementDirection * Movementspeed;

        if (MovementDirection != Vector2.Zero)
        {
            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
        }

        MoveAndSlide();

        if (Input.IsActionJustPressed("F"))
        {
            var spell = skills.FirstOrDefault(s => s.TriggeredBy(Key.F));

            if (spell is null)
                return;

            if (!spell.CanUse())
                return;

            var node = spell.CreateVisual<Fireball>();
            GetTree().CurrentScene.GetNode<Node2D>("Environment").AddChild(node);
            node.Init(GlobalPosition, GetGlobalMousePosition());
            spell.SkillBarIcon.Use();
        }
    }
}