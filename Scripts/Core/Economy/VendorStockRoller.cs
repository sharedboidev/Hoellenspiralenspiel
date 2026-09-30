using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

//Die meiste Ware ist weiß. Nur selten trägt ein Stück ein bis zwei Affixe, noch seltener ist es Rare
public sealed class VendorStockRoller
{
    public const int MagicAffixesMax = 2;
    public const int RareAffixesMin  = 3;

    private readonly AffixRoller affixRoller;

    public VendorStockRoller(AffixRoller affixRoller)
    {
        ArgumentNullException.ThrowIfNull(affixRoller);

        this.affixRoller = affixRoller;
    }

    public List<ItemInstance> Roll(IReadOnlyList<ItemDefinition> bases, int count, int itemLevel, float magicChance, float rareChance, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(bases);
        ArgumentNullException.ThrowIfNull(random);

        var stock = new List<ItemInstance>();

        if (bases.Count == 0)
            return stock;

        for (var i = 0; i < count; i++)
        {
            var item   = new ItemInstance(bases[random.NextInt(0, bases.Count)], itemLevel);
            var rarity = random.NextFloat();

            if (rarity < rareChance)
                affixRoller.RollAffixesFor(item, random, RareAffixesMin, Math.Max(RareAffixesMin, AffixRoller.GetAffixCountCeiling(item.ItemLevel)));
            else if (rarity < rareChance + magicChance)
                affixRoller.RollAffixesFor(item, random, 1, MagicAffixesMax);

            stock.Add(item);
        }

        return stock;
    }

    //"Jedes zwanzigste" heißt eine Chance von 1 zu 20. 0 schaltet die Seltenheit ab
    public static float ChanceOfOneIn(int every)
        => every <= 0 ? 0f : 1f / every;
}
