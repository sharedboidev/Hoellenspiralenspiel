using System;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Affixes;
using Hoellenspiralenspiel.Resources.Affixes.Prefixes;
using Hoellenspiralenspiel.Resources.Affixes.Suffixes;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
using Hoellenspiralenspiel.Scripts.Items.Consumables;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Models.Weapons;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using LootTable = Hoellenspiralenspiel.Resources.LootTable;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class Lootsystem : Node
{
    private Dictionary<string, LootTable> Tables  { get; set; } = new();
    private Array<Affix>                  Affixes { get; set; } = new();
    public  RandomNumberGenerator         Rng     { get; set; } = new();

    [Export]
    public int MaximumAffixesPerItem { get; set; } = 8;

    [Export]
    public string LootTablesPath { get; set; } = "res://Resources/LootTables/";

    [Export]
    public string AffixesPath { get; set; } = "res://Resources/Affixes";

    public override void _Ready()
        => LoadAllTables();

    public BaseItem[] GenerateLoot(BaseEnemy enemy)
    {
        if (!Tables.TryGetValue(enemy.LootTableId, out var fittingTable))
            return [];

        var loot = fittingTable.RollLoot();

        RollModifiersFor(loot);

        return loot;
    }

    private void RollModifiersFor(BaseItem[] loot)
    {
        Rng.Randomize();

        foreach (var item in loot)
            RollModifier(item);
    }

    private void RollModifier(BaseItem item)
    {
        if (item is ConsumableItem)
            return;

        var normalizedAffixCount = GetNormalizedAffixAmount(item);
        var nextAffixToRoll      = RollNextAffixType();

        for (var i = 0; i < normalizedAffixCount; i++)
        {
            var newModifier = item switch
            {
                BaseWeapon => RollWeaponAffix(nextAffixToRoll, item.ItemLevel),
                BaseArmor  => RollArmorAffix(nextAffixToRoll, item.ItemLevel),
                _          => throw new ArgumentOutOfRangeException(nameof(item), item, null)
            };

            item.AddModifier(newModifier);

            nextAffixToRoll = nextAffixToRoll == AffixType.Prefix ? AffixType.Suffix : AffixType.Prefix;
        }

        item.Init();
    }

    private ItemModifier RollArmorAffix(AffixType nextAffixToRoll, int itemItemLevel)
    {
        return new ItemModifier(AffixType.Prefix, CombatStat.Damagereduction, ModificationType.Percentage, .1f, "Edgelord's");
    }

    private AffixType RollNextAffixType()
    {
        var nextAffixToRoll = Rng.Randi() % 2 == 0 ? AffixType.Prefix : AffixType.Suffix;

        return nextAffixToRoll;
    }

    private int GetNormalizedAffixAmount(BaseItem item)
    {
        var possibleAffixCountCeiling = GetAffixCountCeilingByItemlevel(item) + 1;
        var totalAffixes              = (int)(Rng.Randi() % possibleAffixCountCeiling);

        return Math.Min(totalAffixes, MaximumAffixesPerItem);
    }

    private void LoadAllTables()
    {
        LoadLootTables();
        LoadAffixes();
    }

    private void LoadAffixes()
    {
        LoadResourcesFromDirectory(AffixesPath, LoadAffixIntoCollection);

        GD.Print($"Loaded {Affixes.Count} Affixes");
    }

    private void LoadLootTables()
    {
        LoadResourcesFromDirectory(LootTablesPath, LoadTableIntoDictionary);

        GD.Print($"Loaded {Tables.Count} loot tables");
    }

    private void LoadResourcesFromDirectory(string path, Action<string> loadAction)
    {
        using var directory = DirAccess.Open(path);
        var       fileName  = GetDirectoryFilename(path, directory);

        while (!string.IsNullOrWhiteSpace(fileName))
        {
            var fullPath = Path.Combine(path, fileName);

            if (directory.CurrentIsDir())
            {
                if (fileName != "." && fileName != "..")
                    LoadResourcesFromDirectory(fullPath + "/", loadAction);
            }
            else
            {
                if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
                    loadAction(fullPath);
            }

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    private static string GetDirectoryFilename(string path, DirAccess directory)
    {
        if (directory == null)
            throw new IOException($"Failed to open directory: {path}");

        var fileName = GetFileName(directory);

        return fileName;
    }

    private void LoadTableIntoDictionary(string fullPath)
    {
        var table = GD.Load<LootTable>(fullPath);

        if (table != null && !string.IsNullOrEmpty(table.TableId))
        {
            Tables[table.TableId] = table;

            GD.Print($"Loaded loot table: {table.TableId}");
        }
    }

    private void LoadAffixIntoCollection(string fullPath)
    {
        var affix = GD.Load<Affix>(fullPath);

        if (affix is null)
            return;

        Affixes.Add(affix);

        var resourceName = affix.ResourcePath.Split('/').Last();
        GD.Print($"Loaded Affix: {resourceName}");
    }

    private static string GetFileName(DirAccess directory)
    {
        directory.ListDirBegin();
        var fileName = directory.GetNext();

        return fileName;
    }

    private ItemModifier RollWeaponAffix(AffixType affixType, int itemLevel)
    {
        var filteredAffixes    = FilterAffixesByType(affixType);
        var possibleAffixTiers = FindPossibleAffixTiers(itemLevel, filteredAffixes);

        var totalWeight      = possibleAffixTiers.Sum(pat => pat.Weight) + 1;
        var luckyNumber      = GD.Randi() % totalWeight;
        var cumulativeWeight = 0f;

        ItemModifier finalModifier = null;

        foreach (var affixTier in possibleAffixTiers)
        {
            cumulativeWeight += affixTier.Weight;

            if (cumulativeWeight < luckyNumber)
                continue;

            var kongruentAffix = filteredAffixes.FirstOrDefault(affix => affix.Tiers.Contains(affixTier));

            if (kongruentAffix is null)
                break;

            var affixValue = RollAffixValue(kongruentAffix, affixTier);

            finalModifier = new ItemModifier(affixType, kongruentAffix.AffectedCombatStat, kongruentAffix.ModificationType, affixValue, affixTier.ItemnameAddition);

            break;
        }

        return finalModifier;
    }

    private static AffixTier[] FindPossibleAffixTiers(int itemLevel, Affix[] filteredAffixes)
    {
        var possibleAffixTiers = filteredAffixes.SelectMany(affix => affix.Tiers)
                                                .Where(tier => tier.MinItemLevelToAppearOn <= itemLevel)
                                                .Shuffle()
                                                .ToArray();

        return possibleAffixTiers;
    }

    private Affix[] FilterAffixesByType(AffixType affixType)
    {
        var filteredAffixes = affixType switch
        {
            AffixType.Prefix => Affixes.Where(a => a.GetType() == typeof(Prefix)).ToArray(),
            AffixType.Suffix => Affixes.Where(a => a.GetType() == typeof(Suffix)).ToArray(),
            _                => throw new ArgumentOutOfRangeException(nameof(affixType), affixType, null)
        };

        return filteredAffixes;
    }

    private static float RollAffixValue(Affix kongruentAffix, AffixTier affixTier)
    {
        float affixValue;

        if (kongruentAffix.AllowFractions)
            affixValue = (float)GD.RandRange(affixTier.MinValue, affixTier.MaxValue);
        else
            affixValue = GD.Randi() % (affixTier.MaxValue - affixTier.MinValue + 1) + affixTier.MinValue;

        if (kongruentAffix.ModificationType is ModificationType.Percentage or ModificationType.More)
            affixValue /= 100;
        
        return affixValue;
    }

    private int GetAffixCountCeilingByItemlevel(BaseItem item)
        => item.ItemLevel switch
        {
            >= 0 and <= 10 => 3,
            <= 25          => 5,
            <= 40          => 6,
            <= 50          => 7,
            <= 60          => 8,
            <= 70          => 9,
            <= 80          => 10,
            <= 90          => 11,
            <= 100         => 12,
            _              => throw new ArgumentOutOfRangeException()
        };
}