namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public sealed record StatusEffectApplication(StatusEffectKind Kind, float Magnitude, float DurationSec)
{
    //Wer den Effekt gelegt hat, etwa der Held, dem eine Explosion beim Tod gehört. Der Kern kennt keine Einheiten und reicht ihn nur durch
    public object Source { get; init; }
}

public readonly record struct StatusTick(StatusEffectKind Kind, int Damage);
