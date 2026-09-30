using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class GameController : Node
{
    private CircleDialog            circleDialog;
    private DeathScreen             deathScreen;
    private bool                    isSaveDue;
    private LevelUpDialog           levelUpDialog;
    private OpenLevelUpDialogButton openLevelUpDialogButton;
    private double                  secUntilSave;
    private StashWindow             stashWindow;
    private VendorWindow            vendorWindow;

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

    [Export]
    public VendorController Vendors { get; set; }

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

            SaveGameMapper.CaptureJourney(Descent.Journey, save);
        }

        if (Vendors is not null)
            SaveGameMapper.CaptureVendor(Vendors.Vendor, save);

        SaveGameStore.Save(save);
    }

    private void LoadCharacter()
    {
        var save = SavingEnabled ? LoadSave() : null;

        if (save is not null)
            Restore(save);
        else
            BeginNewCharacter();

        if (SavingEnabled)
            WatchForChanges();

        //Ein Spielstand aus der Zeit vor dem Händler bekommt hier seinen ersten Bestand, und der wird gleich gespeichert
        Vendors?.EnsureStocked();

        if (SavingEnabled && save is null)
            RequestSave();

        Descent?.ShowHub();
    }

    private void BeginNewCharacter()
    {
        Hero.CharacterName = CharacterNames.Clean(SaveSlots.TakePendingName());

        Hero.GiveStartingItems();
    }

    private static SaveGame LoadSave()
    {
        SaveGameStore.UseFileFromCommandLine();

        if (!SaveGameStore.IsFileFromCommandLine)
            AdoptSlot();

        return SaveGameStore.Load();
    }

    //Ohne Hauptmenü gestartet, etwa aus dem Editor, spielt der Charakter auf dem ersten Platz
    private static void AdoptSlot()
    {
        SaveSlots.UseDirectoryFromCommandLine();
        SaveSlots.AdoptLegacySave();

        SaveGameStore.FilePath = SaveSlots.GetPath(SaveSlots.Selected);
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

        if (Descent is not null)
            SaveGameMapper.RestoreJourney(save, Descent.Journey, Descent.FirstCircle?.Id);

        if (Vendors is not null)
            SaveGameMapper.RestoreVendor(save, Vendors.Vendor, ItemLibrary.Catalog);
    }

    private void WatchForChanges()
    {
        Hero.Items.Changed       += RequestSave;
        Hero.Loadout.SlotChanged += _ => RequestSave();
        Hero.XpChanged           += RequestSave;
        Hero.Died                += _ => RequestSave();
        Hero.Gold.Changed        += RequestSave;
        Hero.StashGold.Changed   += RequestSave;

        if (Vendors is not null)
            Vendors.Vendor.Changed += RequestSave;

        if (Descent is null)
            return;

        Descent.LevelEntered += RequestSave;
        Descent.PlaceEntered += RequestSave;
        Descent.Changed      += RequestSave;
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
        Hero.Died                                 += _ => deathScreen.ShowFor(Hero.LastXpLoss, Hero.LastGoldLoss);
        deathScreen.RespawnRequested              += Hero.Respawn;
        openLevelUpDialogButton.OpenDialogPressed += levelUpDialog.ShowDialog;

        if (Descent is not null)
            Descent.PlaceEntered += ConnectFixtures;

        if (Descent is null || circleDialog is null)
            return;

        Descent.CirclePortalUsed         += portal => circleDialog.ShowFor(portal, Descent.Journey.GetDescent(portal.Circle.Id), Hero);
        circleDialog.LevelChosen         += Descent.EnterCircle;
        circleDialog.NewDescentRequested += Descent.BeginAnew;
    }

    //Truhe und Händler stehen im Ort und entstehen mit ihm neu
    private void ConnectFixtures()
    {
        if (Descent.Place is null)
            return;

        foreach (var chest in Descent.Place.GetAllChildren<StashChest>())
            chest.Used += used => stashWindow?.ShowFor(used, Hero);

        foreach (var merchant in Descent.Place.GetAllChildren<Merchant>())
            merchant.Used += used => vendorWindow?.ShowFor(used, Hero);
    }

    private void ShowSpendablePoints()
    {
        openLevelUpDialogButton.SpendablePointLabel.Text = Hero.AttributePoints < 10 ? $"  {Hero.AttributePoints}" : $"{Hero.AttributePoints}";

        openLevelUpDialogButton.Visible = true;
    }

    private void LoadNodes()
    {
        circleDialog            = GetNodeOrNull<CircleDialog>($"%{nameof(CircleDialog)}");
        deathScreen             = GetNode<DeathScreen>($"%{nameof(DeathScreen)}");
        levelUpDialog           = GetNode<LevelUpDialog>($"%{nameof(LevelUpDialog)}");
        openLevelUpDialogButton = GetNode<OpenLevelUpDialogButton>($"%{nameof(OpenLevelUpDialogButton)}");

        var sheet = GetNodeOrNull<CharacterSheet>($"%{nameof(CharacterSheet)}");

        stashWindow  = sheet?.Stash;
        vendorWindow = sheet?.Vendor;

        vendorWindow?.Connect(Vendors);
    }
}
