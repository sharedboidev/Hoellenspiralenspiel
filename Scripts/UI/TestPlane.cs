using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class TestPlane : Node
{
	[Export] private AudioStreamPlayer2D backgroundPlayer;

	public override void _Ready()
	{
		if (!backgroundPlayer.IsPlaying())
			backgroundPlayer.Play();
	}
}
