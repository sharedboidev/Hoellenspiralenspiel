using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors.Gloves;

public abstract partial class BaseGloves : BaseArmor
{
    public override bool     IsStackable => false;
    public override ItemSlot ItemSlot    => ItemSlot.Hands;
}