using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class BaseSpell : BaseSkill
{
    public BaseSpell(int      baseDamageMin,
                     int      baseDamageMax,
                     int      baseCritRate,
                     double   baseCooldown,
                     BaseUnit owner)
            : base(baseDamageMin, baseDamageMax, baseCritRate, baseCooldown, owner) { }

    public bool CanFork  { get; }
    
    public int  MaxForks { get; }
}