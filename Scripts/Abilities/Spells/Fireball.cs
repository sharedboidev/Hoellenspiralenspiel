using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Abilities.Spells;

public partial class Fireball : Area2D
{
	[Export] public AnimatedSprite2D AnimationSprite;
	private         Vector2          richtung;
	private         FireballSkill    skill;
	public          BaseUnit         ShotBy               { get; set; }
	public          int              MaxForks             { get; set; } = 5;
	public          int              TimesForked          { get; set; }
	public          double           LebenszeitSec        { get; set; } = 15;
	public          double           LebenszeitCurrentSec { get; set; }

	private List<BaseUnit> forkTargets = new List<BaseUnit>(); 
	
	
	public override void _Ready()
	{
		AnimationSprite.Play("default");
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is TileMapLayer tileMapLayer && tileMapLayer.Name == "Walls")
			QueueFree();

		else
		{
			if (body.IsInGroup("monsters") && ShotBy != body && body is BaseEnemy hitEnemy)
			{
				var damageResult = skill.MakeRealDamage(hitEnemy);
				hitEnemy.LifeCurrent -= (int)damageResult.Value;
				hitEnemy.InstatiateFloatingCombatText(damageResult, GetTree().CurrentScene, new Vector2(0, -60));

				var controller = GetTree().CurrentScene.GetNode<EnemyController>("%" + nameof(EnemyController));

				if (controller.SpawnedEnemies.Count < 2)
				{
					QueueFree();

					return;
				}
				
				
				
				var possibleTargets = controller.SpawnedEnemies.Except([hitEnemy]).ToList();
				var nearestBois     = hitEnemy.FindClosestEnemyFrom(possibleTargets, 2);
				var fireballScene   = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/fireball.tscn");

				var timesForked = TimesForked + 1;

				foreach (var friend in nearestBois)
				{
					if (TimesForked >= MaxForks)
						continue;

					var fireball = (Fireball)fireballScene.Instantiate();
					fireball.TimesForked = timesForked;
					fireball.ShotBy      = hitEnemy;

					fireball.Init(new FireballSkill(skill.Owner), hitEnemy.Position, friend.Position);
					GetTree().CurrentScene.GetNode<Node2D>("Environment").CallDeferred(Node.MethodName.AddChild, fireball);
				}

				QueueFree();
			}
		}
	}

	public void Init(FireballSkill skill, Vector2 startGlobal, Vector2 destinationGlobal)
	{
		this.skill     = skill;
		GlobalPosition = startGlobal;

		richtung = destinationGlobal - startGlobal;

		if (richtung.LengthSquared() < 0.0001f)
			richtung = Vector2.Right;
		else
			richtung = richtung.Normalized();

		Rotation = (-richtung).Angle();
	}

	public override void _Process(double d)
	{
		var delta = Convert.ToSingle(d);

		Position             += richtung * 800 * delta;
		LebenszeitCurrentSec += d;

		if (LebenszeitCurrentSec >= LebenszeitSec)
		{
			LebenszeitCurrentSec = 0;
			QueueFree();
		}
	}
}
