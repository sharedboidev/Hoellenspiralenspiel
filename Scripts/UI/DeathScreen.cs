using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class DeathScreen : Control
{
    public delegate void RespawnRequestedEventHandler();

    private Label  lossLabel;
    private Button respawnButton;

    public event RespawnRequestedEventHandler RespawnRequested;

    public override void _Ready()
    {
        lossLabel     = GetNode<Label>("%LossLabel");
        respawnButton = GetNode<Button>("%RespawnButton");

        respawnButton.Pressed += OnRespawnPressed;

        Hide();
    }

    public void ShowFor(long experienceLost, int goldDropped = 0)
    {
        lossLabel.Text = experienceLost > 0 ? $"You lost {experienceLost:N0} experience." : "You lost no experience.";

        if (goldDropped > 0)
            lossLabel.Text += $"\nYou dropped {goldDropped:N0} gold where you fell.";

        Show();

        respawnButton.GrabFocus();
    }

    private void OnRespawnPressed()
    {
        Hide();

        RespawnRequested?.Invoke();
    }
}
