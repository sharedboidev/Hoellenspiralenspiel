using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Spatial;

public sealed class SpatialHash<T>
        where T : class
{
    private readonly Dictionary<T, long>       cellOfItem = new();
    private readonly Dictionary<long, List<T>> cells      = new();
    private readonly float                     cellSize;

    public SpatialHash(float cellSize)
    {
        if (cellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Die Zellgröße muss größer als 0 sein.");

        this.cellSize = cellSize;
    }

    public int Count => cellOfItem.Count;

    public void Place(T item, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(item);

        var cell = GetCellKey(ToCell(x), ToCell(y));

        if (cellOfItem.TryGetValue(item, out var previousCell))
        {
            if (previousCell == cell)
                return;

            RemoveFromCell(item, previousCell);
        }

        cellOfItem[item] = cell;

        if (!cells.TryGetValue(cell, out var items))
        {
            items       = new List<T>();
            cells[cell] = items;
        }

        items.Add(item);
    }

    public bool Remove(T item)
    {
        if (item is null || !cellOfItem.Remove(item, out var cell))
            return false;

        RemoveFromCell(item, cell);

        return true;
    }

    //Liefert alles aus den Zellen, die der Kreis berührt. Den genauen Abstand prüft der Aufrufer
    public void Query(float x, float y, float radius, List<T> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        results.Clear();

        var reach = Math.Max(0f, radius);
        var minX  = ToCell(x - reach);
        var maxX  = ToCell(x + reach);
        var minY  = ToCell(y - reach);
        var maxY  = ToCell(y + reach);

        for (var cellX = minX; cellX <= maxX; cellX++)
        {
            for (var cellY = minY; cellY <= maxY; cellY++)
            {
                if (cells.TryGetValue(GetCellKey(cellX, cellY), out var items))
                    results.AddRange(items);
            }
        }
    }

    private void RemoveFromCell(T item, long cell)
    {
        if (!cells.TryGetValue(cell, out var items))
            return;

        items.Remove(item);

        if (items.Count == 0)
            cells.Remove(cell);
    }

    private int ToCell(float coordinate)
        => (int)MathF.Floor(coordinate / cellSize);

    private static long GetCellKey(int cellX, int cellY)
        => ((long)cellX << 32) | (uint)cellY;
}
