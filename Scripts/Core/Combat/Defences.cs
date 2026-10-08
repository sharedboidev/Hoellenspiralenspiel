using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Was der Verteidiger einem Treffer entgegensetzt, über Rüstung und Resistenz hinaus
public static class Defences
{
    //Das Maximum beginnt bei 75 % und wächst mit den Affixen, mehr als 90 % zählt nie
    public static float GetMaximumResistance(StatSheet defender, DamageType element)
    {
        ArgumentNullException.ThrowIfNull(defender);

        return Math.Min(CombatRules.BaseMaximumResistance + defender.GetFinal(element.GetMaximumResistanceStat()), CombatRules.ResistanceHardCap);
    }

    public static float GetEffectiveResistance(StatSheet defender, DamageType element)
    {
        ArgumentNullException.ThrowIfNull(defender);

        return Math.Min(defender.GetFinal(element.GetMitigatingStat()), GetMaximumResistance(defender, element));
    }

    public static float GetPhysicalDamageReduction(StatSheet defender)
    {
        ArgumentNullException.ThrowIfNull(defender);

        return Math.Clamp(defender.GetFinal(CombatStat.Damagereduction), 0f, CombatRules.MaxPhysicalDamageReduction);
    }

    //Brittle und Ähnliches heben den physischen Schaden, den die Einheit nimmt. Ein Treffer, der einen Effekt löst, zählt dessen Anteil nicht mehr
    public static float GetDamageTakenFactor(StatSheet defender, DamageType damageType, IReadOnlyList<StatusEffectKind> removedEffects = null)
    {
        ArgumentNullException.ThrowIfNull(defender);

        if (!damageType.IsPhysical())
            return 1f;

        var factor = removedEffects is { Count: > 0 }
                ? defender.GetTotalMultiplierWithout(CombatStat.PhysicalDamageTaken, removedEffects.Select(StatusEffectRules.GetOriginId).ToArray())
                : defender.GetTotalMultiplier(CombatStat.PhysicalDamageTaken);

        return Math.Max(0f, factor);
    }

    //Ein Krit macht seinen Zusatzschaden, "You take 50% reduced Extra Damage from Critical Strikes" halbiert ihn
    public static float GetCriticalDamageBonusTaken(StatSheet defender, float criticalDamageBonus)
    {
        ArgumentNullException.ThrowIfNull(defender);

        return criticalDamageBonus * (1f - Math.Clamp(defender.GetFinal(CombatStat.ReducedCriticalDamageTaken), 0f, 100f) / 100f);
    }
}
