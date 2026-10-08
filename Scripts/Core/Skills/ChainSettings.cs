using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Blitz ohne Flugzeit trifft das erste Ziel in Range und springt von dort auf den nächsten Gegner in JumpRange, jeden nur einmal.
//Jeder Sprung macht FalloffPercent weniger Schaden als der davor. Proliferate gibt weitere Sprünge, zusätzliche Projektile zählen nicht.
//Längen in Pixeln auf dem Boden, gemessen von Rand zu Rand der Körper
public sealed record ChainSettings(float Range, float JumpRange, int Jumps, float FalloffPercent)
{
    public static int GetProliferate(StatSheet stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        return Math.Max(0, stats.GetFinalWhole(CombatStat.Proliferate));
    }

    public int GetJumps(int proliferate)
        => Math.Max(0, Jumps) + Math.Max(0, proliferate);

    //Das erste Ziel ist Sprung 0 und bekommt den vollen Schaden, Sprung 2 bei 25 % also 75 % von 75 %
    public float GetDamageFactor(int jump)
        => MathF.Pow(1f - Math.Clamp(FalloffPercent, 0f, 100f) / 100f, Math.Max(0, jump));

    //Ein Mauspunkt jenseits der Reichweite rückt auf ihren Rand, um ihn herum sucht der Blitz sein erstes Ziel.
    //Der Versatz zählt ab der Mitte des Wirkenden, die Reichweite ab dem Rand seines Körpers
    public (float X, float Y) ClampToRange(float offsetX, float offsetY, float casterRadius = 0f)
    {
        var length = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);
        var range  = Math.Max(0f, Range) + Math.Max(0f, casterRadius);

        if (length <= range || length <= 0f)
            return (offsetX, offsetY);

        return (offsetX / length * range, offsetY / length * range);
    }
}
