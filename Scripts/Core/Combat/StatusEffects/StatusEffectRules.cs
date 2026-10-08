using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

//RemovedBy: Schaden dieser Art löst den Effekt, bevor er den Treffer verstärkt
public sealed record StatusEffectRule(StatusEffectKind Kind, StackingRule Stacking, int MaxInstances, bool DealsDamage, DamageType? RemovedBy = null);

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
        new(StatusEffectKind.Chill, StackingRule.Strongest, int.MaxValue, false),
        new(StatusEffectKind.Brittle, StackingRule.Strongest, int.MaxValue, false, DamageType.Fire)
    ];

    private static readonly StatusEffectKind[] NoKinds = [];

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

    public static DamageOverTimeRule FindDamageOverTime(StatusEffectKind kind)
        => Array.Find(DamageOverTimeRules, rule => rule.Kind == kind);

    //Die Effekte, die ein Treffer mit dieser Schadensart löst, egal ob die Einheit sie gerade hat
    public static IReadOnlyList<StatusEffectKind> GetRemovedBy(HitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<StatusEffectKind> removed = null;

        foreach (var rule in Rules)
        {
            if (rule.RemovedBy is { } damageType && request.Deals(damageType))
                (removed ??= new List<StatusEffectKind>()).Add(rule.Kind);
        }

        return removed ?? (IReadOnlyList<StatusEffectKind>)NoKinds;
    }

    //Was eine Fläche oder ein Skill ohne Treffer legt. Effekte mit Schaden über Zeit hängen am Schaden eines Treffers und fehlen hier
    public static StatusEffectApplication CreateApplication(StatusEffectKind kind, object source = null)
        => kind switch
        {
            StatusEffectKind.Shock   => new StatusEffectApplication(kind, CombatRules.ShockActionFailureChance, CombatRules.ShockDurationSec) { Source = source },
            StatusEffectKind.Chill   => new StatusEffectApplication(kind, CombatRules.ChillSlow, CombatRules.ChillDurationSec) { Source = source },
            StatusEffectKind.Brittle => new StatusEffectApplication(kind, CombatRules.BrittlePhysicalDamageTaken, CombatRules.BrittleDurationSec) { Source = source },
            _                        => null
        };

    //Erhöhter Schaden über Zeit addiert sich, der Multiplikator ist ein More-Modifier
    public static float GetDamageOverTimeMultiplier(StatSheet attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return attacker.GetTotalMultiplier(CombatStat.DamageOverTime);
    }

    //Kälte und Blitz haben noch keinen Effekt mit Schaden über Zeit und darum keinen eigenen Stat
    public static DamageOverTimeByType GetDamageOverTimeByType(StatSheet attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return new DamageOverTimeByType(attacker.GetTotalMultiplier(CombatStat.PhysicalDamageOverTime),
                                        attacker.GetTotalMultiplier(CombatStat.FireDamageOverTime),
                                        1f,
                                        1f);
    }

    //Ein Treffer bringt jeden Effekt einzeln, "chance to Avoid Ailments" würfelt für jeden. Ohne Chance fällt kein Wurf
    public static bool IsAvoided(StatSheet defender, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(defender);
        ArgumentNullException.ThrowIfNull(random);

        var chance = CombatFormulas.ClampChance(defender.GetFinal(CombatStat.AilmentAvoidance));

        return chance > 0f && random.NextPercent() < chance;
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
        if (magnitude <= 0)
            return [];

        var originId = GetOriginId(kind);

        if (kind == StatusEffectKind.Brittle)
            return [new CombatStatModifier(CombatStat.PhysicalDamageTaken, ModificationType.More, magnitude, originId)];

        if (kind != StatusEffectKind.Chill)
            return [];

        var slow = -Math.Min(magnitude, 1f);

        return
        [
            new CombatStatModifier(CombatStat.Movementspeed, ModificationType.More, slow, originId),
            new CombatStatModifier(CombatStat.Attackspeed, ModificationType.More, slow, originId)
        ];
    }
}
