using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Units;

public static class UnitRegistry
{
    private const float CellSizePx      = 256f;
    private const float SearchPaddingPx = 48f;

    private static readonly SpatialHash<BaseUnit> Grid            = new(CellSizePx);
    private static readonly List<BaseUnit>        RegisteredUnits = new();

    private static float largestBodyRadiusPx;

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
        var position = unit.GlobalPosition;

        Grid.Place(unit, WorldScale.ToPx(position.X), WorldScale.ToPx(position.Z));
    }

    //Abstände zählen ab dem Rand der Körper, die Suche muss deshalb um den größten Körper weiter reichen
    public static void NoteBodyRadius(float radiusPx)
        => largestBodyRadiusPx = Math.Max(largestBodyRadiusPx, radiusPx);

    //Liefert eine Obermenge. Den genauen Abstand prüft der Aufrufer
    public static void FindNear(Vector3 center, float radiusPx, List<BaseUnit> results)
        => Grid.Query(WorldScale.ToPx(center.X), WorldScale.ToPx(center.Z), radiusPx + SearchPaddingPx + largestBodyRadiusPx, results);
}
