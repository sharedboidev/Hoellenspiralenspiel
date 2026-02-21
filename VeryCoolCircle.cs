using Godot;
using System;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class VeryCoolCircle : Area2D
{
	private bool isPositionFixed;

	public override void _Ready()
	{
		// Signale verbinden
		BodyEntered += OnBodyEntered;
		BodyExited  += OnBodyExited;
	}
	

	private void OnBodyEntered(Node2D body)
	{
		if (body is BaseEnemy enemy)
		{
			enemy.SetHighlight(true);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is BaseEnemy enemy)
		{
			enemy.SetHighlight(false);
		}
	}
	public void StickToCurrentPosition()
	{
		isPositionFixed = true;
	}

	public void BeCool(Action onItWasCool)
	{
		var animation = GetNode<AnimatedSprite2D>("Animation");
		animation.Play();
		animation.AnimationFinished += () => onItWasCool();
	}

	public void UpdateGlobalPosition(Vector2 globalPos)
	{
		if (isPositionFixed)
		{
			return;
		}
		
		this.GlobalPosition = globalPos;
	}
}
