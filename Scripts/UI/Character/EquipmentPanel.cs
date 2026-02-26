using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class EquipmentPanel : PanelContainer
{
    private Dictionary<ItemType, EquipmentSlot> slotMap = new();

    [Export]
    public Inventory Inventory { get; set; }

    public override void _Ready()
    {
        var alleMeineKinder = this.GetAllChildren<EquipmentSlot>();
        slotMap = alleMeineKinder.ToDictionary(slot => slot.FittingItemType, slot => slot);
    }

    public BaseItem EquipIntoFittingSlot(BaseItem itemToEquip)
    {
        var fittingSlot          = slotMap[itemToEquip.ItemType];
        var formerlyEquippedItem = fittingSlot.RetrieveItem();

        fittingSlot.EquipItem(itemToEquip);

        return formerlyEquippedItem;
    }
}