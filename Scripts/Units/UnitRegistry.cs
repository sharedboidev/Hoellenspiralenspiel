using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Units;

public static class UnitRegistry
{
    private static readonly List<BaseUnit> RegisteredUnits = new();

    public static IReadOnlyList<BaseUnit> Units => RegisteredUnits;

    public static void Register(BaseUnit unit)
    {
        if (!RegisteredUnits.Contains(unit))
            RegisteredUnits.Add(unit);
    }

    public static void Unregister(BaseUnit unit)
        => RegisteredUnits.Remove(unit);
}
