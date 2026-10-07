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

        var hit            = HitRequests.ForSkill(attacker, weapon, skill);
        var main           = Measure(hit);
        var critFactor     = CombatFormulas.GetCriticalFactor(hit.CriticalDamageBonus);
        var failureChance  = Math.Clamp(actionFailureChance, 0f, 1f);
        var landingShare   = (1f - failureChance) * main.HitChance / 100f;
        var usesPerSecond  = GetUsesPerSecond(attacker, skill);
        var manaPerSecond  = paysMana ? GetManaPerSecond(skill, usesPerSecond) : 0f;
        var arrowCount     = skill.Rain?.GetCount(BonusProjectiles.ForBow(attacker, weapon)) ?? 0;
        var arrowsOnTarget = arrowCount * (skill.Rain?.GetShareOnCenter() ?? 0f);
        var scatter        = MeasureScatter(attacker, weapon, skill);
        var scatterCount   = scatter is null ? 0 : skill.Scatter.GetCount(BonusProjectiles.FromStats(attacker));

        //Bei einem Pfeilregen ist der Haupttreffer ein Pfeil, und nur ein Teil der Pfeile erreicht ein Ziel in der Mitte
        var hits = new List<(MeasuredHit Hit, float LandingsPerUse)> { (main, skill.Rain is null ? landingShare : landingShare * arrowsOnTarget) };

        //Die Kugeln springen nur aus einem gelandeten Treffer. Mit einem Ziel trifft es jede von ihnen
        if (scatter is not null)
            hits.Add((scatter, landingShare * scatterCount * scatter.HitChance / 100f));

        var estimate = new SkillDamageEstimate
        {
            DamageType          = hit.DamageType,
            MinHit              = main.MinHit,
            MaxHit              = main.MaxHit,
            MaxCriticalHit      = main.MaxHit * critFactor,
            AverageHit          = main.AverageHit,
            HitChance           = main.HitChance,
            CriticalHitChance   = CombatFormulas.ClampChance(hit.CriticalHitChance),
            CriticalDamageBonus = Math.Max(0f, hit.CriticalDamageBonus),
            ActionFailureChance = failureChance,
            UsesPerSecond       = usesPerSecond,
            HitDps              = GetHitDps(hits, usesPerSecond),
            DamagingEffect      = StatusEffectRules.FindDamageOverTime(hit.DamageType)?.Kind,
            EffectDps           = GetEffectDps(hits, usesPerSecond),
            ScatterCount        = scatterCount,
            ScatterAverageHit   = scatter?.AverageHit ?? 0f,
            ArrowCount          = arrowCount,
            ArrowsOnTarget      = arrowsOnTarget,
            ManaPerSecond       = manaPerSecond
        };

        var manaRegeneration = Math.Max(0f, attacker.GetFinal(CombatStat.Manaregeneration));

        if (!paysMana || manaPerSecond <= 0f || manaPerSecond <= manaRegeneration)
            return estimate with { SustainedDps = estimate.Dps };

        var sustainedUses = usesPerSecond * manaRegeneration / manaPerSecond;

        return estimate with
        {
            IsLimitedByMana = true,
            SustainedDps    = GetHitDps(hits, sustainedUses) + GetEffectDps(hits, sustainedUses)
        };
    }

    public static double GetUsesPerSecond(StatSheet attacker, SkillDefinition skill)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(skill);

        //Ein Zauber kommt erst wieder, wenn seine Abklingzeit um ist und der Held das Wirken beendet hat
        var intervalSec = skill.Kind == SkillKind.Attack
                ? Math.Max(GetAttackCycleSec(attacker, skill), skill.CooldownSec)
                : Math.Max(Math.Max(CombatRules.MinSpellCooldownSec, skill.CooldownSec), skill.GetCastSec(attacker));

        return 1.0 / intervalSec;
    }

    //Ein geladener Schuss lädt voll, statt auszuholen, schneller mit erhöhtem Angriffstempo. Danach erholt der Held sich wie nach jedem Angriff.
    //Ein Wirbel trifft je Tick, sein Einsatz ist der Tick
    private static double GetAttackCycleSec(StatSheet attacker, SkillDefinition skill)
    {
        var swingSec = 1.0 / Math.Max(CombatRules.MinAttacksPerSecond, attacker.GetFinal(CombatStat.Attackspeed));

        if (skill.IsChanneled)
            return skill.Channel.GetIntervalSec(ChannelSettings.GetAttacksPerSec(attacker));

        return skill.IsCharged
                ? skill.Charge.GetSecToReach(ChargeSettings.FullPercent, ChargeSettings.GetRateFactor(attacker)) + swingSec * (1 - CombatRules.ActionImpactFraction)
                : swingSec;
    }

    //Ein Wirbel kostet je Sekunde, alles andere je Einsatz
    private static float GetManaPerSecond(SkillDefinition skill, double usesPerSecond)
        => skill.IsChanneled ? Math.Max(0f, skill.Channel.ManaPerSec) : (float)(skill.ManaCost * usesPerSecond);

    private static MeasuredHit MeasureScatter(StatSheet attacker, WeaponProfile weapon, SkillDefinition skill)
    {
        if (skill.Scatter is not { Count: > 0 } scatter || skill.Attack is null)
            return null;

        return Measure(HitRequests.ForAttack(attacker, weapon, scatter.GetAttack(skill.Attack)));
    }

    private static MeasuredHit Measure(HitRequest hit)
    {
        var damageFactor = hit.DamageType.GetDamageFactor();
        var mainMin      = Math.Max(0f, hit.MinDamage) * damageFactor;
        var mainMax      = Math.Max(mainMin, hit.MaxDamage * damageFactor);
        var critChance   = CombatFormulas.ClampChance(hit.CriticalHitChance);
        var critFactor   = CombatFormulas.GetCriticalFactor(hit.CriticalDamageBonus);
        var critShare    = 1f + critChance / 100f * (critFactor - 1f);
        var parts        = GetParts(hit, mainMin, mainMax);
        var minHit       = 0f;
        var maxHit       = 0f;

        foreach (var part in parts)
        {
            minHit += part.Range.Min;
            maxHit += part.Range.Max;
        }

        return new MeasuredHit(hit, parts, critShare, CombatFormulas.GetHitChance(hit), minHit, maxHit, (minHit + maxHit) / 2f * critShare);
    }

    private static float GetHitDps(List<(MeasuredHit Hit, float LandingsPerUse)> hits, double usesPerSecond)
    {
        var hitDps = 0f;

        foreach (var (hit, landingsPerUse) in hits)
            hitDps += (float)(hit.AverageHit * usesPerSecond * landingsPerUse);

        return hitDps;
    }

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

    //Jeder Teil löst den Effekt seiner Schadensart aus. Wie viele Instanzen zugleich wirken, bestimmt die Stapelregel des Effekts,
    //für alle Treffer eines Skills zusammen. Eine Instanz wirkt im Mittel so stark wie die Treffer, aus denen sie entsteht
    private static float GetEffectDps(List<(MeasuredHit Hit, float LandingsPerUse)> hits, double usesPerSecond)
    {
        var byEffect = new Dictionary<DamageOverTimeRule, (double Landings, double WeightedDamagePerSecond)>();

        foreach (var (hit, landingsPerUse) in hits)
        {
            var landings = usesPerSecond * landingsPerUse;

            foreach (var (damageType, range) in hit.Parts)
            {
                if (StatusEffectRules.FindDamageOverTime(damageType) is not { } dot)
                    continue;

                var averageHit      = (range.Min + range.Max) / 2f * hit.CritShare;
                var damagePerSecond = averageHit * dot.DamageFraction / dot.DurationSec * (hit.Request.DamageOverTimeMultiplier * hit.Request.DamageOverTimeByType.For(damageType));
                var sum             = byEffect.GetValueOrDefault(dot);

                byEffect[dot] = (sum.Landings + landings, sum.WeightedDamagePerSecond + damagePerSecond * landings);
            }
        }

        var effectDps = 0f;

        foreach (var (dot, (landings, weightedDamagePerSecond)) in byEffect)
        {
            if (landings <= 0)
                continue;

            var rule         = StatusEffectRules.Get(dot.Kind);
            var maxInstances = rule.Stacking == StackingRule.Sum ? rule.MaxInstances : 1;
            var instances    = Math.Min(maxInstances, landings * dot.DurationSec);

            effectDps += (float)(weightedDamagePerSecond / landings * instances);
        }

        return effectDps;
    }

    private sealed record MeasuredHit(HitRequest                                       Request,
                                      List<(DamageType DamageType, DamageRange Range)> Parts,
                                      float                                            CritShare,
                                      float                                            HitChance,
                                      float                                            MinHit,
                                      float                                            MaxHit,
                                      float                                            AverageHit);
}
