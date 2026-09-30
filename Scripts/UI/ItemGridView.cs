using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI;

//Zeigt ein Gitter mit seinen Items. Was ein Klick bewirkt, entscheidet das Fenster, in dem die Ansicht liegt
public partial class ItemGridView : Control
{
    private readonly PackedScene                             itemScene = GD.Load<PackedScene>("res://Scenes/UI/inventory_item.tscn");
    private readonly Dictionary<ItemInstance, InventoryItem> itemViews = new();
    private          InventoryItem                           hoveredView;
    private          GridContainer                           itemGrid;
    private          CharacterItems                          items;
    private          Vector2                                 slotSize;
    private          BaseTooltip                             tooltip;

    [Export]
    public PackedScene SlotScene { get; set; }

    public InventoryGrid Grid { get; private set; }

    public Func<ItemInstance, string> PriceNote { get; set; }

    public event Action<InventoryItem, InputEventMouseButton> ItemClicked;
    public event Action<GridCell>                             SlotClicked;

    private BaseTooltip Tooltip => tooltip ??= GetTree().CurrentScene.GetNodeOrNull<ItemTooltip>("%" + nameof(ItemTooltip));

    //Der Besitzer färbt die Items nach seinen Anforderungen, auch die eines Händlers
    public void Bind(InventoryGrid grid, CharacterItems owner)
    {
        Grid     = grid;
        items    = owner;
        itemGrid = GetNode<GridContainer>("%ItemGrid");

        BuildSlots();
        Refresh();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && !IsVisibleInTree())
            ForgetHover();
    }

    public void Refresh()
    {
        if (Grid is null)
            return;

        foreach (var (item, view) in itemViews.ToArray())
        {
            if (!Grid.Contains(item))
                RemoveView(item, view);
        }

        foreach (var item in Grid.GetItemsInReadingOrder())
        {
            if (!itemViews.TryGetValue(item, out var view))
                view = AddView(item);

            view.ShowAt(Grid.GetPositionOf(item));
        }

        if (hoveredView is not null)
            Tooltip?.Show(hoveredView);
    }

    public GridCell GetCellAt(InventoryItem view, Vector2 localPosition)
        => view.GetCellAt(Grid.GetPositionOf(view.Item), localPosition);

    public InventoryItem GetViewOf(ItemInstance item)
        => itemViews.GetValueOrDefault(item);

    private void BuildSlots()
    {
        itemGrid.Columns = Grid.Width;

        for (var y = 0; y < Grid.Height; y++)
        {
            for (var x = 0; x < Grid.Width; x++)
            {
                var slot = SlotScene.Instantiate<InventorySlot>();

                slot.Cell    =  new GridCell(x, y);
                slot.Clicked += clicked => SlotClicked?.Invoke(clicked.Cell);

                itemGrid.AddChild(slot);

                slotSize = slot.CustomMinimumSize;
            }
        }

        CustomMinimumSize = new Vector2(Grid.Width, Grid.Height) * slotSize;
    }

    private InventoryItem AddView(ItemInstance item)
    {
        var view = itemScene.Instantiate<InventoryItem>();

        view.Init(item, items, slotSize);

        view.PriceNote    =  shown => PriceNote?.Invoke(shown);
        view.Clicked      += (clicked, click) => ItemClicked?.Invoke(clicked, click);
        view.HoverChanged += OnHoverChanged;

        itemViews[item] = view;

        AddChild(view);

        return view;
    }

    private void RemoveView(ItemInstance item, InventoryItem view)
    {
        itemViews.Remove(item);

        if (hoveredView == view)
            ForgetHover();

        view.QueueFree();
    }

    private void OnHoverChanged(InventoryItem view, bool isHovered)
    {
        if (isHovered)
        {
            hoveredView = view;

            Tooltip?.Show(view);
        }
        else if (hoveredView == view)
        {
            ForgetHover();
        }
    }

    private void ForgetHover()
    {
        if (hoveredView is null)
            return;

        hoveredView = null;

        Tooltip?.Hide();
    }
}
