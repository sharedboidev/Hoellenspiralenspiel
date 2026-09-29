using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class EquipmentPanel : PanelContainer
{
    private EquipmentSlot[] equipmentSlots = [];
    private EquipmentSlot   hoveredSlot;
    private CharacterItems  items;
    private BaseTooltip     tooltip;

    private BaseTooltip Tooltip => tooltip ??= GetTree().CurrentScene.GetNodeOrNull<ItemTooltip>("%" + nameof(ItemTooltip));

    public void Bind(CharacterItems owner)
    {
        items          = owner;
        equipmentSlots = this.GetAllChildren<EquipmentSlot>();

        foreach (var equipmentSlot in equipmentSlots)
        {
            equipmentSlot.Bind(items);

            equipmentSlot.HoverChanged += OnHoverChanged;
        }

        items.Changed += Refresh;

        Refresh();
    }

    public override void _ExitTree()
    {
        if (items is not null)
            items.Changed -= Refresh;
    }

    private void Refresh()
    {
        foreach (var equipmentSlot in equipmentSlots)
            equipmentSlot.Refresh();

        if (hoveredSlot is not null)
            ShowTooltipOf(hoveredSlot);
    }

    private void OnHoverChanged(EquipmentSlot equipmentSlot, bool isHovered)
    {
        if (isHovered)
        {
            hoveredSlot = equipmentSlot;

            ShowTooltipOf(equipmentSlot);
        }
        else if (hoveredSlot == equipmentSlot)
        {
            hoveredSlot = null;

            Tooltip?.Hide();
        }
    }

    private void ShowTooltipOf(EquipmentSlot equipmentSlot)
    {
        if (equipmentSlot.IsEmpty)
            Tooltip?.Hide();
        else
            Tooltip?.Show(equipmentSlot);
    }
}
