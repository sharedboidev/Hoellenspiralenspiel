using Godot;
using System;

public partial class TestPlane : Node
{
	[Export] AudioStreamPlayer2D BackgroundPlayer;

	public override void _Ready()
	{
		if (!BackgroundPlayer.IsPlaying())
			BackgroundPlayer.Play();
	}
}
