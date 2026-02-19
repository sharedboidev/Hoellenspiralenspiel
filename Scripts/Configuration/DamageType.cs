using Hoellenspiralenspiel.Scripts.Items.Weapons;

namespace Hoellenspiralenspiel.Scripts.Configuration;

public abstract class DamageType
{
    public abstract string Name { get; }

    public virtual void ProvideBonusTo(BaseWeapon weapon) { }
}