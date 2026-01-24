using System;
using Godot;

public partial class Fireball : Node2D
{
	private Vector2 richtung;
	public  Vector2 Destination { get; set; }

	public override void _Ready()
	{
		var animationNode = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animationNode.Play("default");
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
		GlobalPosition += richtung * 800 * delta;
	}
}
