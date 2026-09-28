namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

//Ein Statuseffekt, der auf eine Einheit gelegt wird.
//Die Stärke ist bei Bleed und Burn der Schaden pro Sekunde, bei Shock und Chill ein Anteil von 0 bis 1
public sealed record StatusEffectApplication(StatusEffectKind Kind, float Magnitude, float DurationSec);

//Schaden, den ein Statuseffekt in einem Takt verursacht
public readonly record struct StatusTick(StatusEffectKind Kind, int Damage);
