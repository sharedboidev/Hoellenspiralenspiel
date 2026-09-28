namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Neue Werte nur am Ende anhängen: Resources speichern die Wirkung als Zahl
public enum ConsumableEffectKind
{
    RestoreLife,
    RestoreMana
}

public sealed record ConsumableEffect(ConsumableEffectKind Kind, float Percent);
