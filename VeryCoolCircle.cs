using Godot;
using System;

public partial class VeryCoolCircle : Node2D
{
	private bool isPositionFixed;

	public void StickToCurrentPosition()
	{
		isPositionFixed = true;
	}

	public void BeCool()
	{
		GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play();
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
