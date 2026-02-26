using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class EquipmentPanel : PanelContainer
{
    private Dictionary<ItemSlot, EquipmentSlot> slotMap = new();

    [Export]
    public Inventory Inventory { get; set; }

    private BaseTooltip Tooltip => GetTree().CurrentScene.GetNode<ItemTooltip>("%" + nameof(ItemTooltip));

    public override void _Ready()
    {
        foreach (var equipmentSlot in this.GetAllChildren<EquipmentSlot>())
        {
            equipmentSlot.MouseMoving += EquipmentSlotOnMouseMoving;
            slotMap.Add(equipmentSlot.FittingItemSlot, equipmentSlot);
        }
    }

    private void EquipmentSlotOnMouseMoving(MousemovementDirection mousemovementdirection, EquipmentSlot equipmentslot)
    {
        switch (mousemovementdirection)
        {
            case MousemovementDirection.Entered:
                if (equipmentslot.IsEmpty)
                    return;

                Tooltip.Show(equipmentslot);

                break;
            case MousemovementDirection.Left:
                if (equipmentslot.IsEmpty)

                    return;

                Tooltip.Hide();

                break;
        }
    }

    public BaseItem EquipIntoFittingSlot(BaseItem itemToEquip)
    {
        var fittingSlot          = slotMap[itemToEquip.ItemSlot];
        var formerlyEquippedItem = fittingSlot.RetrieveItem();

        fittingSlot.EquipItem(itemToEquip);

        return formerlyEquippedItem;
    }
}