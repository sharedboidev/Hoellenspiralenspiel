using System;

namespace Hoellenspiralenspiel.Scripts.Core.Progression;

public static class DeathPenalty
{
    public const float XpLossFraction = 0.1f;

    public static long GetXpLoss(long xpTotal, long levelFloor, long nextLevelThreshold, float fraction = XpLossFraction)
    {
        var levelSpan = Math.Max(0, nextLevelThreshold - levelFloor);
        var loss      = (long)(levelSpan * (double)Math.Clamp(fraction, 0f, 1f));
        var available = Math.Max(0, xpTotal - levelFloor);

        return Math.Min(loss, available);
    }
}
