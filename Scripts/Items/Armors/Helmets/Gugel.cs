using System.Text;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors.Helmets;

public partial class Gugel : BaseArmor
{
    public override bool     IsStackable  => false;
    public override string   ItembaseName => nameof(Gugel);
    public override ItemSlot ItemSlot     => ItemSlot.Helmet;

    protected override void AppendItembaseStats(StringBuilder emil)
        => emil.AppendLine($"Armor: {GetStyledValue(ArmorvalueFinal, ArmorvalueBase)}");
}