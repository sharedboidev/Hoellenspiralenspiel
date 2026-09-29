using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Spatial;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public static class UnitRegistry3D
{
    private const float CellSizePx      = 256f;
    private const float SearchPaddingPx = 48f;

    private static readonly SpatialHash<Unit3D> Grid            = new(CellSizePx);
    private static readonly List<Unit3D>        RegisteredUnits = new();

    public static IReadOnlyList<Unit3D> Units => RegisteredUnits;

    public static void Register(Unit3D unit)
    {
        if (!RegisteredUnits.Contains(unit))
            RegisteredUnits.Add(unit);

        Track(unit);
    }

    public static void Unregister(Unit3D unit)
    {
        RegisteredUnits.Remove(unit);
        Grid.Remove(unit);
    }

    public static void Track(Unit3D unit)
    {
        var position = unit.GlobalPosition;

        Grid.Place(unit, WorldScale.ToPx(position.X), WorldScale.ToPx(position.Z));
    }

    //Liefert eine Obermenge. Den genauen Abstand prüft der Aufrufer
    public static void FindNear(Vector3 center, float radiusPx, List<Unit3D> results)
        => Grid.Query(WorldScale.ToPx(center.X), WorldScale.ToPx(center.Z), radiusPx + SearchPaddingPx, results);
}
