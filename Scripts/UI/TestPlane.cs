using Godot;

public partial class TestPlane : Node
{
    [Export] private AudioStreamPlayer2D BackgroundPlayer;

    public override void _Ready()
    {
        if (!BackgroundPlayer.IsPlaying())
            BackgroundPlayer.Play();
    }
}