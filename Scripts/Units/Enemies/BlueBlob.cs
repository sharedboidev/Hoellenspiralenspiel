using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class BlueBlob : BaseEnemy
{
    public override void _Ready()
    {
        base._Ready();

        ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");
    }

    protected override PackedScene AttackScene { get; }

    protected override void ExecuteAttack() { }
}