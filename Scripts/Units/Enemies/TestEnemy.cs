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

	protected override Sprite2D MovementSprite => GetNode<Sprite2D>(nameof(Sprite2D));

	//Der Feuerball ist ein SPELL und bringt seinen Grundschaden aus dem Inspector mit
	protected override void ExecuteAttack()
	{
		Velocity = Vector2.Zero;

		var fireball = AttackScene.Instantiate<Fireball>();
		fireball.ShotBy = this;

		GetTree()
			   .CurrentScene
			   .GetNode<Node2D>("Environment")
			   .AddChild(fireball);

		fireball.Init(new FireballSkill(this, AttackDamageMin, AttackDamageMax), GlobalPosition, ChasedPlayer.GlobalPosition);
	}
}
