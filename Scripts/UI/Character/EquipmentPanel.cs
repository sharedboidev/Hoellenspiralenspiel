using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
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
            equipmentSlot.Player          =  Player;
            equipmentSlot.MouseMoving     += EquipmentSlotOnMouseMoving;
            equipmentSlot.PropertyChanged += EquipmentSlotOnPropertyChanged;

            slotMap.Add(equipmentSlot.FittingItemSlot, equipmentSlot);
        }
    }

    public int GetTotalIncreasedAttackspeed()
    {
        var ke = (int)equipmentSlots
                     .Where(slot => slot.ContainedItem is not null)
                     .Select(slot => slot.ContainedItem)
                     .Cast<BaseItem>()
                     .Sum(mod => ((1 + mod.GetModifierSumOf(ModificationType.Percentage, CombatStat.Attackspeed)) * mod.GetTotalMoreMultiplierOf(CombatStat.Attackspeed) - 1) * 100);

        return ke;
    }

    public int GetTotalCriticalDamage()
        => 50;

    public float GetTotalMeleeCritChance()
        => (float)(((BaseWeapon)slotMap[ItemSlot.Weapon].ContainedItem)?.CriticalHitChanceFinal ?? 0);

    public float GetTotalDodge()
        => 0f;

    public int GetTotalArmor()
        => equipmentSlots
          .Where(slot => slot.ContainedItem is BaseArmor)
          .Select(slot => slot.ContainedItem)
          .Cast<BaseArmor>()
          .Sum(armor => armor.ArmorvalueFinal);

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
        var fittingSlot          = slotMap[itemToEquip.ItemSlot];
        var formerlyEquippedItem = fittingSlot.RetrieveItem();

        fittingSlot.EquipItem(itemToEquip);

        return formerlyEquippedItem;
    }
}