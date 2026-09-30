using System.Linq;
using Godot;
using Hoellenspiralenspiel.Interfaces;
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
    private          Statdisplay    statdisplay;

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

    //Zum Bogen gehören auch die Werteliste links und die Anzeige der Stufe, nicht nur Ausrüstung und Inventar. Ein geschlossener Bogen deckt nichts ab
    public bool Covers(Vector2 globalPosition)
        => GetChildren().OfType<Control>().Any(part => part.IsVisibleInTree() && part.GetGlobalRect().HasPoint(globalPosition));

    public override void _Process(double delta)
    {
        if (hero is not null && Input.IsActionJustPressed(InputActions.ToggleCharacterSheet))
            ToggleVisibility();
    }

    public bool IsOpen => Visible;

    public void Close() => Hide();

    private void ToggleVisibility()
    {
        Visible = !Visible;

        if(Visible)
            statdisplay.Render(hero.Stats);
    }

    private void ModifyVisibilityThroughSelfModulate(Control control)
    {
        var newSelfModulate = control.SelfModulate;
        newSelfModulate.A = newSelfModulate.A == 0 ? 1 : 0;

        control.SetSelfModulate(newSelfModulate);
    }
}
