using System.Text;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors.Torsos;

public abstract partial class BaseTorso : BaseArmor
{
    public override bool     IsStackable  => false;
    public override ItemSlot ItemSlot     => ItemSlot.Torso;

    protected override void AppendItembaseStats(StringBuilder emil)
    => emil.AppendLine($"Armor: {GetStyledValue(ArmorvalueFinal, ArmorvalueBase)}");
}