using System;
using Godot;

public partial class LightningStrike : Node2D
{
	public override void _Ready()
	{
		GetNode<AnimatedSprite2D>("AnimationLightning").Play();
		GetNode<AnimatedSprite2D>("AnimationExplosion").Play();
		Started?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler Finished;
	public event EventHandler Started;

	public void PlaySound()
	{
		var sfx = GetNode<AudioStreamPlayer2D>("SFX");
		sfx.Finished += () => Finished?.Invoke(this, EventArgs.Empty);
		sfx.Play();
	}
}
