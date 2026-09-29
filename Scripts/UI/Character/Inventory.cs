using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Inventory : PanelContainer
{
    private readonly PackedScene                             itemScene = GD.Load<PackedScene>("res://Scenes/UI/inventory_item.tscn");
    private readonly Dictionary<ItemInstance, InventoryItem> itemViews = new();
    private          InventoryItem                           hoveredView;
    private          GridContainer                           itemGrid;
    private          CharacterItems                          items;
    private          MouseObject                             mouseObject;
    private          Control                                 overlay;
    private          IHero                                   player;
    private          Vector2                                 slotSize;
    private          BaseTooltip                             tooltip;

    [Export]
    public PackedScene SlotScene { get; set; }

    private BaseTooltip Tooltip => tooltip ??= GetTree().CurrentScene.GetNode<ItemTooltip>("%" + nameof(ItemTooltip));

    public void Bind(IHero owner)
    {
        player      = owner;
        items       = owner.Items;
        itemGrid    = GetNode<GridContainer>("%ItemGrid");
        overlay     = GetNode<Control>("MarginContainer/OverlayLayer");
        mouseObject = GetNode<MouseObject>(nameof(MouseObject));

        BuildSlots();

        items.Changed += Refresh;

        Refresh();
    }

    public override void _ExitTree()
    {
        if (items is not null)
            items.Changed -= Refresh;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
            return;

        if (items?.HeldItem is null || !IsOutsideCharacterSheet(click.GlobalPosition))
            return;

        items.DropHeld();

        GetViewport().SetInputAsHandled();
    }

    private bool IsOutsideCharacterSheet(Vector2 globalPosition)
        => !GetParent<Control>().GetGlobalRect().HasPoint(globalPosition);

    private void BuildSlots()
    {
        itemGrid.Columns = items.Inventory.Width;

        for (var y = 0; y < items.Inventory.Height; y++)
        {
            for (var x = 0; x < items.Inventory.Width; x++)
            {
                var slot = SlotScene.Instantiate<InventorySlot>();

                slot.Cell    =  new GridCell(x, y);
                slot.Clicked += OnSlotClicked;

                itemGrid.AddChild(slot);

                slotSize = slot.CustomMinimumSize;
            }
        }
    }

    private void Refresh()
    {
        foreach (var (item, view) in itemViews.ToArray())
        {
            if (!items.Inventory.Contains(item))
                RemoveView(item, view);
        }

        foreach (var item in items.Inventory.GetItemsInReadingOrder())
        {
            if (!itemViews.TryGetValue(item, out var view))
                view = AddView(item);

            view.ShowAt(items.Inventory.GetPositionOf(item));
        }

        mouseObject.ShowItem(items.HeldItem);

        if (hoveredView is not null)
            Tooltip.Show(hoveredView);
    }

    private InventoryItem AddView(ItemInstance item)
    {
        var view = itemScene.Instantiate<InventoryItem>();

        view.Init(item, items, slotSize);

        view.Clicked      += OnItemClicked;
        view.HoverChanged += OnHoverChanged;

        itemViews[item] = view;

        overlay.AddChild(view);

        return view;
    }

    private void RemoveView(ItemInstance item, InventoryItem view)
    {
        itemViews.Remove(item);

        if (hoveredView == view)
        {
            hoveredView = null;

            Tooltip.Hide();
        }

        view.QueueFree();
    }

    private void OnSlotClicked(InventorySlot slot)
        => items.PlaceHeldAt(slot.Cell);

    private void OnItemClicked(InventoryItem view, MouseButton button, Vector2 localPosition)
    {
        switch (button)
        {
            case MouseButton.Left when items.HeldItem is null:
                items.TakeFromInventory(view.Item);

                break;
            case MouseButton.Left:
                items.PlaceHeldAt(view.GetCellAt(items.Inventory.GetPositionOf(view.Item), localPosition));

                break;
            case MouseButton.Right when view.Item.Definition.Consumable is not null:
                player.Consume(view.Item);

                break;
            case MouseButton.Right:
                items.EquipFromInventory(view.Item);

                break;
        }
    }

    private void OnHoverChanged(InventoryItem view, bool isHovered)
    {
        if (isHovered)
        {
            hoveredView = view;

            Tooltip.Show(view);
        }
        else if (hoveredView == view)
        {
            hoveredView = null;

            Tooltip.Hide();
        }
    }
}
