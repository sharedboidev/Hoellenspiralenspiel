using Godot;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class CharacterSheet : Control
{
    [Export] private EquipmentPanel equipmentPanel;
    [Export] private Inventory      inventory;
    private          LevelDisplay   levelDisplay;
    [Export] private Player2D       player;
    private          Statdisplay    statdisplay;
    [Export] private int            viewportMarginHeightPx;
    [Export] private int            viewportMarginWidthPx;

    public override void _Ready()
    {
        SetPositionRelativeToViewport();
        ConfigureStatDisplay();
        BindItems();
        ConfigureLevelDisplay();

        SetVisible(false);
    }

    private void ConfigureStatDisplay()
    {
        statdisplay = GetNode<Statdisplay>(nameof(Statdisplay));
        statdisplay.Render(player);

        //Hält die Anzeige aktuell, wenn sich Attribute ändern, z.B. beim Verteilen von Punkten nach einem Level-up
        player.StatsChanged += RerenderStatdisplay;

        GetNode<StatdisplayButton>(nameof(StatdisplayButton)).Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        if (player is null)
            return;

        player.StatsChanged     -= RerenderStatdisplay;
        player.LeveledUp        -= PlayerOnLeveledUp;
        player.ProgressRestored -= SetDisplayedLevel;
    }

    private void BindItems()
    {
        inventory.Bind(player);
        equipmentPanel.Bind(player);
    }

    private void ConfigureLevelDisplay()
    {
        levelDisplay = GetNode<LevelDisplay>("%" + nameof(LevelDisplay));
        SetDisplayedLevel();

        player.LeveledUp        += PlayerOnLeveledUp;
        player.ProgressRestored += SetDisplayedLevel;
    }

    private void SetDisplayedLevel() => levelDisplay.SetDisplayedValue(player.Level);

    private void PlayerOnLeveledUp(Player2D player2D) => SetDisplayedLevel();

    private void OnPressed(bool isToggledOpen)
    {
        statdisplay.Render(player);
        statdisplay.Visible = isToggledOpen;
    }

    private void RerenderStatdisplay()
        => statdisplay.Render(player);

    private void SetPositionRelativeToViewport()
    {
        var viewportSize  = GetViewportRect().Size;
        var sheetsize     = equipmentPanel.Size;
        var sheetPosition = new Vector2(viewportSize.X - sheetsize.X - viewportMarginWidthPx, viewportMarginHeightPx);

        Position = sheetPosition;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(InputActions.ToggleCharacterSheet))
            ToggleVisibility();
    }

    private void ToggleVisibility()
    {
        Visible = !Visible;

        if(Visible)
            statdisplay.Render(player);
    }

    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}
