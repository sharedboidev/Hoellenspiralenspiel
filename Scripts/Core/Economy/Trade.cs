using System;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

public enum TradeResult
{
    Done,
    NotEnoughGold,
    NoRoom,
    NotForSale
}

//Jeder Handel ist mit einem Aufruf abgeschlossen. Ware geht nie durch die Hand, sonst ließe sie sich ohne Bezahlung behalten
public sealed class Trade
{
    private readonly Purse          gold;
    private readonly CharacterItems items;
    private readonly PriceRule      prices;
    private readonly Vendor         vendor;

    public Trade(CharacterItems items, Purse gold, Vendor vendor, PriceRule prices)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(gold);
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(prices);

        this.items  = items;
        this.gold   = gold;
        this.vendor = vendor;
        this.prices = prices;
    }

    public int GetBuyPrice(ItemInstance item)
        => vendor.Buyback.Contains(item) ? prices.GetSellPrice(item) : prices.GetBuyPrice(item);

    public int GetSellPrice(ItemInstance item)
        => prices.GetSellPrice(item);

    //Waren gehen nie aus. Gekauft wird Stück für Stück, bis Gold oder Platz fehlen
    public TradeResult BuyWare(ItemInstance ware, int amount = 1)
    {
        if (ware is null || !vendor.Wares.Contains(ware) || prices.GetUnitBuyPrice(ware) <= 0)
            return TradeResult.NotForSale;

        var unitPrice = prices.GetUnitBuyPrice(ware);
        var result    = TradeResult.NotForSale;

        for (var i = 0; i < Math.Max(1, amount); i++)
        {
            var unit = new ItemInstance(ware.Definition, ware.ItemLevel);

            result = CheckPurchase(unit, unitPrice);

            if (result != TradeResult.Done)
                return i > 0 ? TradeResult.Done : result;

            gold.TrySpend(unitPrice);
            items.PickUp(unit);
        }

        return result;
    }

    public TradeResult BuyStock(ItemInstance item)
    {
        if (item is null || !vendor.Stock.Contains(item) || prices.GetBuyPrice(item) <= 0)
            return TradeResult.NotForSale;

        var price  = prices.GetBuyPrice(item);
        var result = CheckPurchase(item, price);

        if (result != TradeResult.Done)
            return result;

        vendor.TakeFromStock(item);
        gold.TrySpend(price);
        items.PickUp(item);

        return TradeResult.Done;
    }

    //Der Rückkauf kostet, was der Händler gezahlt hat
    public TradeResult BuyBack(ItemInstance item)
    {
        if (item is null || !vendor.Buyback.Contains(item))
            return TradeResult.NotForSale;

        var price  = prices.GetSellPrice(item);
        var result = CheckPurchase(item, price);

        if (result != TradeResult.Done)
            return result;

        vendor.TakeFromBuyback(item);
        gold.TrySpend(price);
        items.PickUp(item);

        return TradeResult.Done;
    }

    public bool CanSell(ItemInstance item)
        => item is not null && (items.HeldItem == item || items.Inventory.Contains(item)) && prices.GetSellPrice(item) > 0;

    public bool Sell(ItemInstance item)
    {
        if (!CanSell(item))
            return false;

        var price = prices.GetSellPrice(item);

        items.Release(item);
        gold.Add(price);
        vendor.AddToBuyback(item);

        return true;
    }

    private TradeResult CheckPurchase(ItemInstance item, int price)
    {
        if (!gold.CanAfford(price))
            return TradeResult.NotEnoughGold;

        return items.HasRoomFor(item) ? TradeResult.Done : TradeResult.NoRoom;
    }
}
