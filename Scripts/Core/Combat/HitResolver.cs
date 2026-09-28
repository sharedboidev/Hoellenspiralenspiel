using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Die zentrale Trefferauflösung. Reihenfolge: Treffen, Ausweichen, Parry, Block, Krit, Minderung.
//Angewendet wird das Ergebnis von der Einheit, die getroffen wurde
public static class HitResolver
{
    public static HitResult Resolve(HitRequest request, StatSheet defender, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(defender);
        ArgumentNullException.ThrowIfNull(random);

        //Alle Würfe fallen vorab und in fester Reihenfolge. So verbraucht jeder Treffer gleich viele Würfe,
        //und derselbe Seed ergibt denselben Kampf, egal wie die einzelnen Treffer ausgehen
        var hitRoll    = random.NextPercent();
        var dodgeRoll  = random.NextPercent();
        var parryRoll  = random.NextPercent();
        var blockRoll  = random.NextPercent();
        var critRoll   = random.NextPercent();
        var damageRoll = random.NextFloat();

        var rolledDamage = Math.Max(0f, request.MinDamage + damageRoll * (request.MaxDamage - request.MinDamage));
        var avoidance    = GetAvoidance(request, defender, hitRoll, dodgeRoll, parryRoll);

        if (avoidance != HitAvoidance.None)
        {
            return new HitResult
            {
                DamageType   = request.DamageType,
                SkillKind    = request.SkillKind,
                Avoidance    = avoidance,
                RolledDamage = rolledDamage
            };
        }

        var isCritical = critRoll < CombatFormulas.ClampChance(request.CriticalHitChance);
        var wasBlocked = blockRoll < CombatFormulas.ClampChance(defender.GetFinal(GetBlockStat(request.SkillKind)));
        var damage     = rolledDamage;

        if (isCritical)
            damage *= 1f + Math.Max(0f, request.CriticalDamageBonus) / 100f;

        if (request.DamageType == DamageType.Crush)
            damage *= 1f + CombatRules.CrushMoreDamage;

        if (wasBlocked)
            damage *= 1f - CombatFormulas.ClampChance(defender.GetFinal(CombatStat.BlockReduction)) / 100f;

        var finalDamage = (int)MathF.Round(Mitigate(damage, request.DamageType, defender));

        return new HitResult
        {
            DamageType        = request.DamageType,
            SkillKind         = request.SkillKind,
            Avoidance         = HitAvoidance.None,
            WasBlocked        = wasBlocked,
            IsCritical        = isCritical,
            RolledDamage      = rolledDamage,
            UnmitigatedDamage = damage,
            FinalDamage       = finalDamage,
            InflictedEffect   = StatusEffectRules.GetEffectOfHit(request.DamageType, damage, finalDamage)
        };
    }

    private static HitAvoidance GetAvoidance(HitRequest request,
                                             StatSheet  defender,
                                             float      hitRoll,
                                             float      dodgeRoll,
                                             float      parryRoll)
    {
        var hitChance = request.HitChance;

        if (request.DamageType == DamageType.Pierce)
            hitChance *= 1f - CombatRules.PierceLessHitChance;

        if (hitRoll >= CombatFormulas.ClampChance(hitChance))
            return HitAvoidance.Missed;

        if (dodgeRoll < CombatFormulas.ClampChance(defender.GetFinal(CombatStat.Dodge)))
            return HitAvoidance.Dodged;

        if (parryRoll < CombatFormulas.ClampChance(defender.GetFinal(GetParryStat(request.SkillKind))))
            return HitAvoidance.Parried;

        return HitAvoidance.None;
    }

    private static float Mitigate(float damage, DamageType damageType, StatSheet defender)
    {
        //Pierce ignoriert die Rüstung
        if (damageType == DamageType.Pierce)
            return Math.Max(0f, damage);

        var mitigatingValue = defender.GetFinal(damageType.GetMitigatingStat());

        return damageType.IsPhysical()
                ? CombatFormulas.MitigateByArmor(damage, mitigatingValue)
                : CombatFormulas.MitigateByResistance(damage, mitigatingValue);
    }

    private static CombatStat GetParryStat(SkillKind skillKind)
        => skillKind == SkillKind.Spell ? CombatStat.SpellParry : CombatStat.MeleeParry;

    private static CombatStat GetBlockStat(SkillKind skillKind)
        => skillKind == SkillKind.Spell ? CombatStat.SpellBlock : CombatStat.MeleeBlock;
}
