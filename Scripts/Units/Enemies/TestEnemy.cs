using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class TestEnemy : BaseEnemy
{
	protected override PackedScene AttackScene => ResourceLoader.Load<PackedScene>("res://Scenes/Spells/fireball.tscn");

	public override void _Ready()
	{
		base._Ready();

		ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");
	}

	protected override void ExecuteAttack()
	{
		Velocity = Vector2.Zero;

		var fireball = AttackScene.Instantiate<Fireball>();
		fireball.ShotBy = this;

		GetTree()
			   .CurrentScene
			   .GetNode<Node2D>("Environment")
			   .AddChild(fireball);

		fireball.Init(new FireballSkill(this), Position, ChasedPlayer.Position);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (MovementDirection != Vector2.Zero)
		{
			AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
			AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
		}
	}
}
