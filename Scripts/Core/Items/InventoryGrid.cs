using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class InventoryGrid
{
    private readonly ItemInstance[,]                    cells;
    private readonly Dictionary<ItemInstance, GridCell> positions = new();

    public InventoryGrid(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width  = width;
        Height = height;
        cells  = new ItemInstance[width, height];
    }

    public int Width  { get; }
    public int Height { get; }
    public int Count  => positions.Count;

    public bool Contains(ItemInstance item)
        => item is not null && positions.ContainsKey(item);

    public GridCell GetPositionOf(ItemInstance item)
        => positions[item];

    public ItemInstance GetItemAt(GridCell cell)
        => IsInside(cell) ? cells[cell.X, cell.Y] : null;

    //Zeile für Zeile von links oben, damit Stapeln, Speichern und Anzeige dieselbe Reihenfolge sehen
    public IEnumerable<ItemInstance> GetItemsInReadingOrder()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var item = cells[x, y];

                if (item is not null && positions[item] == new GridCell(x, y))
                    yield return item;
            }
        }
    }

    public bool CanPlace(ItemInstance item, GridCell cell)
    {
        if (item is null || !Fits(item, cell))
            return false;

        for (var x = cell.X; x < cell.X + item.Definition.Width; x++)
        {
            for (var y = cell.Y; y < cell.Y + item.Definition.Height; y++)
            {
                if (cells[x, y] is not null && cells[x, y] != item)
                    return false;
            }
        }

        return true;
    }

    public bool TryPlace(ItemInstance item, GridCell cell)
    {
        if (!CanPlace(item, cell))
            return false;

        Remove(item);

        positions[item] = cell;

        Fill(item, cell, item);

        return true;
    }

    public bool TryAdd(ItemInstance item)
    {
        var freeCell = FindFreeCellFor(item);

        return freeCell is not null && TryPlace(item, freeCell.Value);
    }

    public bool Remove(ItemInstance item)
    {
        if (item is null || !positions.Remove(item, out var cell))
            return false;

        Fill(item, cell, null);

        return true;
    }

    public void Clear()
    {
        positions.Clear();

        Array.Clear(cells);
    }

    public GridCell? FindFreeCellFor(ItemInstance item)
    {
        if (item is null)
            return null;

        for (var y = 0; y <= Height - item.Definition.Height; y++)
        {
            for (var x = 0; x <= Width - item.Definition.Width; x++)
            {
                var cell = new GridCell(x, y);

                if (CanPlace(item, cell))
                    return cell;
            }
        }

        return null;
    }

    public GridCell ClampIntoGrid(ItemInstance item, GridCell cell)
        => new(Math.Clamp(cell.X, 0, Math.Max(0, Width - item.Definition.Width)),
               Math.Clamp(cell.Y, 0, Math.Max(0, Height - item.Definition.Height)));

    public List<ItemInstance> GetItemsUnder(ItemInstance item, GridCell cell)
    {
        var covered = new List<ItemInstance>();

        if (item is null || !Fits(item, cell))
            return covered;

        for (var x = cell.X; x < cell.X + item.Definition.Width; x++)
        {
            for (var y = cell.Y; y < cell.Y + item.Definition.Height; y++)
            {
                var occupant = cells[x, y];

                if (occupant is not null && occupant != item && !covered.Contains(occupant))
                    covered.Add(occupant);
            }
        }

        return covered;
    }

    private bool Fits(ItemInstance item, GridCell cell)
        => cell.X >= 0 && cell.Y >= 0 && cell.X + item.Definition.Width <= Width && cell.Y + item.Definition.Height <= Height;

    private bool IsInside(GridCell cell)
        => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    private void Fill(ItemInstance item, GridCell cell, ItemInstance content)
    {
        for (var x = cell.X; x < cell.X + item.Definition.Width; x++)
        {
            for (var y = cell.Y; y < cell.Y + item.Definition.Height; y++)
                cells[x, y] = content;
        }
    }
}
