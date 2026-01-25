using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class TestEnemy : BaseEnemy
{
	private AnimationTree AnimationTree { get; set; }

	public override void _Ready()
	{
		base._Ready();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        ChasedPlayer  = CurrentScene.GetNode<Player2D>("Player 2D");
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