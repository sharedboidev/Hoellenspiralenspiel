using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public static class HitResolver
{
    public static HitResult Resolve(HitRequest request, StatSheet defender, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(defender);
        ArgumentNullException.ThrowIfNull(random);

        //Alle Würfe fallen vorab in fester Reihenfolge, damit derselbe Seed denselben Kampf ergibt, egal wie die Treffer ausgehen
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

        //Ein Treffer mit Feuer löst Brittle, bevor dessen Mehrschaden zählt
        var removed    = StatusEffectRules.GetRemovedBy(request);
        var isCritical = critRoll < CombatFormulas.ClampChance(request.CriticalHitChance);
        var wasBlocked = blockRoll < CombatFormulas.ClampChance(defender.GetFinal(GetBlockStat(request.SkillKind)));
        var critFactor = isCritical ? CombatFormulas.GetCriticalFactor(Defences.GetCriticalDamageBonusTaken(defender, request.CriticalDamageBonus)) : 1f;
        var damage     = Strike(rolledDamage, request.DamageType, defender, critFactor, wasBlocked);

        var finalDamage = (int)MathF.Round(Mitigate(damage, request.DamageType, defender, removed));

        //Der Zusatzschaden der Elemente trifft mit demselben Wurf, kritisch und geblockt wie der Hauptteil
        var addedUnmitigated = request.AddedDamage.Select((element, range) => range.IsEmpty
                                                                                  ? 0f
                                                                                  : Strike(Roll(range, damageRoll), element, defender, critFactor, wasBlocked));

        var addedDamage  = addedUnmitigated.Select((element, unmitigated) => (int)MathF.Round(Mitigate(unmitigated, element, defender, removed)));
        var addedEffects = addedUnmitigated.Select((element, unmitigated) => StatusEffectRules.GetEffectOfHit(element, unmitigated, addedDamage[element], GetDamageOverTimeMultiplier(request, element)));

        return new HitResult
        {
            DamageType        = request.DamageType,
            SkillKind         = request.SkillKind,
            Avoidance         = HitAvoidance.None,
            WasBlocked        = wasBlocked,
            IsCritical        = isCritical,
            RolledDamage      = rolledDamage,
            UnmitigatedDamage = damage,
            FinalDamage       = finalDamage + addedDamage.Fire + addedDamage.Frost + addedDamage.Lightning,
            InflictedEffect   = StatusEffectRules.GetEffectOfHit(request.DamageType, damage, finalDamage, GetDamageOverTimeMultiplier(request, request.DamageType)),
            AddedDamage       = addedDamage,
            AddedEffects      = addedEffects,
            RemovedEffects    = removed
        };
    }

    private static float Roll(DamageRange range, float damageRoll)
        => Math.Max(0f, range.Min + damageRoll * (range.Max - range.Min));

    //Der allgemeine Multiplikator und der der Schadensart, beides More
    private static float GetDamageOverTimeMultiplier(HitRequest request, DamageType damageType)
        => request.DamageOverTimeMultiplier * request.DamageOverTimeByType.For(damageType);

    private static float Strike(float rolledDamage, DamageType damageType, StatSheet defender, float critFactor, bool wasBlocked)
    {
        var damage = rolledDamage;

        if (critFactor != 1f)
            damage *= critFactor;

        damage *= damageType.GetDamageFactor();

        if (wasBlocked)
            damage *= 1f - CombatFormulas.ClampChance(defender.GetFinal(CombatStat.BlockReduction)) / 100f;

        return damage;
    }

    private static HitAvoidance GetAvoidance(HitRequest request,
                                             StatSheet  defender,
                                             float      hitRoll,
                                             float      dodgeRoll,
                                             float      parryRoll)
    {
        if (hitRoll >= CombatFormulas.GetHitChance(request))
            return HitAvoidance.Missed;

        if (dodgeRoll < CombatFormulas.ClampChance(defender.GetFinal(CombatStat.Dodge)))
            return HitAvoidance.Dodged;

        if (parryRoll < CombatFormulas.ClampChance(defender.GetFinal(GetParryStat(request.SkillKind))))
            return HitAvoidance.Parried;

        return HitAvoidance.None;
    }

    //Pierce geht an der Rüstung vorbei, die zusätzliche Minderung physischen Schadens trifft ihn trotzdem
    private static float Mitigate(float damage, DamageType damageType, StatSheet defender, IReadOnlyList<StatusEffectKind> removedEffects)
    {
        if (damageType.IsElemental())
            return CombatFormulas.MitigateByResistance(damage, Defences.GetEffectiveResistance(defender, damageType));

        var afterArmor = damageType == DamageType.Pierce
                ? Math.Max(0f, damage)
                : CombatFormulas.MitigateByArmor(damage, defender.GetFinal(CombatStat.Armor));

        return afterArmor * (1f - Defences.GetPhysicalDamageReduction(defender) / 100f) * Defences.GetDamageTakenFactor(defender, damageType, removedEffects);
    }

    private static CombatStat GetParryStat(SkillKind skillKind)
        => skillKind == SkillKind.Spell ? CombatStat.SpellParry : CombatStat.MeleeParry;

    private static CombatStat GetBlockStat(SkillKind skillKind)
        => skillKind == SkillKind.Spell ? CombatStat.SpellBlock : CombatStat.MeleeBlock;
}
