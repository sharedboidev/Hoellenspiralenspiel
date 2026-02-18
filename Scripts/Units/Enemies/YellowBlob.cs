using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class YellowBlob : BaseEnemy
{
    protected override PackedScene AttackScene { get; }

    public override void _Ready()
    {
        base._Ready();

        ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");
    }

    protected override void ExecuteAttack() { }
}