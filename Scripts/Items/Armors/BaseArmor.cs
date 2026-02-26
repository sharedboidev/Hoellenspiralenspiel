using Godot;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors;

public abstract partial class BaseArmor : BaseItem
{
    [Export]
    public int ArmorvalueBase { get; set; }

    private float ArmorvalueAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Armor);
    private float ArmorvaluePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Armor);
    private float ArmorvalueMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Armor);
    public  int   ArmorvalueFinal                => (int)((ArmorvalueBase + ArmorvalueAddedFlat) * ArmorvaluePercentageMultiplier * ArmorvalueMoreMultiplierTotal);
}