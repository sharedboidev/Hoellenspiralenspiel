using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public static class ItemRequirements
{
    public static IReadOnlyList<Requirement> GetUnmet(ItemDefinition item, Func<Requirement, int> getCharacterValue)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(getCharacterValue);

        var unmet = new List<Requirement>();

        foreach (var (requirement, neededValue) in item.Requirements)
        {
            if (getCharacterValue(requirement) < neededValue)
                unmet.Add(requirement);
        }

        return unmet;
    }

    public static bool AreMet(ItemDefinition item, Func<Requirement, int> getCharacterValue)
        => GetUnmet(item, getCharacterValue).Count == 0;
}
