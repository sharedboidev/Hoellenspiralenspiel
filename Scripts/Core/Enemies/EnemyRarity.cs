using System;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public enum EnemyRarity
{
    Normal,
    Elite,
    RareElite
}

public sealed record EnemyRarityChances(float ElitePercent, float RareElitePercent);

public static class EnemyRarityRules
{
    public const int MinEliteMods     = 1;
    public const int MaxEliteMods     = 2;
    public const int MinRareEliteMods = 3;
    public const int MaxRareEliteMods = 5;

    public static EnemyRarity FromModCount(int modCount)
    {
        if (modCount < MinEliteMods)
            return EnemyRarity.Normal;

        return modCount <= MaxEliteMods ? EnemyRarity.Elite : EnemyRarity.RareElite;
    }

    //Zieht immer zwei Zahlen, damit jeder Spawn gleich viele Würfe verbraucht
    public static int RollModCount(EnemyRarityChances chances, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(chances);
        ArgumentNullException.ThrowIfNull(random);

        var rarityRoll = random.NextPercent();
        var countRoll  = random.NextFloat();

        if (rarityRoll < chances.RareElitePercent)
            return PickBetween(MinRareEliteMods, MaxRareEliteMods, countRoll);

        if (rarityRoll < chances.RareElitePercent + chances.ElitePercent)
            return PickBetween(MinEliteMods, MaxEliteMods, countRoll);

        return 0;
    }

    private static int PickBetween(int min, int max, float roll)
        => Math.Min(max, min + (int)(roll * (max - min + 1)));
}
