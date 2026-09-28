using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public abstract class BaseSkill
{
    private readonly decimal    baseCritModifier = 1.3m;
    private readonly int        baseCritRate;
    private readonly int        baseDamageMax;
    private readonly int        baseDamageMin;
    private readonly Random     baseDamageRng = new();
    private readonly Random     critRng       = new();
    private readonly CombatStat mitigatedBy;

    public BaseSkill(int        baseDamageMin,
                     int        baseDamageMax,
                     int        baseCritRate,
                     double     baseCooldown,
                     CombatStat mitigatedBy,
                     BaseUnit   owner)
    {   
        this.baseDamageMin = baseDamageMin;
        this.baseDamageMax = baseDamageMax;
        this.baseCritRate  = baseCritRate;
        this.mitigatedBy   = mitigatedBy;
        RealCooldown       = baseCooldown;
        Owner              = owner;
    }

    public BaseUnit Owner        { get; }
    public double   RealCooldown { get; }

    public HitResult MakeRealDamage(BaseUnit target)
    {
        var val              = critRng.Next(0, 101);
        var isCrit           = val <= baseCritRate;
        var rolledBaseDamage = (float)baseDamageRng.Next(baseDamageMin, baseDamageMax + 1);

        if (this is BaseSpell)
        {
            rolledBaseDamage += Owner.Stats.GetAddedFlat(CombatStat.SpellDamage);
            rolledBaseDamage *= Owner.Stats.GetTotalMultiplier(CombatStat.SpellDamage);
        }

        var realDamage       = isCrit ? rolledBaseDamage * (float)baseCritModifier : rolledBaseDamage;
        var hitType          = isCrit ? HitType.Critical : HitType.Normal;

        return new HitResult(realDamage, hitType, LifeModificationMode.Damage, target, mitigatedBy);
    }
}