using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Chancen und Krit-Schaden in Prozent, ActionFailureChance als Anteil von 0 bis 1
public sealed record SkillDamageEstimate
{
    public DamageType DamageType { get; init; }

    public float MinHit { get; init; }
    public float MaxHit { get; init; }

    public float MaxCriticalHit { get; init; }

    public float AverageHit { get; init; }

    public float HitChance           { get; init; }
    public float CriticalHitChance   { get; init; }
    public float CriticalDamageBonus { get; init; }

    public float ActionFailureChance { get; init; }

    public double UsesPerSecond { get; init; }

    public float HitDps { get; init; }

    public StatusEffectKind? DamagingEffect { get; init; }
    public float             EffectDps      { get; init; }

    public float Dps => HitDps + EffectDps;

    public float ManaPerSecond { get; init; }

    public bool IsLimitedByMana { get; init; }

    public float SustainedDps { get; init; }
}
