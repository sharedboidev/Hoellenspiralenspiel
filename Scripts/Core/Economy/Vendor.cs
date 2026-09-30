using System;
using System.Collections.Generic;
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

        foreach (var ware in wares ?? [])
            Wares.TryAdd(ware);

        Changed?.Invoke();
    }

    //Was nicht mehr ins Gitter passt, bietet der Händler nicht an
    public void Restock(IEnumerable<ItemInstance> items, int itemLevel)
    {
        Stock.Clear();

        foreach (var item in items ?? [])
            Stock.TryAdd(item);

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
}
