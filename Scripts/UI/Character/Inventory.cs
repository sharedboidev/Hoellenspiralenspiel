using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Inventory : PanelContainer
{
    private Label          goldLabel;
    private ItemGridView   gridView;
    private CharacterItems items;
    private MouseObject    mouseObject;
    private IHero          player;
    private CharacterSheet sheet;

    public ItemGridView GridView => gridView;

    public void Bind(IHero owner)
    {
        player      = owner;
        items       = owner.Items;
        gridView    = GetNode<ItemGridView>("%GridView");
        goldLabel   = GetNode<Label>("%GoldLabel");
        mouseObject = GetNode<MouseObject>(nameof(MouseObject));

        gridView.Bind(items.Inventory, items);

        gridView.PriceNote   =  item => FindSheet()?.GetPriceNote(item);
        gridView.ItemClicked += OnItemClicked;
        gridView.SlotClicked += cell => items.PlaceHeldAt(cell);

        items.Changed       += Refresh;
        player.Gold.Changed += ShowGold;

        Refresh();
        ShowGold();
    }

    public override void _ExitTree()
    {
        if (items is null)
            return;

        items.Changed       -= Refresh;
        player.Gold.Changed -= ShowGold;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
            return;

        if (items?.HeldItem is null || !IsOutsideCharacterSheet(click.GlobalPosition))
            return;

        items.DropHeld();

        //Sonst behielte das Goldfeld der Truhe den Fokus, dessen eigenes _Input kommt nach diesem nicht mehr dran
        GetViewport().GuiReleaseFocus();
        GetViewport().SetInputAsHandled();
    }

    private bool IsOutsideCharacterSheet(Vector2 globalPosition)
        => FindSheet() is { } characterSheet ? !characterSheet.Covers(globalPosition) : !GetParent<Control>().GetGlobalRect().HasPoint(globalPosition);

    private CharacterSheet FindSheet()
    {
        for (var node = GetParent(); sheet is null && node is not null; node = node.GetParent())
            sheet = node as CharacterSheet;

        return sheet;
    }

    private void Refresh()
    {
        gridView.Refresh();

        mouseObject.ShowItem(items.HeldItem);
    }

    private void ShowGold()
        => goldLabel.Text = $"{player.Gold.Amount:N0} Gold";

    private void OnItemClicked(InventoryItem view, InputEventMouseButton click)
    {
        switch (click.ButtonIndex)
        {
            //Mit Strg wandert das Item in die offene Truhe oder zum offenen Händler
            case MouseButton.Left when items.HeldItem is null && click.CtrlPressed && FindSheet()?.OfferQuick(view.Item) == true:
                break;
            case MouseButton.Left when items.HeldItem is null:
                items.TakeFromInventory(view.Item);

                break;
            case MouseButton.Left:
                items.PlaceHeldAt(gridView.GetCellAt(view, click.Position));

                break;
            case MouseButton.Right when view.Item.Definition.Consumable is not null:
                player.Consume(view.Item);

                break;
            case MouseButton.Right:
                items.EquipFromInventory(view.Item);

                break;
        }
    }
}
