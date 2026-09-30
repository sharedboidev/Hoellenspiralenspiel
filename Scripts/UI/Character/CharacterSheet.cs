using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.UI.Buttons;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class CharacterSheet : Control, IClosableWindow
{
    [Export] private EquipmentPanel equipmentPanel;
    private          IHero          hero;
    [Export] private Inventory      inventory;
    private          LevelDisplay   levelDisplay;
    [Export] private Node           player;
    private          Control        side;
    private          Statdisplay    statdisplay;

    public StashWindow Stash { get; private set; }

    public VendorWindow Vendor { get; private set; }

    private IEnumerable<ISideWindow> SideWindows => side?.GetChildren().OfType<ISideWindow>() ?? [];

    private ISideWindow OpenSideWindow => SideWindows.FirstOrDefault(window => window.IsOpen);

    public override void _Ready()
    {
        hero = player as IHero;

        if (hero is null)
        {
            GD.PushError($"Der Charakterbogen braucht einen Helden, {player?.Name} ist keiner.");

            return;
        }

        ConfigureStatDisplay();
        BindItems();
        ConfigureLevelDisplay();

        SetVisible(false);
    }

    private void ConfigureStatDisplay()
    {
        statdisplay = GetNode<Statdisplay>(nameof(Statdisplay));
        statdisplay.Render(hero.Stats);

        //Hält die Anzeige aktuell, wenn sich Attribute ändern, z.B. beim Verteilen von Punkten nach einem Level-up
        hero.SheetChanged += ShowCurrentValues;

        GetNode<StatdisplayButton>(nameof(StatdisplayButton)).Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        if (hero is not null)
            hero.SheetChanged -= ShowCurrentValues;
    }

    private void BindItems()
    {
        inventory.Bind(hero);
        equipmentPanel.Bind(hero.Items);

        side   = GetNode<Control>("Side");
        Stash  = side.GetNode<StashWindow>(nameof(StashWindow));
        Vendor = side.GetNode<VendorWindow>(nameof(VendorWindow));

        Stash.Bind(hero, this);
        Vendor.Bind(hero, this);
    }

    private void ConfigureLevelDisplay()
    {
        levelDisplay = GetNode<LevelDisplay>("%" + nameof(LevelDisplay));
        SetDisplayedLevel();
    }

    private void SetDisplayedLevel() => levelDisplay?.SetDisplayedValue(hero.Level);

    private void OnPressed(bool isToggledOpen)
    {
        statdisplay.Render(hero.Stats);
        statdisplay.Visible = isToggledOpen;
    }

    private void ShowCurrentValues()
    {
        statdisplay.Render(hero.Stats);

        SetDisplayedLevel();
    }

    //Zum Bogen gehören auch die Werteliste links, die Anzeige der Stufe und die Fenster von Truhe und Händler oben links. Ein geschlossener Bogen deckt nichts ab
    public IEnumerable<Rect2> GetCoveredRects()
        => GetChildren().OfType<Control>()
                        .SelectMany(part => part == side ? SideWindows.OfType<Control>() : [part])
                        .Where(part => part.IsVisibleInTree())
                        .Select(part => part.GetGlobalRect());

    public bool Covers(Vector2 globalPosition)
        => GetCoveredRects().Any(rect => rect.HasPoint(globalPosition));

    public override void _Process(double delta)
    {
        if (hero is not null && Input.IsActionJustPressed(InputActions.ToggleCharacterSheet) && !InputActions.IsTyping(GetViewport()))
            ToggleVisibility();
    }

    public bool IsOpen => Visible;

    public void Close()
    {
        foreach (var window in SideWindows)
            window.Close();

        Hide();
    }

    //Truhe und Händler stehen nie allein: Mit ihnen geht der Bogen auf, und es ist immer nur eines von beiden offen
    public void OpenFor(ISideWindow window)
    {
        foreach (var other in SideWindows.Where(other => other != window))
            other.Close();

        if (!Visible)
            ToggleVisibility();
    }

    public bool OfferQuick(ItemInstance item)
        => OpenSideWindow?.OfferQuick(item) ?? false;

    public string GetPriceNote(ItemInstance item)
        => OpenSideWindow?.GetPriceNote(item);

    private void ToggleVisibility()
    {
        if (Visible)
        {
            Close();

            return;
        }

        Show();

        statdisplay.Render(hero.Stats);
    }

    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}
