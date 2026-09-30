using System;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

//Der Preis folgt aus dem Grundpreis der Basis und der Seltenheit. Der Händler zahlt beim Ankauf nur einen Anteil davon
public sealed record PriceRule(float MagicFactor = 3f, float RareFactor = 8f, float SellShare = 0.25f)
{
    public int GetUnitBuyPrice(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var basePrice = item.Definition.Price;

        if (basePrice <= 0)
            return 0;

        var factor = item.Rarity switch
        {
            ItemRarity.Magic => MagicFactor,
            ItemRarity.Rare  => RareFactor,
            _                => 1f
        };

        return Math.Max(1, Round(basePrice * (double)Math.Max(0f, factor)));
    }

    public int GetBuyPrice(ItemInstance item)
        => Multiply(GetUnitBuyPrice(item), StackOf(item));

    //Was einen Preis hat, bringt mindestens eine Münze
    public int GetUnitSellPrice(ItemInstance item)
    {
        var buyPrice = GetUnitBuyPrice(item);

        return buyPrice <= 0 ? 0 : Math.Max(1, (int)Math.Floor(buyPrice * (double)Math.Clamp(SellShare, 0f, 1f)));
    }

    public int GetSellPrice(ItemInstance item)
        => Multiply(GetUnitSellPrice(item), StackOf(item));

    private static int StackOf(ItemInstance item)
        => Math.Max(1, item.StackSize);

    private static int Multiply(int unitPrice, int amount)
        => (int)Math.Min(int.MaxValue, (long)unitPrice * amount);

    private static int Round(double value)
        => (int)Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero));
}
