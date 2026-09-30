using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die Truhe des Charakters. Sie liegt links neben dem Charakterbogen und hält Items und Gold
public partial class StashWindow
        : PanelContainer,
          ISideWindow
{
    private const float ReachSlackPx = 60f;

    private HBoxContainer  depositRow;
    private Label          goldLabel;
    private ItemGridView   gridView;
    private IHero          hero;
    private CharacterItems items;
    private CharacterSheet sheet;
    private Fixture        source;
    private BaseUnit       user;
    private HBoxContainer  withdrawRow;

    //Die Knöpfe zum Ein- und Auszahlen. Dahinter steht immer noch einer für alles
    [Export]
    public Array<int> GoldSteps { get; set; } = [10, 100, 1000];

    [Export]
    public Vector2 GoldButtonSize { get; set; } = new(120, 44);

    [Export]
    public int GoldFontSize { get; set; } = 20;

    public bool IsOpen => Visible;

    public void Bind(IHero owner, CharacterSheet characterSheet)
    {
        hero        = owner;
        items       = owner.Items;
        sheet       = characterSheet;
        gridView    = GetNode<ItemGridView>("%GridView");
        goldLabel   = GetNode<Label>("%StashGoldLabel");
        depositRow  = GetNode<HBoxContainer>("%DepositRow");
        withdrawRow = GetNode<HBoxContainer>("%WithdrawRow");

        gridView.Bind(items.Stash, items);

        gridView.ItemClicked += OnItemClicked;
        gridView.SlotClicked += cell => items.PlaceHeldAt(items.Stash, cell);

        items.Changed          += gridView.Refresh;
        hero.StashGold.Changed += ShowGold;

        AddGoldButtons(depositRow, hero.Gold, hero.StashGold);
        AddGoldButtons(withdrawRow, hero.StashGold, hero.Gold);

        ShowGold();
        Hide();
    }

    public override void _ExitTree()
    {
        if (items is null)
            return;

        items.Changed          -= gridView.Refresh;
        hero.StashGold.Changed -= ShowGold;
    }

    //Wer von der Truhe wegläuft, braucht ihr Fenster nicht mehr
    public override void _Process(double delta)
    {
        if (Visible && (!IsInstanceValid(source) || !IsInstanceValid(user) || !source.IsNear(user, ReachSlackPx)))
            Close();
    }

    public void ShowFor(Fixture chest, BaseUnit opener)
    {
        if (items is null || chest is null || opener is null)
            return;

        source = chest;
        user   = opener;

        sheet.OpenFor(this);

        gridView.Refresh();

        Show();
    }

    //Das Item an der Maus soll im Hub nicht zu Boden fallen, dort verfiele es bei der nächsten Reise
    public void Close()
    {
        if (!Visible)
            return;

        Hide();

        source = null;

        if (!items.ReturnHeld())
            items.StowHeld(items.Stash);
    }

    public bool OfferQuick(ItemInstance item)
    {
        items.Transfer(item, items.Inventory, items.Stash);

        return true;
    }

    public string GetPriceNote(ItemInstance item)
        => null;

    private void OnItemClicked(InventoryItem view, InputEventMouseButton click)
    {
        if (click.ButtonIndex != MouseButton.Left)
            return;

        if (items.HeldItem is not null)
            items.PlaceHeldAt(items.Stash, gridView.GetCellAt(view, click.Position));
        else if (click.CtrlPressed)
            items.Transfer(view.Item, items.Stash, items.Inventory);
        else
            items.TakeFrom(items.Stash, view.Item);
    }

    private void ShowGold()
        => goldLabel.Text = $"Stored: {hero.StashGold.Amount:N0} Gold";

    private void AddGoldButtons(HBoxContainer row, Purse from, Purse to)
    {
        foreach (var step in GoldSteps)
            row.AddChild(CreateGoldButton($"{step:N0}", from, to, step));

        row.AddChild(CreateGoldButton("All", from, to, int.MaxValue));
    }

    private Button CreateGoldButton(string text, Purse from, Purse to, int amount)
    {
        var button = new Button
        {
            Name              = amount == int.MaxValue ? "All" : $"Step{amount}",
            Text              = text,
            CustomMinimumSize = GoldButtonSize,
            FocusMode         = FocusModeEnum.None
        };

        button.AddThemeFontSizeOverride("font_size", GoldFontSize);

        button.Pressed += () => Purse.Move(from, to, amount);

        return button;
    }
}
