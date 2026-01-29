using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class CooldownSkill : TextureButton
{
	private         double             cooldown = 1.0d;
	[Export] public Label              LabelTime;
	[Export] public TextureProgressBar ProgressBarCooldown;
	private         BaseSkill          skill;
	[Export] public Timer              TimerCooldown;
	private         PackedScene        visualScene;

	public void Init(BaseSkill skill,
					 string    visualResourceName)
	{
		this.skill  = skill;
		cooldown    = skill.RealCooldown;
		visualScene = ResourceLoader.Load<PackedScene>(visualResourceName);
	}

	public override void _Ready()
	{
		TimerCooldown.WaitTime       = cooldown;
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
		var someSkill = visualScene.Instantiate<Area2D>() as Fireball;

		someSkill.Init(skill as FireballSkill,
					   skill.Owner.GlobalPosition,
					   GetViewport().GetCamera2D().GetGlobalMousePosition());
		GetTree().CurrentScene.GetNode<Node2D>("Environment").AddChild(someSkill);

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

	public void _on_pressed()
		=> Use();
}
