namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Multiplikatoren für Schaden über Zeit je Schadensart, etwa "+20% to Fire Damage over Time Multiplier". Jeder wirkt als More.
//Bleed macht physischen Schaden, Burn Feuer. Ein neuer Effekt nimmt den Wert der Schadensart, die ihn auslöst
public readonly record struct DamageOverTimeByType(float Physical, float Fire, float Frost, float Lightning)
{
    public static DamageOverTimeByType None { get; } = new(1f, 1f, 1f, 1f);

    public float For(DamageType damageType)
        => damageType switch
        {
            DamageType.Fire      => Fire,
            DamageType.Frost     => Frost,
            DamageType.Lightning => Lightning,
            _                    => Physical
        };
}
