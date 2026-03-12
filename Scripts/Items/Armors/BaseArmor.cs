using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items.Armors;

public abstract partial class BaseArmor : BaseItem
{
    [Export]
    public int ArmorvalueBase { get; set; }

    private float ArmorvalueAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Armor);
    private float ArmorvaluePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Armor);
    private float ArmorvalueMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Armor);
    public  int   ArmorvalueFinal                => (int)((ArmorvalueBase + ArmorvalueAddedFlat) * ArmorvaluePercentageMultiplier * ArmorvalueMoreMultiplierTotal);

    protected override void AppendItembaseStats(StringBuilder emil)
        => emil.AppendLine($"Armor: {GetStyledValue(ArmorvalueFinal, ArmorvalueBase)}");

    protected override void SetExceptionalName()
        => ExceptionalName = NameGenerator.GenerateRareArmor();
}