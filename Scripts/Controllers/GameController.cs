using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class GameController : Node
{
    private DeathScreen             deathScreen;
    private bool                    isSaveDue;
    private LevelUpDialog           levelUpDialog;
    private OpenLevelUpDialogButton openLevelUpDialogButton;
    private Player2D                player;
    private double                  secUntilSave;

    //Aus, um im Editor mit einem frischen Charakter zu testen, ohne den Spielstand anzufassen
    [Export]
    public bool SavingEnabled { get; set; } = true;

    [Export]
    public double SaveDelaySec { get; set; } = 1.0;

    public override void _Ready()
    {
        LoadNodes();
        SubscribeToEvents();

        //Geladen wird erst, wenn Spieler und Oberfläche fertig aufgebaut sind
        Callable.From(LoadCharacter).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (!isSaveDue)
            return;

        secUntilSave -= delta;

        if (secUntilSave <= 0)
            SaveCharacter();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && isSaveDue)
            SaveCharacter();
    }

    public override void _ExitTree()
    {
        if (isSaveDue)
            SaveCharacter();
    }

    public void SaveCharacter()
    {
        isSaveDue = false;

        if (!SavingEnabled)
            return;

        var save = new SaveGame { Character = player.CaptureProgress() };

        SaveGameMapper.CaptureLoadout(player.Loadout, save);
        SaveGameMapper.CaptureItems(player.Items, save);

        SaveGameStore.Save(save);
    }

    private void LoadCharacter()
    {
        if (!SavingEnabled)
            return;

        SaveGameStore.UseFileFromCommandLine();

        var save = SaveGameStore.Load();

        if (save is not null)
            Restore(save);

        WatchForChanges();
    }

    private void Restore(SaveGame save)
    {
        player.RestoreProgress(save.Character);

        SaveGameMapper.RestoreLoadout(save, player.Loadout);

        foreach (var missingBaseId in SaveGameMapper.RestoreItems(save, player.Items, ItemLibrary.Catalog))
            GD.PushWarning($"Die Item-Basis {missingBaseId} aus dem Spielstand gibt es nicht mehr, das Item fehlt.");

        player.RefillResources();

        if (player.AttributePointsAllowedToSpend > 0)
            ShowSpendablePoints();
    }

    private void WatchForChanges()
    {
        player.Items.Changed       += RequestSave;
        player.Loadout.SlotChanged += _ => RequestSave();
        player.LeveledUp           += _ => RequestSave();
        player.Died                += _ => RequestSave();
        player.PropertyChanged     += PlayerOnPropertyChanged;
    }

    private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Player2D.XpTotal)
                           or nameof(Player2D.StrengthBase)
                           or nameof(Player2D.DexterityBase)
                           or nameof(Player2D.IntelligenceBase)
                           or nameof(Player2D.ConstitutionBase)
                           or nameof(Player2D.AwarenessBase))
            RequestSave();
    }

    //Mehrere Änderungen kurz hintereinander ergeben einen einzigen Schreibvorgang
    private void RequestSave()
    {
        if (isSaveDue)
            return;

        isSaveDue    = true;
        secUntilSave = SaveDelaySec;
    }

    private void SubscribeToEvents()
    {
        player.LeveledUp                          += PlayerOnLeveledUp;
        player.Died                               += PlayerOnDied;
        deathScreen.RespawnRequested              += player.Respawn;
        openLevelUpDialogButton.OpenDialogPressed += OpenLevelUpDialogButtonOnOpenDialogPressed;
    }

    private void OpenLevelUpDialogButtonOnOpenDialogPressed()
        => levelUpDialog.ShowDialog();

    private void PlayerOnDied(BaseUnit unit)
        => deathScreen.ShowFor(player.LastXpLoss);

    private void PlayerOnLeveledUp(Player2D player2D)
        => ShowSpendablePoints();

    private void ShowSpendablePoints()
    {
        openLevelUpDialogButton.SpendablePointLabel.Text = player.AttributePointsAllowedToSpend < 10
                ? $"  {player.AttributePointsAllowedToSpend}"
                : $"{player.AttributePointsAllowedToSpend}";

        openLevelUpDialogButton.Visible = true;
    }

    private void LoadNodes()
    {
        player                  = GetNode<Player2D>("%Player 2D");
        deathScreen             = GetNode<DeathScreen>($"%{nameof(DeathScreen)}");
        levelUpDialog           = GetNode<LevelUpDialog>($"%{nameof(LevelUpDialog)}");
        openLevelUpDialogButton = GetNode<OpenLevelUpDialogButton>($"%{nameof(OpenLevelUpDialogButton)}");
    }
}
