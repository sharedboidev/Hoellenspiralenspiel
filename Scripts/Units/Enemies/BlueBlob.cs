using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class BlueBlob : BaseEnemy
{
    public override void _Ready()
    {
        base._Ready();

        ChasedPlayer = CurrentScene.GetNode<Player2D>("%Player 2D");

        RandomizeAnimation();
    }

    protected override Sprite2D MovementSprite => GetNode<Sprite2D>("RunSprite");

    private void RandomizeAnimation()
    {
        AnimationTree.Active = false;

        var animationPlayer = GetNode<AnimationPlayer>(nameof(AnimationPlayer));
        animationPlayer.Play("run_down");
        animationPlayer.Seek(GD.Randf() * animationPlayer.CurrentAnimationLength, true);

        AnimationTree.Active = true;
    }
}
