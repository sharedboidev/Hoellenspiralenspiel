using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public static class MonsterModRoller
{
    //Liefert weniger Mods als verlangt, wenn der Vorrat nicht mehr passende hergibt
    public static List<MonsterModDefinition> Pick(IReadOnlyList<MonsterModDefinition> pool,
                                                  int                                 count,
                                                  MonsterTraits                       monster,
                                                  IRandomSource                       random)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(random);

        var picked     = new List<MonsterModDefinition>();
        var candidates = new List<MonsterModDefinition>();

        foreach (var mod in pool)
        {
            if (mod is not null && mod.Weight > 0f && mod.Fits(monster))
                candidates.Add(mod);
        }

        while (picked.Count < count && candidates.Count > 0)
        {
            var mod = PickWeighted(candidates, random);

            picked.Add(mod);

            candidates.RemoveAll(candidate => candidate.Id == mod.Id || SharesGroup(candidate, mod));
        }

        return picked;
    }

    private static bool SharesGroup(MonsterModDefinition left, MonsterModDefinition right)
        => !string.IsNullOrEmpty(left.ExclusiveGroup) && left.ExclusiveGroup == right.ExclusiveGroup;

    private static MonsterModDefinition PickWeighted(List<MonsterModDefinition> candidates, IRandomSource random)
    {
        var totalWeight = 0f;

        foreach (var candidate in candidates)
            totalWeight += candidate.Weight;

        var luckyNumber      = random.NextFloat() * totalWeight;
        var cumulativeWeight = 0f;

        foreach (var candidate in candidates)
        {
            cumulativeWeight += candidate.Weight;

            if (luckyNumber < cumulativeWeight)
                return candidate;
        }

        return candidates[^1];
    }
}
