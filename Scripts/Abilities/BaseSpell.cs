using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class BaseSpell : BaseSkill
{
    public BaseSpell(int        baseDamageMin,
                     int        baseDamageMax,
                     int        baseCritRate,
                     double     baseCooldown,
                     DamageType damageType,
                     BaseUnit   owner)
            : base(baseDamageMin, baseDamageMax, baseCritRate, baseCooldown, damageType, owner) { }

    public bool CanFork  { get; }

    public int  MaxForks { get; }
}
