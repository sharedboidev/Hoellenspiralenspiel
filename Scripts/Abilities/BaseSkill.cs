using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public abstract class BaseSkill
{
    private readonly SpellDefinition definition;

    public BaseSkill(int        baseDamageMin,
                     int        baseDamageMax,
                     int        baseCritRate,
                     double     baseCooldown,
                     DamageType damageType,
                     BaseUnit   owner)
    {
        definition   = new SpellDefinition(GetType().Name, baseDamageMin, baseDamageMax, damageType, baseCritRate);
        RealCooldown = baseCooldown;
        Owner        = owner;
    }

    public BaseUnit Owner        { get; }
    public double   RealCooldown { get; }

    //Würfelt den Treffer über die zentrale Trefferauflösung. Angewendet wird er vom Ziel
    public HitResult MakeRealDamage(BaseUnit target)
        => HitResolver.Resolve(HitRequests.ForSpell(Owner.Stats, definition), target.Stats, GameRandom.Shared);
}
