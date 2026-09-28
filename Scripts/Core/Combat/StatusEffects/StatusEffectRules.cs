using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public sealed record StatusEffectRule(StatusEffectKind Kind, StackingRule Stacking, int MaxInstances, bool DealsDamage);

public static class StatusEffectRules
{
    private static readonly StatusEffectRule[] Rules =
    [
        new(StatusEffectKind.Bleed, StackingRule.Sum, int.MaxValue, true),
        new(StatusEffectKind.Burn, StackingRule.Sum, CombatRules.BurnMaxStacks, true),
        new(StatusEffectKind.Shock, StackingRule.Strongest, int.MaxValue, false),
        new(StatusEffectKind.Chill, StackingRule.Strongest, int.MaxValue, false)
    ];

    public static StatusEffectRule Get(StatusEffectKind kind)
        => Rules[(int)kind];

    public static string GetOriginId(StatusEffectKind kind)
        => $"Status:{kind}";

    public static StatusEffectApplication GetEffectOfHit(DamageType damageType, float unmitigatedDamage, int finalDamage)
    {
        if (finalDamage <= 0)
            return null;

        return damageType switch
        {
            DamageType.Slash => new StatusEffectApplication(StatusEffectKind.Bleed,
                                                            unmitigatedDamage * CombatRules.BleedDamageFraction / CombatRules.BleedDurationSec,
                                                            CombatRules.BleedDurationSec),
            DamageType.Fire => new StatusEffectApplication(StatusEffectKind.Burn,
                                                           finalDamage * CombatRules.BurnDamageFraction / CombatRules.BurnDurationSec,
                                                           CombatRules.BurnDurationSec),
            DamageType.Lightning => new StatusEffectApplication(StatusEffectKind.Shock, CombatRules.ShockActionFailureChance, CombatRules.ShockDurationSec),
            DamageType.Frost => new StatusEffectApplication(StatusEffectKind.Chill, CombatRules.ChillSlow, CombatRules.ChillDurationSec),
            _ => null
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
