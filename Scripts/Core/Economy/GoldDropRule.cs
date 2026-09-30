using System;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

public static class GoldDropRule
{
    //0 heißt, der Gegner lässt nichts fallen
    public static int Roll(int baseMin, int baseMax, int level, float growthPerLevel, float rarityFactor, float chancePercent, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var low  = Math.Max(0, Math.Min(baseMin, baseMax));
        var high = Math.Max(0, Math.Max(baseMin, baseMax));

        if (high == 0 || random.NextPercent() >= chancePercent)
            return 0;

        var rolled = random.NextInt(low, high + 1);
        var scaled = rolled * GetLevelFactor(level, growthPerLevel) * Math.Max(0f, rarityFactor);

        return Math.Max(1, (int)Math.Round(scaled, MidpointRounding.AwayFromZero));
    }

    public static float GetLevelFactor(int level, float growthPerLevel)
        => 1f + Math.Max(0f, growthPerLevel) * (Math.Max(1, level) - 1);
}
