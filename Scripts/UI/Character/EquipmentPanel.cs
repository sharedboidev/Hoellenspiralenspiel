using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.UI.Tooltips;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils.EventArgs;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class EquipmentPanel : PanelContainer
{
    public delegate void EquipmentChangedEventHandler(object formerlyEqipped, object newlyEquipped);

    private readonly Dictionary<ItemSlot, EquipmentSlot> slotMap = new();
    private          EquipmentSlot[]                     equipmentSlots;

    [Export]
    public Inventory Inventory { get; set; }

    private Player2D    Player  => GetTree().CurrentScene.GetNodeOrNull<Player2D>("%Player 2D");
    private BaseTooltip Tooltip => GetTree().CurrentScene.GetNodeOrNull<ItemTooltip>("%" + nameof(ItemTooltip));

    public event EquipmentChangedEventHandler EquipmentChanged;

    public override void _Ready()
    {
        equipmentSlots = this.GetAllChildren<EquipmentSlot>();

        foreach (var equipmentSlot in equipmentSlots)
        {
            foreach (var fittingItemType in equipmentSlot.FittingItemSlot)
            {
                equipmentSlot.Player          =  Player;
                equipmentSlot.MouseMoving     += EquipmentSlotOnMouseMoving;
                equipmentSlot.PropertyChanged += EquipmentSlotOnPropertyChanged;

                slotMap.Add(fittingItemType, equipmentSlot);
            }
        }
    }

    private void EquipmentSlotOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e is not CustomPropertyChangedEventArgs customArg)
            return;

        if (customArg.OldValue != customArg.NewValue)
            EquipmentChanged?.Invoke(customArg.OldValue, customArg.NewValue);
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
                Tooltip.Hide();

                break;
        }
    }

    public BaseItem EquipIntoFittingSlot(BaseItem itemToEquip)
    {
        if (itemToEquip is null)
            return null; 
                    
        var fittingSlot          = slotMap[itemToEquip.ItemSlot];
        var formerlyEquippedItem = fittingSlot.RetrieveItem();

        fittingSlot.EquipItem(itemToEquip);

        return formerlyEquippedItem;
    }
}