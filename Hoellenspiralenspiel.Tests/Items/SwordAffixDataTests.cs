using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Tests.Balance;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

//Die Affixe der Schwerter stammen aus den Basis-Affixen der Einhandschwerter von poedb.tw, mit eigenen Namen
[TestFixture]
public class SwordAffixDataTests
{
    private const string PrefixFolder = "res://Resources/Affixes/Prefixes/Weapons/Swords";
    private const string SuffixFolder = "res://Resources/Affixes/Suffixes/Weapons/Swords";

    private static readonly (string File, AffixType Type, CombatStat Stat, ModificationType Modification, bool IsLocal, bool AllowsFractions)[] Expected =
    [
        ("AddedFireDamage", AffixType.Prefix, CombatStat.FireDamage, ModificationType.Flat, true, false),
        ("AddedFrostDamage", AffixType.Prefix, CombatStat.FrostDamage, ModificationType.Flat, true, false),
        ("AddedLightningDamage", AffixType.Prefix, CombatStat.LightningDamage, ModificationType.Flat, true, false),
        ("AddedPhysicalDamage", AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat, true, false),
        ("IncreasedElementalDamage", AffixType.Prefix, CombatStat.ElementalAttackDamage, ModificationType.Percentage, false, false),
        ("IncreasedPhysicalDamage", AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Percentage, true, false),
        ("DamageOverTimeMultiplier", AffixType.Suffix, CombatStat.DamageOverTime, ModificationType.More, false, false),
        ("FlatCriticalDamage", AffixType.Suffix, CombatStat.CriticalDamage, ModificationType.Flat, false, false),
        ("FlatDexterity", AffixType.Suffix, CombatStat.Dexterity, ModificationType.Flat, false, false),
        ("FlatStrength", AffixType.Suffix, CombatStat.Strength, ModificationType.Flat, false, false),
        ("IncreasedAttackspeed", AffixType.Suffix, CombatStat.Attackspeed, ModificationType.Percentage, true, false),
        ("IncreasedCriticalHitChance", AffixType.Suffix, CombatStat.CriticalHitChance, ModificationType.Percentage, true, false),
        ("LifeLeech", AffixType.Suffix, CombatStat.Leech, ModificationType.Flat, false, true),
        ("LifeOnHit", AffixType.Suffix, CombatStat.LifeOnHit, ModificationType.Flat, false, false),
        ("LifeOnKill", AffixType.Suffix, CombatStat.LifeOnKill, ModificationType.Flat, false, false),
        ("ManaLeech", AffixType.Suffix, CombatStat.ManaLeech, ModificationType.Flat, false, true),
        ("ManaOnKill", AffixType.Suffix, CombatStat.ManaOnKill, ModificationType.Flat, false, false),
        ("ReducedAttributeRequirements", AffixType.Suffix, CombatStat.AttributeRequirements, ModificationType.Percentage, true, false)
    ];

    private static IEnumerable<string> SwordAffixFiles()
        => Files(PrefixFolder).Concat(Files(SuffixFolder));

    private static IEnumerable<TestCaseData> ExpectedAffixes()
        => Expected.Select(affix => new TestCaseData(affix.File, affix.Type, affix.Stat, affix.Modification, affix.IsLocal, affix.AllowsFractions).SetName($"Affix_{affix.File}"));

    [Test]
    public void Schwerter_HabenGenauDieErwartetenAffixe()
        => Assert.That(SwordAffixFiles().Select(Path.GetFileNameWithoutExtension), Is.EquivalentTo(Expected.Select(affix => affix.File)));

    [TestCaseSource(nameof(ExpectedAffixes))]
    public void Affix_HatStatArtUndOrtWieErwartet(string file, AffixType type, CombatStat stat, ModificationType modification, bool isLocal, bool allowsFractions)
    {
        var path   = SwordAffixFiles().Single(candidate => Path.GetFileNameWithoutExtension(candidate) == file);
        var tres   = TresFile.Read(path);
        var values = tres.Resource;
        var script = type == AffixType.Prefix ? "res://Resources/Affixes/Prefixes/Prefix.cs" : "res://Resources/Affixes/Suffixes/Suffix.cs";

        Assert.Multiple(() =>
        {
            Assert.That(tres.ScriptPath, Is.EqualTo(script));
            Assert.That(values.Enum("AffectedCombatStat", CombatStat.Life), Is.EqualTo(stat));
            Assert.That(values.Enum("ModificationType", ModificationType.Flat), Is.EqualTo(modification));
            Assert.That(values.Bool("IsInherentMod", false), Is.EqualTo(isLocal));
            Assert.That(values.Bool("AllowFractions", false), Is.EqualTo(allowsFractions));
            Assert.That(IntArray(values.String("AffectableItemTypes")), Is.EqualTo(new[] { (int)ItemSlot.PhysicalWeapon }));
            Assert.That(IntArray(values.String("AffectableWeaponTypes")), Is.EqualTo(new[] { (int)WeaponType.Sword }));
        });
    }

