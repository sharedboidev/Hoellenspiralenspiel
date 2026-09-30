using System;
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
        var minHit        = Math.Max(0f, hit.MinDamage) * damageFactor;
        var maxHit        = Math.Max(minHit, hit.MaxDamage * damageFactor);
        var critChance    = CombatFormulas.ClampChance(hit.CriticalHitChance);
        var critFactor    = CombatFormulas.GetCriticalFactor(hit.CriticalDamageBonus);
        var averageHit    = (minHit + maxHit) / 2f * (1f + critChance / 100f * (critFactor - 1f));
        var hitChance     = CombatFormulas.GetHitChance(hit);
        var failureChance = Math.Clamp(actionFailureChance, 0f, 1f);
        var landingShare  = (1f - failureChance) * hitChance / 100f;
        var usesPerSecond = GetUsesPerSecond(attacker, skill);
        var manaPerSecond = paysMana ? (float)(skill.ManaCost * usesPerSecond) : 0f;
        var effect        = GetDamagingEffect(hit.DamageType);

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
            DamagingEffect      = effect,
            EffectDps           = GetEffectDps(effect, averageHit, usesPerSecond * landingShare),
            ManaPerSecond       = manaPerSecond
        };

        var manaRegeneration = Math.Max(0f, attacker.GetFinal(CombatStat.Manaregeneration));

        if (!paysMana || skill.ManaCost <= 0f || manaPerSecond <= manaRegeneration)
            return estimate with { SustainedDps = estimate.Dps };

        var sustainedLandings = manaRegeneration / skill.ManaCost * landingShare;

        return estimate with
        {
            IsLimitedByMana = true,
            SustainedDps    = GetHitDps(averageHit, sustainedLandings) + GetEffectDps(effect, averageHit, sustainedLandings)
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

    private static StatusEffectKind? GetDamagingEffect(DamageType damageType)
        => damageType switch
        {
            DamageType.Slash => StatusEffectKind.Bleed,
            DamageType.Fire  => StatusEffectKind.Burn,
            _                => null
        };

    //Wie viele Instanzen zugleich wirken, bestimmt die Stapelregel des Effekts
    private static float GetEffectDps(StatusEffectKind? effect, float averageHit, double landingsPerSecond)
    {
        if (effect is null)
            return 0f;

        var (damageFraction, durationSec) = effect == StatusEffectKind.Bleed
                ? (CombatRules.BleedDamageFraction, CombatRules.BleedDurationSec)
                : (CombatRules.BurnDamageFraction, CombatRules.BurnDurationSec);

        var rule            = StatusEffectRules.Get(effect.Value);
        var maxInstances    = rule.Stacking == StackingRule.Sum ? rule.MaxInstances : 1;
        var instances       = Math.Min(maxInstances, landingsPerSecond * durationSec);
        var damagePerSecond = averageHit * damageFraction / durationSec;

        return (float)(damagePerSecond * instances);
    }
}
