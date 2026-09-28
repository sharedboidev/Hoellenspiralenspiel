using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Spatial;

namespace Hoellenspiralenspiel.Scripts.Units;

public static class UnitRegistry
{
    private const float CellSizePx = 256f;

    //Das Raster kennt die Position vom Beginn des Frames, der Zuschlag deckt die Bewegung seitdem ab
    private const float SearchPaddingPx = 48f;

    private static readonly SpatialHash<BaseUnit> Grid            = new(CellSizePx);
    private static readonly List<BaseUnit>        RegisteredUnits = new();

    public static IReadOnlyList<BaseUnit> Units => RegisteredUnits;

    public static void Register(BaseUnit unit)
    {
        if (!RegisteredUnits.Contains(unit))
            RegisteredUnits.Add(unit);

        Track(unit);
    }

    public static void Unregister(BaseUnit unit)
    {
        RegisteredUnits.Remove(unit);
        Grid.Remove(unit);
    }

    public static void Track(BaseUnit unit)
    {
        var center = unit.BodyCenter;

        Grid.Place(unit, center.X, center.Y);
    }

    //Liefert eine Obermenge. Den genauen Abstand prüft der Aufrufer
    public static void FindNear(Vector2 center, float radius, List<BaseUnit> results)
        => Grid.Query(center.X, center.Y, radius + SearchPaddingPx, results);
}
