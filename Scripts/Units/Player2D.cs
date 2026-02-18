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
	[Export] private ResourceOrb     LifeOrb;
	private          float           manaCurrent;
	[Export] private ResourceOrb     ManaOrb;
	private          float           manaProSekunde = 5f;
	[Export] public  HBoxContainer   SkillBar;
	private          PackedScene     SkillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
	private          AnimationTree   AnimationTree { get; set; }

	[Export] private PackedScene FlamethrowerScene = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/nova.tscn");
	
	[Export]
	public AudioStreamPlayer2D NoManaSound { get; set; }

	public float ManaCurrent
	{
		get => manaCurrent;
		set => SetField(ref manaCurrent, Math.Min(value, ManaMaximum));
	}

	[Export]
	public float ManaMaximum { get; set; } = 100;

	public override void _Ready()
	{
		ManaCurrent = ManaMaximum;

		ManaOrb.Init(this, ResourceType.Mana);
		LifeOrb.Init(this, ResourceType.Life);

		base._Ready();

		skills.Add(new FireballSkill(this));
		skills.Add(new FrostNovaSkill(this));

		var fireballActionBarItem = SkillBarIcon.Instantiate<CooldownSkill>();
		fireballActionBarItem.Init(skills.First(), "res://Scenes/Spells/fireball.tscn", Key.F);
		var frostNovaActionBarItem = SkillBarIcon.Instantiate<CooldownSkill>();
		frostNovaActionBarItem.Init(skills.Last(), "res://Scenes/Spells/frost_nova.tscn", Key.E);

		SkillBar.AddChild(fireballActionBarItem);
		SkillBar.AddChild(frostNovaActionBarItem);

		AnimationTree        = GetNode<AnimationTree>(nameof(AnimationTree));
		
	}

	public bool IsInAggroRangeOf(BaseEnemy enemy)
	{
		var distanceToEnemy = Math.Sqrt(GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition));

		return distanceToEnemy <= enemy.AggroRange;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (ManaCurrent < ManaMaximum)
		{
			ManaCurrent += manaProSekunde * (float)delta;
			ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
			ManaOrb.SetRessource(ManaCurrent);
		}

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

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Flamethrower                ??= FlamethrowerScene.Instantiate<Node2D>();
			Flamethrower.GlobalPosition =   this.GlobalPosition;

			var richtung = this.GlobalPosition - GetViewport().GetCamera2D().GetGlobalMousePosition();

			if (richtung.LengthSquared() < 0.0001f)
				richtung = Vector2.Right;
			else
				richtung = richtung.Normalized();

			Flamethrower.Rotation = (-richtung).Angle();
			 
			GetNode("SpellsContainer").AddChild(Flamethrower);
			
		}
		else
		{
			Flamethrower?.QueueFree();
			Flamethrower = null;
		}
	}

	private Node2D Flamethrower = null;
	
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
}
