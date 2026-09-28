namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public sealed record StatusEffectApplication(StatusEffectKind Kind, float Magnitude, float DurationSec);

public readonly record struct StatusTick(StatusEffectKind Kind, int Damage);
