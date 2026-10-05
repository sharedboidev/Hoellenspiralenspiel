using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public sealed record StatusEffectRule(StatusEffectKind Kind, StackingRule Stacking, int MaxInstances, bool DealsDamage);

//Ein Effekt mit Schaden über Zeit: welche Schadensart ihn auslöst und welcher Anteil des Treffers über welche Dauer wirkt.
//Bleed rechnet mit dem Schaden vor der Rüstung, Burn mit dem nach der Resistenz
public sealed record DamageOverTimeRule(StatusEffectKind Kind, DamageType Trigger, float DamageFraction, float DurationSec, bool UsesMitigatedDamage);

public static class StatusEffectRules
{
    private static readonly StatusEffectRule[] Rules =
    [
        new(StatusEffectKind.Bleed, StackingRule.Sum, int.MaxValue, true),
        new(StatusEffectKind.Burn, StackingRule.Sum, CombatRules.BurnMaxStacks, true),
        new(StatusEffectKind.Shock, StackingRule.Strongest, int.MaxValue, false),
        new(StatusEffectKind.Chill, StackingRule.Strongest, int.MaxValue, false)
    ];

    //Ein neuer Effekt mit Schaden über Zeit braucht hier eine Zeile und in Rules DealsDamage. Der Multiplikator des Angreifers,
    //die Schätzung im Tooltip und die Treffer greifen dann von selbst
    private static readonly DamageOverTimeRule[] DamageOverTimeRules =
    [
        new(StatusEffectKind.Bleed, DamageType.Slash, CombatRules.BleedDamageFraction, CombatRules.BleedDurationSec, false),
        new(StatusEffectKind.Burn, DamageType.Fire, CombatRules.BurnDamageFraction, CombatRules.BurnDurationSec, true)
    ];

    public static StatusEffectRule Get(StatusEffectKind kind)
        => Rules[(int)kind];

    public static string GetOriginId(StatusEffectKind kind)
        => $"Status:{kind}";

    public static DamageOverTimeRule FindDamageOverTime(DamageType trigger)
        => Array.Find(DamageOverTimeRules, rule => rule.Trigger == trigger);

    //Erhöhter Schaden über Zeit addiert sich, der Multiplikator ist ein More-Modifier
    public static float GetDamageOverTimeMultiplier(StatSheet attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return attacker.GetTotalMultiplier(CombatStat.DamageOverTime);
    }

    public static StatusEffectApplication GetEffectOfHit(DamageType damageType, float unmitigatedDamage, int finalDamage, float damageOverTimeMultiplier = 1f)
    {
        if (finalDamage <= 0)
            return null;

        if (FindDamageOverTime(damageType) is { } rule)
        {
            var damage = rule.UsesMitigatedDamage ? finalDamage : unmitigatedDamage;

            return new StatusEffectApplication(rule.Kind, damage * rule.DamageFraction / rule.DurationSec * damageOverTimeMultiplier, rule.DurationSec);
        }

        return damageType switch
        {
            DamageType.Lightning => new StatusEffectApplication(StatusEffectKind.Shock, CombatRules.ShockActionFailureChance, CombatRules.ShockDurationSec),
            DamageType.Frost     => new StatusEffectApplication(StatusEffectKind.Chill, CombatRules.ChillSlow, CombatRules.ChillDurationSec),
            _                    => null
        };
    }

    public static IReadOnlyList<CombatStatModifier> GetModifiers(StatusEffectKind kind, float magnitude)
    {
        if (kind != StatusEffectKind.Chill || magnitude <= 0)
            return [];

        var originId = GetOriginId(kind);
        var slow     = -Math.Min(magnitude, 1f);

        return
        [
            new CombatStatModifier(CombatStat.Movementspeed, ModificationType.More, slow, originId),
            new CombatStatModifier(CombatStat.Attackspeed, ModificationType.More, slow, originId)
        ];
    }
}
