using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors.Helmets;

public abstract partial class BaseHelmet : BaseArmor
{
    public override bool     IsStackable => false;
    public override ItemSlot ItemSlot    => ItemSlot.Helmet;
}