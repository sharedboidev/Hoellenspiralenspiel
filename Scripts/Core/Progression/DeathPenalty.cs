using System;

namespace Hoellenspiralenspiel.Scripts.Core.Progression;

public static class DeathPenalty
{
    //Anteil der XP-Spanne des aktuellen Levels, der beim Tod verloren geht
    public const float XpLossFraction = 0.1f;

    //Der Verlust bemisst sich an der Spanne des aktuellen Levels und kann kein Level kosten
    public static long GetXpLoss(long xpTotal, long levelFloor, long nextLevelThreshold, float fraction = XpLossFraction)
    {
        var levelSpan = Math.Max(0, nextLevelThreshold - levelFloor);
        var loss      = (long)(levelSpan * (double)Math.Clamp(fraction, 0f, 1f));
        var available = Math.Max(0, xpTotal - levelFloor);

        return Math.Min(loss, available);
    }
}
