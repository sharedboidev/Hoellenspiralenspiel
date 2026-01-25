using System;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class Fireball : Area2D
{
	private       Vector2 richtung;
	public        Vector2 Destination { get; set; }

	private Random damageRng = new Random();
	private Random critRng = new Random();
	
	public override void _Ready()
	{
		var animationNode = GetNode<CollisionPolygon2D>("CollisionPolygon2D").GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animationNode.Play("default");
		
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("monsters"))
		{
			var damage  = damageRng.Next(50, 251);
			var isCrit  = critRng.Next(1, 11) == 10;
			var hitType = isCrit ? HitType.Critical : HitType.Normal;
			damage = isCrit ? (int)(damage * 1.3m) : damage;
			
			var       enemy  = body as BaseEnemy;
			enemy.LifeCurrent -= damage;
			
			
			var fakeHit = new HitResult(damage, hitType, LifeModificationMode.Damage);
			enemy.InstatiateFloatingCombatText(fakeHit, GetTree().CurrentScene, offset: new Vector2(0, -188));
			QueueFree(); 
		}
	}

	public void Init(Vector2 startGlobal, Vector2 destinationGlobal)
	{
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

		Position += richtung * 800 * delta;
	}
}
