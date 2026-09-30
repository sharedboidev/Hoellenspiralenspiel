using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

//Der Händler hat Waren, die nie ausgehen, einen gewürfelten Bestand und merkt sich, was er angekauft hat
public sealed class Vendor
{
    public const int DefaultWidth  = 14;
    public const int DefaultHeight = 10;

    private readonly List<ItemInstance> buybackOrder = new();

    public Vendor(int width = DefaultWidth, int height = DefaultHeight)
    {
        Wares   = new InventoryGrid(width, height);
        Stock   = new InventoryGrid(width, height);
        Buyback = new InventoryGrid(width, height);
    }

    public InventoryGrid Wares { get; }

    public InventoryGrid Stock { get; }

    public InventoryGrid Buyback { get; }

    public int ItemLevel { get; private set; } = 1;

    public bool IsStocked { get; private set; }

    public event Action Changed;

    public void SetWares(IEnumerable<ItemInstance> wares)
    {
        Wares.Clear();

        LayOutByType(Wares, wares);

        Changed?.Invoke();
    }

    public void Restock(IEnumerable<ItemInstance> items, int itemLevel)
    {
        Stock.Clear();

        LayOutByType(Stock, items);

        FinishStocking(itemLevel);
    }

    public void RestoreStock(IEnumerable<(ItemInstance Item, GridCell Cell)> placed, int itemLevel)
    {
        Stock.Clear();

        foreach (var (item, cell) in placed ?? [])
        {
            if (!Stock.TryPlace(item, cell))
                Stock.TryAdd(item);
        }

        FinishStocking(itemLevel);
    }

    public bool TakeFromStock(ItemInstance item)
    {
        if (!Stock.Remove(item))
            return false;

        Changed?.Invoke();

        return true;
    }

    //Ist kein Platz mehr, weicht das älteste Item
    public void AddToBuyback(ItemInstance item)
    {
        if (item is null || item.Definition.Width > Buyback.Width || item.Definition.Height > Buyback.Height)
            return;

        while (!Buyback.TryAdd(item) && buybackOrder.Count > 0)
        {
            Buyback.Remove(buybackOrder[0]);

            buybackOrder.RemoveAt(0);
        }

        if (Buyback.Contains(item))
            buybackOrder.Add(item);

        Changed?.Invoke();
    }

    public bool TakeFromBuyback(ItemInstance item)
    {
        if (!Buyback.Remove(item))
            return false;

        buybackOrder.Remove(item);

        Changed?.Invoke();

        return true;
    }

    public void ClearBuyback()
    {
        if (buybackOrder.Count == 0)
            return;

        Buyback.Clear();
        buybackOrder.Clear();

        Changed?.Invoke();
    }

    private void FinishStocking(int itemLevel)
    {
        ItemLevel = Math.Max(1, itemLevel);
        IsStocked = true;

        Changed?.Invoke();
    }

    //Genommen wird die erste Auslage, in die alles passt, sonst die mit den meisten Stücken. So bringt die Sortierung nie weniger unter als dichtes Packen
    private static void LayOutByType(InventoryGrid grid, IEnumerable<ItemInstance> items)
    {
        var sorted = ItemTypeOrder.Sort(items);
        List<(ItemInstance Item, GridCell Cell)> best = null;

        foreach (var strategy in new[] { Strategy.EvenTopFirst, Strategy.FillLine, Strategy.FirstGap })
        {
            var placed = LayOut(grid.Width, grid.Height, sorted, strategy);

            if (best is null || placed.Count > best.Count)
                best = placed;

            if (best.Count == sorted.Count)
                break;
        }

        foreach (var (item, cell) in best)
            grid.TryPlace(item, cell);
    }

    //Jede Spalte füllt sich von oben nach unten in Typreihenfolge. EvenTopFirst setzt breite Items zuerst auf gleich weit gefüllte Spalten, sonst entstünde eine Treppe
    private static List<(ItemInstance Item, GridCell Cell)> LayOut(int width, int height, List<ItemInstance> sorted, Strategy strategy)
    {
        var scratch  = new InventoryGrid(width, height);
        var filledTo = new int[width];
        var column   = 0;
        var placed   = new List<(ItemInstance Item, GridCell Cell)>();

        foreach (var item in sorted)
        {
            var cell = strategy == Strategy.FirstGap
                           ? FindFirstGap(scratch, item)
                           : (strategy == Strategy.EvenTopFirst ? FindOnFillLine(scratch, filledTo, column, item, true) : null) ?? FindOnFillLine(scratch, filledTo, column, item, false);

            if (cell is not { } at || !scratch.TryPlace(item, at))
                continue;

            for (var covered = at.X; covered < at.X + item.Definition.Width; covered++)
                filledTo[covered] = at.Y + item.Definition.Height;

            column = at.X;

            placed.Add((item, at));
        }

        return placed;
    }

    private static GridCell? FindOnFillLine(InventoryGrid grid, int[] filledTo, int from, ItemInstance item, bool needsEvenTop)
    {
        var width = item.Definition.Width;

        for (var x = from; x <= grid.Width - width; x++)
        {
            var tops = filledTo.Skip(x).Take(width).ToList();
            var cell = new GridCell(x, tops.Max());

            if ((!needsEvenTop || tops.Distinct().Count() == 1) && grid.CanPlace(item, cell))
                return cell;
        }

        return null;
    }

    private static GridCell? FindFirstGap(InventoryGrid grid, ItemInstance item)
    {
        for (var x = 0; x <= grid.Width - item.Definition.Width; x++)
        {
            for (var y = 0; y <= grid.Height - item.Definition.Height; y++)
            {
                if (grid.CanPlace(item, new GridCell(x, y)))
                    return new GridCell(x, y);
            }
        }

        return null;
    }

    private enum Strategy
    {
        EvenTopFirst,
        FillLine,
        FirstGap
    }
}
