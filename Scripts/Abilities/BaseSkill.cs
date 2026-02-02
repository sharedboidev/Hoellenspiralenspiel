using System;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public abstract class BaseSkill
{
    private readonly decimal baseCritModifier = 1.3m;
    private readonly int     baseCritRate;
    private readonly int     baseDamageMax;
    private readonly int     baseDamageMin;
    private readonly Random  baseDamageRng = new();
    private readonly Random  critRng       = new();

    public BaseSkill(int      baseDamageMin,
                     int      baseDamageMax,
                     int      baseCritRate,
                     double   baseCooldown,
                     BaseUnit owner)
    {
        this.baseDamageMin = baseDamageMin;
        this.baseDamageMax = baseDamageMax;
        this.baseCritRate  = baseCritRate;
        RealCooldown       = baseCooldown;
        Owner              = owner;
    }

    public BaseUnit Owner        { get; }
    public double   RealCooldown { get; }

    //@TODO Nyt do magic
    public HitResult MakeRealDamage(BaseUnit target)
    {
        //HitResult vong schnell her

        var kek = GD.Randf();

        var val              = critRng.Next(1, 101);
        var isCrit           = val <= baseCritRate;
        var rolledBaseDamage = baseDamageRng.Next(baseDamageMin, baseDamageMax + 1);
        var realDamage       = isCrit ? (int)(rolledBaseDamage * baseCritModifier) : rolledBaseDamage;
        var hitType          = isCrit ? HitType.Critical : HitType.Normal;

        //Entweder hier maybe defence vong enemu berücksichtigen

        return new HitResult(realDamage, hitType, LifeModificationMode.Damage);
    }
}