using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

//Ein Münzhaufen wächst mit seinem Betrag: erst eine bis fünf lose Münzen, dann ein bis drei Stapel, zuletzt fünf Stapel
public static class CoinPileTiers
{
    public const int Count = 9;

    public static readonly IReadOnlyList<int> DefaultThresholds = [1, 2, 3, 4, 5, 6, 20, 50, 150];

    //Die höchste Stufe, deren Schwelle der Betrag erreicht. -1 ohne Betrag
    public static int GetTier(int amount, IReadOnlyList<int> thresholds)
    {
        thresholds ??= DefaultThresholds;

        var tier = -1;

        for (var i = 0; amount > 0 && i < thresholds.Count; i++)
        {
            if (amount >= thresholds[i])
                tier = i;
        }

        return tier;
    }
}
