using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Units;

public class FireballContainer { }

public partial class Player2D : BaseUnit
{
	private readonly List<BaseSkill> skills = new();
	[Export] private RessourceOrb    LifeOrb;
	[Export] private RessourceOrb    ManaOrb;

	[Export] public HBoxContainer SkillBar;
	private         PackedScene   SkillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
	private         AnimationTree AnimationTree { get; set; }

	[Export]
	public AudioStreamPlayer2D NoManaSound { get; set; }

	public float ManaCurrent { get; set; }

	[Export]
	public float ManaMax { get; set; } = 100;

	public override void _Ready()
	{
		ManaCurrent = ManaMax;
		ManaOrb.Init(ManaMax, RessourceType.Mana);
		LifeOrb.Init(LifeMaximum, RessourceType.Life);

		base._Ready();

		skills.Add(new FireballSkill(this));

		var fireballActionBarItem = SkillBarIcon.Instantiate<CooldownSkill>();
		fireballActionBarItem.Init(skills.First(), "res://Scenes/Spells/fireball.tscn");
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

		for (var i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var collider  = collision.GetCollider() as Node;

			if (collider != null && collider.IsInGroup("monsters"))
			{
				var monsters = collider as BaseUnit;

				var damageTaken = new HitResult(1, HitType.Normal, LifeModificationMode.Damage);
				this.InstatiateFloatingCombatText(damageTaken, GetTree().CurrentScene, new Vector2(0, -60));

				LifeCurrent -= (int)damageTaken.Value;
				LifeOrb.SetRessource(LifeCurrent);
			}
		}
	}

	public bool CanUseAbility(float manaCost)
		=> ManaCurrent >= manaCost;

	public void PlayOutOfMana()
	{
		if (!NoManaSound.IsPlaying())
			NoManaSound.Play();
	}

	public void ReduceMana(float mana)
	{
		ManaCurrent -= mana;
		ManaOrb.SetRessource(ManaCurrent);
	}

	public void _on_mana_reg_timer_timeout()
	{
		ManaCurrent += 2f;
		ManaOrb.SetRessource(ManaCurrent);
	}
}
