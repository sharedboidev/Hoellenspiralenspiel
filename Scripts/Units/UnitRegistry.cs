using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Units;

//Alle Einheiten, die gerade im Szenenbaum hängen. Wer Ziele sucht, fragt hier und filtert nach Fraktion,
//statt den Szenenbaum zu durchsuchen
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
