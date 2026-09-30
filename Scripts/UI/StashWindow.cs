using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die Truhe des Charakters. Sie liegt oben links am Bildschirm und hält Items und Gold
public partial class StashWindow
        : PanelContainer,
          ISideWindow
{
    private const float ReachSlackPx = 60f;

    private LineEdit       amountField;
    private Label          goldLabel;
    private ItemGridView   gridView;
    private IHero          hero;
    private CharacterItems items;
    private CharacterSheet sheet;
    private Fixture        source;
    private BaseUnit       user;

    [Export]
    public float GoldFieldWidth { get; set; } = 220f;

    [Export]
    public Vector2 GoldButtonSize { get; set; } = new(160, 44);

    [Export]
    public int GoldFontSize { get; set; } = 20;

    public bool IsOpen => Visible;

    public void Bind(IHero owner, CharacterSheet characterSheet)
    {
        hero      = owner;
        items     = owner.Items;
        sheet     = characterSheet;
        gridView  = GetNode<ItemGridView>("%GridView");
        goldLabel = GetNode<Label>("%StashGoldLabel");

        gridView.Bind(items.Stash, items);

        gridView.ItemClicked += OnItemClicked;
        gridView.SlotClicked += cell => items.PlaceHeldAt(items.Stash, cell);

        items.Changed          += gridView.Refresh;
        hero.StashGold.Changed += ShowGold;

        AddGoldControls(GetNode<HBoxContainer>("%GoldRow"), GetNode<HBoxContainer>("%GoldAllRow"));

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

    //Klicks in die Welt oder auf Knöpfe ohne Fokus nehmen dem Feld den Fokus nicht ab, die Ziffern 1-4 landeten sonst weiter darin statt bei den Skills
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } click || amountField?.HasFocus() != true)
            return;

        if (!amountField.GetGlobalRect().HasPoint(click.GlobalPosition))
            amountField.ReleaseFocus();
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

    private void AddGoldControls(HBoxContainer amountRow, HBoxContainer allRow)
    {
        amountField = new LineEdit
        {
            Name               = "AmountField",
            PlaceholderText    = "Amount",
            Alignment          = HorizontalAlignment.Right,
            SelectAllOnFocus   = true,
            ContextMenuEnabled = false,
            EmojiMenuEnabled   = false,
            CustomMinimumSize  = new Vector2(GoldFieldWidth, GoldButtonSize.Y)
        };

        amountField.AddThemeFontSizeOverride("font_size", GoldFontSize);

        amountField.TextChanged   += FilterAmount;
        amountField.TextSubmitted += _ => amountField.ReleaseFocus();

        amountRow.AddChild(amountField);

        AddGoldButton(amountRow, "Deposit", hero.Gold, hero.StashGold, movesAll: false);
        AddGoldButton(amountRow, "Withdraw", hero.StashGold, hero.Gold, movesAll: false);
        AddGoldButton(allRow, "Deposit", hero.Gold, hero.StashGold, movesAll: true);
        AddGoldButton(allRow, "Withdraw", hero.StashGold, hero.Gold, movesAll: true);
    }

    private void AddGoldButton(HBoxContainer row, string verb, Purse from, Purse to, bool movesAll)
    {
        var button = new Button
        {
            Name              = movesAll ? $"{verb}All" : verb,
            Text              = movesAll ? $"{verb} all" : verb,
            CustomMinimumSize = GoldButtonSize,
            FocusMode         = FocusModeEnum.None
        };

        button.AddThemeFontSizeOverride("font_size", GoldFontSize);

        button.Pressed += () =>
        {
            Purse.Move(from, to, movesAll ? int.MaxValue : GoldAmountText.Parse(amountField.Text));

            amountField.ReleaseFocus();
        };

        row.AddChild(button);
    }

    //Das Setzen von Text meldet kein TextChanged und stellt den Caret nicht wieder her
    private void FilterAmount(string text)
    {
        var (digits, caret) = GoldAmountText.KeepDigits(text, amountField.CaretColumn);

        if (digits == text)
            return;

        amountField.Text        = digits;
        amountField.CaretColumn = caret;
    }
}
