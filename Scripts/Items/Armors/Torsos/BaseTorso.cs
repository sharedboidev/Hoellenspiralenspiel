using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Items.Armors.Torsos;

public abstract partial class BaseTorso : BaseArmor
{
    public override bool     IsStackable => false;
    public override ItemSlot ItemSlot    => ItemSlot.Torso;
}