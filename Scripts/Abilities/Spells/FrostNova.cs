using Godot;
using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class FrostNova : Area2D,
								  ISpell
{
	// Called when the node enters the scene tree for the first time.
	private bool           durationOver = false;
	private FrostNovaSkill skill;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{		
		if (durationOver)
			QueueFree();
		else
			Scale += new Vector2(8f  * (float)delta, 8f * (float)delta);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("monsters") &&  body is BaseEnemy enemy)
		{
			var result       = skill.MakeRealDamage(enemy);
			enemy.LifeCurrent -= (int)result.Value;
			enemy.InstatiateFloatingCombatText(result, GetTree().CurrentScene, new Vector2(0, -60));
		}
		
	}

	public void _on_expand_duration_timer_timeout()
	{
		durationOver = true;
	}

	public void Init(BaseSkill s, Vector2 globalPlayerPosition, Vector2 _)
	{
		this.skill          = s as FrostNovaSkill;
		this.GlobalPosition = globalPlayerPosition;
	}
}
