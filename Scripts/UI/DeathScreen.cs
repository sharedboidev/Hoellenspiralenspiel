using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

//Erscheint beim Tod des Spielers und meldet, wenn er wiederbelebt werden will
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

    public void ShowFor(long experienceLost)
    {
        lossLabel.Text = experienceLost > 0 ? $"You lost {experienceLost:N0} experience." : "You lost no experience.";

        Show();

        respawnButton.GrabFocus();
    }

    private void OnRespawnPressed()
    {
        Hide();

        RespawnRequested?.Invoke();
    }
}
