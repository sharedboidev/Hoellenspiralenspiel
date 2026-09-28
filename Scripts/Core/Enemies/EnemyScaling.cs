using System;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public static class EnemyScaling
{
    public const int MinLevel = 1;
    public const int MaxLevel = 100;

    public static int GetLevel(int areaLevel, int enemyOffset = 0, int spawnOffset = 0)
        => Math.Clamp(areaLevel + enemyOffset + spawnOffset, MinLevel, MaxLevel);

    public static int GetAttribute(int valueAtLevelOne, float growthPerLevel, int level)
    {
        var levelsGained = Math.Max(0, level - MinLevel);
        var growth       = (int)MathF.Floor(Math.Max(0f, growthPerLevel) * levelsGained);

        return Math.Max(1, valueAtLevelOne + growth);
    }

    public static int GetXp(int baseXp, float rarityFactor)
        => Math.Max(0, (int)MathF.Round(baseXp * Math.Max(0f, rarityFactor)));
}
