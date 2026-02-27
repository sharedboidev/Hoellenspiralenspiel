using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class EquipmentPanel : PanelContainer
{
    private readonly Dictionary<ItemSlot, EquipmentSlot> slotMap = new();

    public delegate void ItemEquippedEventHandler();

    public event ItemEquippedEventHandler ItemEquipped;
    [Export]
    public Inventory Inventory { get; set; }
    

    private BaseTooltip Tooltip => GetTree().CurrentScene.GetNode<ItemTooltip>("%" + nameof(ItemTooltip));

    public override void _Ready()
    {
        foreach (var equipmentSlot in this.GetAllChildren<EquipmentSlot>())
        {
            equipmentSlot.MouseMoving += EquipmentSlotOnMouseMoving;
            equipmentSlot.PropertyChanged += EquipmentSlotOnPropertyChanged;
            
            slotMap.Add(equipmentSlot.FittingItemSlot, equipmentSlot);
        }
    }

    private void EquipmentSlotOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        //todo new/old value verlgeich, dann equipped Invoken   
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