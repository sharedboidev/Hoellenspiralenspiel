using Godot;
using System;
using System.Globalization;

public partial class CooldownSkill : TextureButton
{
	[Export] public TextureProgressBar ProgressBarCooldown;
	[Export] public Timer              TimerCooldown;
	[Export] public Label              LabelTime;
	public          double             Cooldown   { get; set; } = 1.0d;
	public          bool               OnCooldown => Disabled;
	
	public override void _Ready()
	{
		TimerCooldown.WaitTime       = Cooldown;
		ProgressBarCooldown.MaxValue = TimerCooldown.WaitTime;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		LabelTime.Text            = TimerCooldown.TimeLeft.ToString("#.##");
		ProgressBarCooldown.Value = TimerCooldown.TimeLeft;
	}

	public void Use()
	{
		TimerCooldown.Start();
		Disabled = true;
		SetProcess(true);
	}

	public void _on_timer_timeout()
	{
		Disabled                  = false;
		LabelTime.Text            = string.Empty;
		ProgressBarCooldown.Value = 0;
		SetProcess(false);
	}
}
