using System;
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

        var isCritical = critRoll < CombatFormulas.ClampChance(request.CriticalHitChance);
        var wasBlocked = blockRoll < CombatFormulas.ClampChance(defender.GetFinal(GetBlockStat(request.SkillKind)));
        var damage     = Strike(rolledDamage, request.DamageType, request, defender, isCritical, wasBlocked);

        var finalDamage = (int)MathF.Round(Mitigate(damage, request.DamageType, defender));

        //Der Zusatzschaden der Elemente trifft mit demselben Wurf, kritisch und geblockt wie der Hauptteil
        var addedUnmitigated = request.AddedDamage.Select((element, range) => range.IsEmpty
                                                                                  ? 0f
                                                                                  : Strike(Roll(range, damageRoll), element, request, defender, isCritical, wasBlocked));

        var addedDamage  = addedUnmitigated.Select((element, unmitigated) => (int)MathF.Round(Mitigate(unmitigated, element, defender)));
        var addedEffects = addedUnmitigated.Select((element, unmitigated) => StatusEffectRules.GetEffectOfHit(element, unmitigated, addedDamage[element], request.DamageOverTimeMultiplier));

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
            InflictedEffect   = StatusEffectRules.GetEffectOfHit(request.DamageType, damage, finalDamage, request.DamageOverTimeMultiplier),
            AddedDamage       = addedDamage,
            AddedEffects      = addedEffects
        };
    }

    private static float Roll(DamageRange range, float damageRoll)
        => Math.Max(0f, range.Min + damageRoll * (range.Max - range.Min));

    private static float Strike(float rolledDamage, DamageType damageType, HitRequest request, StatSheet defender, bool isCritical, bool wasBlocked)
    {
        var damage = rolledDamage;

        if (isCritical)
            damage *= CombatFormulas.GetCriticalFactor(request.CriticalDamageBonus);

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

    private static float Mitigate(float damage, DamageType damageType, StatSheet defender)
    {
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
