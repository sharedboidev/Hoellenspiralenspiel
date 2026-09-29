using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class GameController : Node
{
    private DeathScreen             deathScreen;
    private bool                    isSaveDue;
    private LevelUpDialog           levelUpDialog;
    private OpenLevelUpDialogButton openLevelUpDialogButton;
    private double                  secUntilSave;

    [Export]
    public Hero Hero { get; set; }

    //Aus, um im Editor mit einem frischen Charakter zu testen, ohne den Spielstand anzufassen
    [Export]
    public bool SavingEnabled { get; set; } = true;

    [Export]
    public double SaveDelaySec { get; set; } = 1.0;

    //Beim Erkunden ändert sich die Karte mit jedem Schritt, geschrieben wird deshalb seltener
    [Export]
    public double ExplorationSaveDelaySec { get; set; } = 10.0;

    [Export]
    public Descent Descent { get; set; }

    public override void _Ready()
    {
        LoadNodes();
        SubscribeToEvents();

        //Geladen wird erst, wenn Held und Oberfläche fertig aufgebaut sind
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

        var save = new SaveGame { Character = Hero.CaptureProgress() };

        SaveGameMapper.CaptureLoadout(Hero.Loadout, save);
        SaveGameMapper.CaptureItems(Hero.Items, save);

        if (Descent is not null)
        {
            Descent.RememberExploration();

            SaveGameMapper.CaptureDescent(Descent.State, save);
        }

        SaveGameStore.Save(save);
    }

    private void LoadCharacter()
    {
        var save = SavingEnabled ? LoadSave() : null;

        if (save is not null)
            Restore(save);
        else
            Hero.GiveStartingItems();

        if (SavingEnabled)
            WatchForChanges();
    }

    private static SaveGame LoadSave()
    {
        SaveGameStore.UseFileFromCommandLine();

        return SaveGameStore.Load();
    }

    private void Restore(SaveGame save)
    {
        Hero.RestoreProgress(save.Character);

        SaveGameMapper.RestoreLoadout(save, Hero.Loadout);

        foreach (var missingBaseId in SaveGameMapper.RestoreItems(save, Hero.Items, ItemLibrary.Catalog))
            GD.PushWarning($"Die Item-Basis {missingBaseId} aus dem Spielstand gibt es nicht mehr, das Item fehlt.");

        Hero.RefillResources();

        if (Hero.AttributePoints > 0)
            ShowSpendablePoints();

        if (Descent is not null && SaveGameMapper.RestoreDescent(save, Descent.State) && Descent.State.IsBelowGround)
            Descent.Enter(Descent.State.Depth);
    }

    private void WatchForChanges()
    {
        Hero.Items.Changed       += RequestSave;
        Hero.Loadout.SlotChanged += _ => RequestSave();
        Hero.XpChanged           += RequestSave;
        Hero.Died                += _ => RequestSave();

        if (Descent is null)
            return;

        Descent.LevelEntered += RequestSave;
        Descent.Explored     += () => RequestSave(ExplorationSaveDelaySec);
    }

    private void RequestSave()
        => RequestSave(SaveDelaySec);

    //Mehrere Änderungen kurz hintereinander ergeben einen einzigen Schreibvorgang
    private void RequestSave(double delaySec)
    {
        if (isSaveDue && secUntilSave <= delaySec)
            return;

        isSaveDue    = true;
        secUntilSave = delaySec;
    }

    private void SubscribeToEvents()
    {
        Hero.LeveledUp                            += ShowSpendablePoints;
        Hero.Died                                 += _ => deathScreen.ShowFor(Hero.LastXpLoss);
        deathScreen.RespawnRequested              += Hero.Respawn;
        openLevelUpDialogButton.OpenDialogPressed += levelUpDialog.ShowDialog;
    }

    private void ShowSpendablePoints()
    {
        openLevelUpDialogButton.SpendablePointLabel.Text = Hero.AttributePoints < 10 ? $"  {Hero.AttributePoints}" : $"{Hero.AttributePoints}";

        openLevelUpDialogButton.Visible = true;
    }

    private void LoadNodes()
    {
        deathScreen             = GetNode<DeathScreen>($"%{nameof(DeathScreen)}");
        levelUpDialog           = GetNode<LevelUpDialog>($"%{nameof(LevelUpDialog)}");
        openLevelUpDialogButton = GetNode<OpenLevelUpDialogButton>($"%{nameof(OpenLevelUpDialogButton)}");
    }
}
