using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Was ein Angreifer aus seinen Treffern und Kills zurückbekommt. Leben je Treffer und Leech gibt es nur für Angriffe,
//die landen, und zwar für jeden getroffenen Gegner einzeln
public static class HitGains
{
    public static float GetLifeOnHit(StatSheet attacker, HitResult hit)
        => IsLandedAttack(hit) ? Positive(attacker, CombatStat.LifeOnHit) : 0f;

    //Leech saugt aus dem physischen Schaden, der nach der Rüstung ankommt
    public static float GetLifeLeech(StatSheet attacker, HitResult hit)
        => GetLeech(attacker, hit, CombatStat.Leech);

    public static float GetManaLeech(StatSheet attacker, HitResult hit)
        => GetLeech(attacker, hit, CombatStat.ManaLeech);

    public static float GetLifeOnKill(StatSheet attacker)
        => Positive(attacker, CombatStat.LifeOnKill);

    //Reflect gibt dem Nahkämpfer einen Teil des physischen Schadens vor der Minderung zurück, selbst ungemindert
    public static float GetReflectedDamage(StatSheet defender, HitResult hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        return hit.HasLanded && hit.DamageType.IsPhysical() ? hit.UnmitigatedDamage * Positive(defender, CombatStat.ReflectPhysical) / 100f : 0f;
    }

    public static float GetManaOnKill(StatSheet attacker)
        => Positive(attacker, CombatStat.ManaOnKill);

    private static float GetLeech(StatSheet attacker, HitResult hit, CombatStat leechStat)
        => IsLandedAttack(hit) ? hit.PhysicalDamage * Positive(attacker, leechStat) / 100f : 0f;

    private static bool IsLandedAttack(HitResult hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        return hit.HasLanded && hit.SkillKind == SkillKind.Attack;
    }

    private static float Positive(StatSheet attacker, CombatStat stat)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return Math.Max(0f, attacker.GetFinal(stat));
    }
}