    //Höhere Stufen kommen später und sind stärker. Bei "reduced" zählt der Betrag, bei "Adds X to Y" beide Werte
    [TestCaseSource(nameof(SwordAffixFiles))]
    public void Stufen_SindVollstaendigUndSteigen(string path)
    {
        var tiers = Tiers(path);

        Assert.That(tiers.Select(tier => tier.Tier), Is.EqualTo(Enumerable.Range(1, tiers.Count).Reverse()));

        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];

            Assert.That(tier.NameAddition, Is.Not.Empty, $"Stufe {tier.Tier}");
            Assert.That(tier.Weight, Is.GreaterThan(0), $"Stufe {tier.Tier}");
            Assert.That(tier.MinValue, Is.LessThanOrEqualTo(tier.MaxValue), $"Stufe {tier.Tier}");

            if (tier.HasRange)
            {
                Assert.That(tier.MinValueTo, Is.LessThanOrEqualTo(tier.MaxValueTo), $"Stufe {tier.Tier}");
                Assert.That(tier.MaxValue, Is.LessThanOrEqualTo(tier.MinValueTo), $"Stufe {tier.Tier}");
            }

            if (i == 0)
                continue;

            Assert.That(tier.MinItemLevel, Is.GreaterThanOrEqualTo(tiers[i - 1].MinItemLevel), $"Stufe {tier.Tier}");
            Assert.That(Strength(tier), Is.GreaterThan(Strength(tiers[i - 1])), $"Stufe {tier.Tier}");
        }
    }

    [Test]
    public void AddsXtoY_HabenEineZweiteSpanne()
    {
        var added = SwordAffixFiles().Where(path => Path.GetFileName(path).StartsWith("Added", StringComparison.Ordinal)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(added, Has.Count.EqualTo(4));
            Assert.That(added.SelectMany(Tiers), Has.All.Matches<AffixTierDefinition>(tier => tier.HasRange));
            Assert.That(SwordAffixFiles().Except(added).SelectMany(Tiers), Has.None.Matches<AffixTierDefinition>(tier => tier.HasRange));
        });
    }

    [Test]
    public void NamenDerSchwerter_KommenSonstNirgendsVor()
    {
        var swordFiles = SwordAffixFiles().ToHashSet();
        var swordNames = swordFiles.SelectMany(Tiers).Select(tier => tier.NameAddition).ToList();
        var otherNames = Files("res://Resources/Affixes").Where(path => !swordFiles.Contains(path))
                                                         .SelectMany(Tiers)
                                                         .Select(tier => tier.NameAddition.Trim())
                                                         .ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(swordNames, Is.Unique);
            Assert.That(swordNames.Where(otherNames.Contains), Is.Empty);
        });
    }

    [Test]
    public void Itemlevel1_KannPrefixUndSuffixWuerfeln()
    {
        var minimumLevels = SwordAffixFiles().ToLookup(path => path.Contains("Prefixes"), path => Tiers(path).Min(tier => tier.MinItemLevel));

        Assert.Multiple(() =>
        {
            Assert.That(minimumLevels[true], Has.Some.EqualTo(1));
            Assert.That(minimumLevels[false], Has.Some.EqualTo(1));
        });
    }

    private static float Strength(AffixTierDefinition tier)
        => Math.Abs(tier.MaxValue) + tier.MaxValueTo;

    private static IEnumerable<string> Files(string resFolder)
        => Directory.GetFiles(GameData.ToFile(resFolder), "*.tres", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal);

    private static List<AffixTierDefinition> Tiers(string path)
    {
        var file = TresFile.Read(path);

        return file.Resource.References("Tiers")
                   .Select(reference => file.Sub(reference.Id))
                   .Select(tier => new AffixTierDefinition(tier.Int("Tier", 0),
                                                           tier.Int("MinItemLevelToAppearOn", 0),
                                                           tier.Int("Weight", 0),
                                                           tier.Float("MinValue", 0f),
                                                           tier.Float("MaxValue", 0f),
                                                           tier.String("ItemnameAddition"),
                                                           tier.Float("MinValueTo", 0f),
                                                           tier.Float("MaxValueTo", 0f)))
                   .ToList();
    }

    //Ein Array aus Zahlen steht als "Array[int]([1, 18])" in der Datei
    private static int[] IntArray(string text)
    {
        var match = Regex.Match(text, @"\(\[(.*)\]\)");

        return match.Success && match.Groups[1].Value.Trim().Length > 0
                ? match.Groups[1].Value.Split(',').Select(part => int.Parse(part.Trim(), CultureInfo.InvariantCulture)).ToArray()
                : [];
    }
}
