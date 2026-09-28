using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public enum HitAvoidance
{
    None,
    Missed,
    Dodged,
    Parried
}

public sealed record HitResult
{
    public DamageType   DamageType { get; init; }
    public SkillKind    SkillKind  { get; init; }
    public HitAvoidance Avoidance  { get; init; }
    public bool         WasBlocked { get; init; }
    public bool         IsCritical { get; init; }

    public float RolledDamage { get; init; }

    public float UnmitigatedDamage { get; init; }

    public int FinalDamage { get; init; }

    public StatusEffectApplication InflictedEffect { get; init; }

    public bool HasLanded => Avoidance == HitAvoidance.None;
}
