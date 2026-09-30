using System;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class InventoryItem
        : PanelContainer,
          ITooltipObjectContainer
{
    public delegate void ClickedEventHandler(InventoryItem view, InputEventMouseButton click);

    public delegate void HoverChangedEventHandler(InventoryItem view, bool isHovered);

    private static readonly Color AllowedColor   = new(0, 0.25f, 0, .9f);
    private static readonly Color ForbiddenColor = new(0.25f, 0, 0, .9f);

    private StyleBoxFlat   allowedStylebox;
    private Vector2        cellSize;
    private StyleBoxFlat   defaultStylebox;
    private StyleBoxFlat   forbiddenStylebox;
    private CharacterItems items;
    private Label          stackLabel;

    public ItemInstance Item { get; private set; }

    //Nennt den Preis, solange ein Händler offen ist. Sonst steht im Tooltip keiner
    public Func<ItemInstance, string> PriceNote { get; set; }

    public ITooltipObject ContainedItem      => new ItemTooltipContent(Item, items.GetUnmetRequirements(Item), PriceNote?.Invoke(Item));
    public Vector2        TooltipAnchorPoint => GlobalPosition;

    public ITooltipObject WornCounterpart
        => items.Equipment.GetWornCounterpart(Item) is { } worn
               ? new ItemTooltipContent(worn, items.GetUnmetRequirements(worn)) { ShowsEquippedNote = true }
               : null;

    public event ClickedEventHandler      Clicked;
    public event HoverChangedEventHandler HoverChanged;

    public override void _Ready()
    {
        defaultStylebox   = (StyleBoxFlat)GetThemeStylebox("panel").Duplicate();
        allowedStylebox   = (StyleBoxFlat)defaultStylebox.Duplicate();
        forbiddenStylebox = (StyleBoxFlat)defaultStylebox.Duplicate();

        allowedStylebox.BgColor   = AllowedColor;
        forbiddenStylebox.BgColor = ForbiddenColor;
    }

    public void Init(ItemInstance item, CharacterItems owner, Vector2 slotSize)
    {
        Item       = item;
        items      = owner;
        cellSize   = slotSize;
        stackLabel = GetNode<Label>("%StackLabel");

        GetNode<TextureRect>("%Icon").Texture = ItemLibrary.GetIcon(item);

        CustomMinimumSize = new Vector2(item.Definition.Width, item.Definition.Height) * cellSize;
    }

    public void ShowAt(GridCell cell)
    {
        Position = new Vector2(cell.X, cell.Y) * cellSize;

        stackLabel.Visible = Item.Definition.IsStackable;
        stackLabel.Text    = $"x{Item.StackSize:N0}";
    }

    public GridCell GetCellAt(GridCell rootCell, Vector2 localPosition)
        => new(rootCell.X + Mathf.FloorToInt(localPosition.X / cellSize.X), rootCell.Y + Mathf.FloorToInt(localPosition.Y / cellSize.Y));

    public void _on_gui_input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true } click)
            Clicked?.Invoke(this, click);
    }

    public void _on_mouse_entered()
    {
        HoverChanged?.Invoke(this, true);

        if (Item.Definition.IsEquippable)
            AddThemeStyleboxOverride("panel", items.CanEquip(Item) ? allowedStylebox : forbiddenStylebox);
    }

    public void _on_mouse_exited()
    {
        HoverChanged?.Invoke(this, false);

        AddThemeStyleboxOverride("panel", defaultStylebox);
    }
}
