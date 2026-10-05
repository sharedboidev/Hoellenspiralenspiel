using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public static class ItemRequirements
{
    public static IReadOnlyList<Requirement> GetUnmet(ItemDefinition item, Func<Requirement, int> getCharacterValue)
    {
        ArgumentNullException.ThrowIfNull(item);

        return GetUnmet(item.Requirements, getCharacterValue);
    }

    //Das Item selbst kann seine Anforderungen über Affixe senken
    public static IReadOnlyList<Requirement> GetUnmet(ItemInstance item, Func<Requirement, int> getCharacterValue)
    {
        ArgumentNullException.ThrowIfNull(item);

        return GetUnmet(item.Requirements, getCharacterValue);
    }

    public static bool AreMet(ItemDefinition item, Func<Requirement, int> getCharacterValue)
        => GetUnmet(item, getCharacterValue).Count == 0;

    public static bool AreMet(ItemInstance item, Func<Requirement, int> getCharacterValue)
        => GetUnmet(item, getCharacterValue).Count == 0;

    private static List<Requirement> GetUnmet(IReadOnlyDictionary<Requirement, int> requirements, Func<Requirement, int> getCharacterValue)
    {
        ArgumentNullException.ThrowIfNull(getCharacterValue);

        var unmet = new List<Requirement>();

        foreach (var (requirement, neededValue) in requirements)
        {
            if (getCharacterValue(requirement) < neededValue)
                unmet.Add(requirement);
        }

        return unmet;
    }
}
