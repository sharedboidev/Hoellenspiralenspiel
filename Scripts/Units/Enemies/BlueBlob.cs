using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class BlueBlob : BaseEnemy
{
    protected override PackedScene AttackScene { get; }

    public override void _Ready()
    {
        base._Ready();

        ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");

        RandomizeAnimation();
    }

    private void RandomizeAnimation()
    {
        AnimationTree.Active = false;

        var animationPlayer = GetNode<AnimationPlayer>(nameof(AnimationPlayer));
        animationPlayer.Play("run_down");
        animationPlayer.Seek(GD.Randf() * animationPlayer.CurrentAnimationLength, true);

        AnimationTree.Active = true;
    }

    protected override void ExecuteAttack() { }
}