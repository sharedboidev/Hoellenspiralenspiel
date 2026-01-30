using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Units;

public class FireballContainer { }

public partial class Player2D : BaseUnit
{
	private readonly List<BaseSkill> skills = new();

	[Export] public HBoxContainer SkillBar;
	private         PackedScene   SkillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
	private         AnimationTree AnimationTree { get; set; }

	public override void _Ready()
	{
		base._Ready();

		skills.Add(new FireballSkill(this));

		var fireballActionBarItem = SkillBarIcon.Instantiate<CooldownSkill>();
		fireballActionBarItem.Init(skills.First(), visualResourceName: "res://Scenes/Spells/fireball.tscn");
		SkillBar.AddChild(fireballActionBarItem);
		
		AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
		Movementspeed = 300;
	}

	public bool IsInAggroRangeOf(BaseEnemy enemy)
	{
		var distanceToEnemy = Math.Sqrt(GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition));

		return distanceToEnemy <= enemy.AggroRange;
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
	}
}
