using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

//Der Testgegner wirft Feuer. Sein Skill ist in der Szene zugewiesen
public partial class TestEnemy : BaseEnemy
{
	public override void _Ready()
	{
		base._Ready();

		ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");
	}

	protected override Sprite2D MovementSprite => GetNode<Sprite2D>(nameof(Sprite2D));
}
