using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class AffixRoller
{
    public const int DefaultMaxAffixesPerItem = 8;

    private readonly IReadOnlyList<AffixDefinition> affixes;
    private readonly int                            maxAffixesPerItem;

    public AffixRoller(IEnumerable<AffixDefinition> affixes, int maxAffixesPerItem = DefaultMaxAffixesPerItem)
    {
        ArgumentNullException.ThrowIfNull(affixes);

        this.affixes           = affixes.ToArray();
        this.maxAffixesPerItem = Math.Max(0, maxAffixesPerItem);
    }

    public static int GetAffixCountCeiling(int itemLevel)
        => itemLevel switch
        {
            <= 10 => 3,
            <= 25 => 5,
            <= 40 => 6,
            <= 50 => 7,
            <= 60 => 8,
            <= 70 => 9,
            <= 80 => 10,
            <= 90 => 11,
            _     => 12
        };

    public void RollAffixesFor(ItemInstance item, IRandomSource random)
        => RollAffixesFor(item, random, 0, GetAffixCountCeiling(item?.ItemLevel ?? 1));

    //Für Ware mit vorgegebener Seltenheit: ein bis zwei Affixe ergeben Magic, ab drei ist das Item Rare
    public void RollAffixesFor(ItemInstance item, IRandomSource random, int minCount, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(random);

        if (!item.Definition.IsEquippable)
            return;

        var wantedCount = Math.Min(random.NextInt(minCount, Math.Max(minCount, maxCount) + 1), maxAffixesPerItem);
        var nextType    = random.NextInt(0, 2) == 0 ? AffixType.Prefix : AffixType.Suffix;

        for (var i = 0; i < wantedCount; i++)
        {
            var affix = RollAffix(nextType, item, random);

            if (affix is not null)
                item.AddAffix(affix);

            nextType = nextType == AffixType.Prefix ? AffixType.Suffix : AffixType.Prefix;
        }

        if (item.Rarity == ItemRarity.Rare)
            item.RareName = ItemNameGenerator.Generate(item.Definition, random);
    }

    private ItemAffix RollAffix(AffixType type, ItemInstance item, IRandomSource random)
    {
        var candidates  = FindCandidates(type, item);
        var totalWeight = candidates.Sum(candidate => candidate.Tier.Weight);

        if (totalWeight <= 0)
            return null;

        var luckyNumber      = random.NextFloat() * totalWeight;
        var cumulativeWeight = 0f;

        foreach (var (affix, tier) in candidates)
        {
            cumulativeWeight += tier.Weight;

            if (luckyNumber < cumulativeWeight)
                return new ItemAffix(type, affix.Stat, affix.Modification, RollValue(affix, tier, random), tier.NameAddition, affix.IsLocal);
        }

        var (lastAffix, lastTier) = candidates[^1];

        return new ItemAffix(type, lastAffix.Stat, lastAffix.Modification, RollValue(lastAffix, lastTier, random), lastTier.NameAddition, lastAffix.IsLocal);
    }

    private List<(AffixDefinition Affix, AffixTierDefinition Tier)> FindCandidates(AffixType type, ItemInstance item)
    {
        var candidates = new List<(AffixDefinition, AffixTierDefinition)>();

        foreach (var affix in affixes)
        {
            if (affix.Type != type || !affix.CanAppearOn(item.Definition.Slot) || item.HasAffixLike(type, affix.Stat, affix.Modification))
                continue;

            foreach (var tier in affix.Tiers)
            {
                if (tier.Weight > 0 && tier.MinItemLevel <= item.ItemLevel)
                    candidates.Add((affix, tier));
            }
        }

        return candidates;
    }

    private static float RollValue(AffixDefinition affix, AffixTierDefinition tier, IRandomSource random)
    {
        var low  = Math.Min(tier.MinValue, tier.MaxValue);
        var high = Math.Max(tier.MinValue, tier.MaxValue);

        var value = affix.AllowsFractions
                ? random.NextRange(low, high)
                : (int)low + random.NextInt(0, (int)high - (int)low + 1);

        return affix.Modification is ModificationType.Percentage or ModificationType.More ? value / 100f : value;
    }
}
