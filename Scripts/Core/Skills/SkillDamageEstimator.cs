using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Rechnet mit Erwartungswerten gegen ein Ziel ohne Verteidigung
public static class SkillDamageEstimator
{
    public static SkillDamageEstimate Estimate(StatSheet       attacker,
                                               WeaponProfile   weapon,
                                               SkillDefinition skill,
                                               float           actionFailureChance = 0f,
                                               bool            paysMana            = true)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(skill);

        var hit           = HitRequests.ForSkill(attacker, weapon, skill);
        var damageFactor  = hit.DamageType.GetDamageFactor();
        var mainMin       = Math.Max(0f, hit.MinDamage) * damageFactor;
        var mainMax       = Math.Max(mainMin, hit.MaxDamage * damageFactor);
        var critChance    = CombatFormulas.ClampChance(hit.CriticalHitChance);
        var critFactor    = CombatFormulas.GetCriticalFactor(hit.CriticalDamageBonus);
        var critShare     = 1f + critChance / 100f * (critFactor - 1f);
        var hitChance     = CombatFormulas.GetHitChance(hit);
        var failureChance = Math.Clamp(actionFailureChance, 0f, 1f);
        var landingShare  = (1f - failureChance) * hitChance / 100f;
        var usesPerSecond = GetUsesPerSecond(attacker, skill);
        var manaPerSecond = paysMana ? (float)(skill.ManaCost * usesPerSecond) : 0f;
        var parts         = GetParts(hit, mainMin, mainMax);
        var minHit        = 0f;
        var maxHit        = 0f;

        foreach (var part in parts)
        {
            minHit += part.Range.Min;
            maxHit += part.Range.Max;
        }

        var averageHit = (minHit + maxHit) / 2f * critShare;

        var estimate = new SkillDamageEstimate
        {
            DamageType          = hit.DamageType,
            MinHit              = minHit,
            MaxHit              = maxHit,
            MaxCriticalHit      = maxHit * critFactor,
            AverageHit          = averageHit,
            HitChance           = hitChance,
            CriticalHitChance   = critChance,
            CriticalDamageBonus = Math.Max(0f, hit.CriticalDamageBonus),
            ActionFailureChance = failureChance,
            UsesPerSecond       = usesPerSecond,
            HitDps              = GetHitDps(averageHit, usesPerSecond * landingShare),
            DamagingEffect      = StatusEffectRules.FindDamageOverTime(hit.DamageType)?.Kind,
            EffectDps           = GetEffectDps(parts, critShare, hit.DamageOverTimeMultiplier, usesPerSecond * landingShare),
            ManaPerSecond       = manaPerSecond
        };

        var manaRegeneration = Math.Max(0f, attacker.GetFinal(CombatStat.Manaregeneration));

        if (!paysMana || skill.ManaCost <= 0f || manaPerSecond <= manaRegeneration)
            return estimate with { SustainedDps = estimate.Dps };

        var sustainedLandings = manaRegeneration / skill.ManaCost * landingShare;

        return estimate with
        {
            IsLimitedByMana = true,
            SustainedDps    = GetHitDps(averageHit, sustainedLandings) + GetEffectDps(parts, critShare, hit.DamageOverTimeMultiplier, sustainedLandings)
        };
    }

    public static double GetUsesPerSecond(StatSheet attacker, SkillDefinition skill)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(skill);

        //Ein Zauber kommt erst wieder, wenn seine Abklingzeit um ist und der Held das Wirken beendet hat
        var intervalSec = skill.Kind == SkillKind.Attack
                ? Math.Max(1.0 / Math.Max(CombatRules.MinAttacksPerSecond, attacker.GetFinal(CombatStat.Attackspeed)), skill.CooldownSec)
                : Math.Max(Math.Max(CombatRules.MinSpellCooldownSec, skill.CooldownSec), skill.CastSec);

        return 1.0 / intervalSec;
    }

    private static float GetHitDps(float averageHit, double landingsPerSecond)
        => (float)(averageHit * landingsPerSecond);

    //Der Hauptteil und der Zusatzschaden je Element, jeder mit seiner Schadensart
    private static List<(DamageType DamageType, DamageRange Range)> GetParts(HitRequest hit, float mainMin, float mainMax)
    {
        var parts = new List<(DamageType, DamageRange)> { (hit.DamageType, new DamageRange(mainMin, mainMax)) };

        foreach (var (element, range) in hit.AddedDamage.Entries)
        {
            if (range.IsEmpty)
                continue;

            var min = Math.Max(0f, range.Min);

            parts.Add((element, new DamageRange(min, Math.Max(min, range.Max))));
        }

        return parts;
    }

    //Jeder Teil löst den Effekt seiner Schadensart aus. Wie viele Instanzen zugleich wirken, bestimmt die Stapelregel des Effekts
    private static float GetEffectDps(List<(DamageType DamageType, DamageRange Range)> parts, float critShare, float damageOverTimeMultiplier, double landingsPerSecond)
    {
        var effectDps = 0f;

        foreach (var (damageType, range) in parts)
        {
            if (StatusEffectRules.FindDamageOverTime(damageType) is not { } dot)
                continue;

            var rule            = StatusEffectRules.Get(dot.Kind);
            var maxInstances    = rule.Stacking == StackingRule.Sum ? rule.MaxInstances : 1;
            var instances       = Math.Min(maxInstances, landingsPerSecond * dot.DurationSec);
            var averageHit      = (range.Min + range.Max) / 2f * critShare;
            var damagePerSecond = averageHit * dot.DamageFraction / dot.DurationSec * damageOverTimeMultiplier;

            effectDps += (float)(damagePerSecond * instances);
        }

        return effectDps;
    }
}
