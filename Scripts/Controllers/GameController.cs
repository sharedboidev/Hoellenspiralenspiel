using Godot;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class GameController : Node
{
    private LevelUpDialog           levelUpDialog;
    private OpenLevelUpDialogButton openLevelUpDialogButton;
    private Player2D                player;

    public override void _Ready()
    {
        LoadNodes();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        player.LeveledUp                          += PlayerOnLeveledUp;
        openLevelUpDialogButton.OpenDialogPressed += OpenLevelUpDialogButtonOnOpenDialogPressed;
    }

    private void OpenLevelUpDialogButtonOnOpenDialogPressed()
        => levelUpDialog.ShowDialog();

    private void PlayerOnLeveledUp(Player2D player2D)
        => openLevelUpDialogButton.Visible = true;

    private void LoadNodes()
    {
        player                  = GetNode<Player2D>("%Player 2D");
        levelUpDialog           = GetNode<LevelUpDialog>($"%{nameof(LevelUpDialog)}");
        openLevelUpDialogButton = GetNode<OpenLevelUpDialogButton>($"%{nameof(OpenLevelUpDialogButton)}");
    }
}