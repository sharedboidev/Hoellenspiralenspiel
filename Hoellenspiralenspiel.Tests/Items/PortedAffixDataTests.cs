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

//Die Affixe je Itemklasse stammen aus den Basis-Affixen eines gängigen aRPG, der Vorlage, mit allen Stufen und eigenen Namen.
//Rüstungsteile nehmen die Fassung mit Armour, der Stab die der Zauberstäbe
[TestFixture]
public class PortedAffixDataTests
{
    private const string AffixFolder = "res://Resources/Affixes";

    private static readonly PortedClass[] Classes =
    [
        new("Weapons/Swords", ItemSlot.PhysicalWeapon, WeaponType.Sword,
            ["AddedFireDamage", "AddedFrostDamage", "AddedLightningDamage", "AddedPhysicalDamage", "IncreasedElementalDamage", "IncreasedPhysicalDamage"],
            ["DamageOverTimeMultiplier", "FlatCriticalDamage", "FlatDexterity", "FlatStrength", "IncreasedAttackspeed", "IncreasedCriticalHitChance", "LifeLeech", "LifeOnHit", "LifeOnKill", "ManaLeech", "ManaOnKill", "ReducedAttributeRequirements"]),
        new("Weapons/Bows", ItemSlot.PhysicalWeapon, WeaponType.Bow,
            ["AddedFireDamage", "AddedFrostDamage", "AddedLightningDamage", "AddedPhysicalDamage", "IncreasedElementalDamage", "IncreasedPhysicalDamage"],
            ["AdditionalArrows", "DamageOverTimeMultiplier", "FlatCriticalDamage", "FlatDexterity", "IncreasedAttackspeed", "IncreasedCriticalHitChance", "IncreasedProjectileSpeed", "LifeLeech", "LifeOnHit", "LifeOnKill", "ManaLeech", "ManaOnKill", "ReducedAttributeRequirements"]),
        new("Weapons/Staves", ItemSlot.SpellWeapon, WeaponType.Staff,
            ["AddedFireDamage", "AddedFireDamageToSpells", "AddedFrostDamage", "AddedFrostDamageToSpells", "AddedLightningDamage", "AddedLightningDamageToSpells", "AddedPhysicalDamage", "FlatMana", "IncreasedElementalDamage", "IncreasedFireDamage", "IncreasedFrostDamage", "IncreasedLightningDamage", "IncreasedPhysicalDamage", "IncreasedSpellDamage", "IncreasedSpellDamageAndMana"],
            ["DamageOverTimeMultiplier", "FireDamageOverTimeMultiplier", "FlatCriticalDamage", "FlatIntelligence", "FlatMeleeBlock", "FlatSpellBlock", "FlatStrength", "IncreasedAttackspeed", "IncreasedCastSpeed", "IncreasedCriticalHitChance", "IncreasedManaRegeneration", "IncreasedSpellCriticalHitChance", "LifeLeech", "LifeOnHit", "LifeOnKill", "ManaLeech", "ManaOnKill", "PhysicalDamageOverTimeMultiplier", "Proliferate", "ReducedAttributeRequirements"]),
        new("Armors/Helmets", ItemSlot.Helmet, null,
            ["FlatArmor", "FlatArmorAndLife", "FlatLife", "IncreasedArmor", "ReflectPhysicalDamage"],
            ["FireResistance", "FlatIntelligence", "FlatLifeRegeneration", "FlatStrength", "FrostResistance", "IncreasedLifeRegeneration", "LightningResistance", "ReducedAttributeRequirements"]),
        new("Armors/BodyArmours", ItemSlot.Torso, null,
            ["FlatArmor", "FlatArmorAndLife", "FlatLife", "IncreasedArmor", "ReflectPhysicalDamage"],
            ["FireResistance", "FlatLifeRegeneration", "FlatStrength", "FrostResistance", "LightningResistance", "PhysicalDamageReduction", "ReducedAttributeRequirements"]),
        new("Armors/Gloves", ItemSlot.Hands, null,
            ["AddedFireDamageToAttacks", "AddedFrostDamageToAttacks", "AddedLightningDamageToAttacks", "AddedPhysicalDamageToAttacks", "FlatArmor", "FlatArmorAndLife", "FlatLife", "IncreasedArmor"],
            ["FireResistance", "FlatDexterity", "FlatLifeRegeneration", "FlatStrength", "FrostResistance", "IncreasedAttackspeed", "IncreasedLifeRegeneration", "LifeLeech", "LifeOnHit", "LifeOnKill", "LightningResistance", "ManaLeech", "ManaOnKill", "ReducedAttributeRequirements"]),
        new("Armors/Shields", ItemSlot.Offhand, null,
            ["FlatArmor", "FlatArmorAndLife", "FlatLife", "FlatSpellBlock", "IncreasedArmor", "IncreasedBlockChance", "ReflectPhysicalDamage"],
            ["AilmentAvoidance", "AllElementalResistances", "AllMaximumResistances", "FireResistance", "FlatLifeRegeneration", "FlatStrength", "FrostResistance", "LightningResistance", "MaxFireResistance", "MaxFrostResistance", "MaxLightningResistance", "PhysicalDamageReduction", "ReducedAttributeRequirements", "ReducedCriticalDamageTaken"])
    ];

    //Je Datei der Stat, die Art und ob der Wert Brüche hat. Gleiche Dateinamen bedeuten in jeder Klasse dasselbe
    private static readonly Dictionary<string, (CombatStat Stat, ModificationType Modification, bool AllowsFractions)> Families = new()
    {
        ["AddedFireDamage"]                  = (CombatStat.FireDamage, ModificationType.Flat, false),
        ["AddedFireDamageToAttacks"]         = (CombatStat.AddedFireToAttacks, ModificationType.Flat, false),
        ["AddedFireDamageToSpells"]          = (CombatStat.AddedFireToSpells, ModificationType.Flat, false),
        ["AddedFrostDamage"]                 = (CombatStat.FrostDamage, ModificationType.Flat, false),
        ["AddedFrostDamageToAttacks"]        = (CombatStat.AddedFrostToAttacks, ModificationType.Flat, false),
        ["AddedFrostDamageToSpells"]         = (CombatStat.AddedFrostToSpells, ModificationType.Flat, false),
        ["AddedLightningDamage"]             = (CombatStat.LightningDamage, ModificationType.Flat, false),
        ["AddedLightningDamageToAttacks"]    = (CombatStat.AddedLightningToAttacks, ModificationType.Flat, false),
        ["AddedLightningDamageToSpells"]     = (CombatStat.AddedLightningToSpells, ModificationType.Flat, false),
        ["AddedPhysicalDamage"]              = (CombatStat.PhysicalDamage, ModificationType.Flat, false),
        ["AddedPhysicalDamageToAttacks"]     = (CombatStat.AddedPhysicalToAttacks, ModificationType.Flat, false),
        ["AdditionalArrows"]                 = (CombatStat.ProjectileCount, ModificationType.Flat, false),
        ["AilmentAvoidance"]                 = (CombatStat.AilmentAvoidance, ModificationType.Flat, false),
        ["AllElementalResistances"]          = (CombatStat.AllElementalResistances, ModificationType.Flat, false),
        ["AllMaximumResistances"]            = (CombatStat.AllMaximumResistances, ModificationType.Flat, false),
        ["DamageOverTimeMultiplier"]         = (CombatStat.DamageOverTime, ModificationType.More, false),
        ["FireDamageOverTimeMultiplier"]     = (CombatStat.FireDamageOverTime, ModificationType.More, false),
        ["FlatArmorAndLife"]                 = (CombatStat.Armor, ModificationType.Flat, false),
        ["IncreasedBlockChance"]             = (CombatStat.MeleeBlock, ModificationType.Percentage, false),
        ["IncreasedCastSpeed"]               = (CombatStat.CastSpeed, ModificationType.Percentage, false),
        ["IncreasedProjectileSpeed"]         = (CombatStat.ProjectileSpeed, ModificationType.Percentage, false),
        ["IncreasedSpellCriticalHitChance"]  = (CombatStat.SpellCriticalHitChance, ModificationType.Percentage, false),
        ["IncreasedSpellDamageAndMana"]      = (CombatStat.SpellDamage, ModificationType.Percentage, false),
        ["MaxFireResistance"]                = (CombatStat.MaxFireResistance, ModificationType.Flat, false),
        ["MaxFrostResistance"]               = (CombatStat.MaxFrostResistance, ModificationType.Flat, false),
        ["MaxLightningResistance"]           = (CombatStat.MaxLightningResistance, ModificationType.Flat, false),
        ["PhysicalDamageOverTimeMultiplier"] = (CombatStat.PhysicalDamageOverTime, ModificationType.More, false),
        ["PhysicalDamageReduction"]          = (CombatStat.Damagereduction, ModificationType.Flat, false),
        //Ohne Vorlage: weitere Sprünge für Chain Lightning, auf dem Stab wie die Zusatzpfeile am Bogen
        ["Proliferate"]                      = (CombatStat.Proliferate, ModificationType.Flat, false),
        ["ReducedCriticalDamageTaken"]       = (CombatStat.ReducedCriticalDamageTaken, ModificationType.Flat, false),
        ["ReflectPhysicalDamage"]            = (CombatStat.ReflectPhysical, ModificationType.Flat, false),
        ["FireResistance"]               = (CombatStat.FireResistance, ModificationType.Flat, false),
        ["FlatArmor"]                    = (CombatStat.Armor, ModificationType.Flat, false),
        ["FlatCriticalDamage"]           = (CombatStat.CriticalDamage, ModificationType.Flat, false),
        ["FlatDexterity"]                = (CombatStat.Dexterity, ModificationType.Flat, false),
        ["FlatIntelligence"]             = (CombatStat.Intelligence, ModificationType.Flat, false),
        ["FlatLife"]                     = (CombatStat.Life, ModificationType.Flat, false),
        ["FlatLifeRegeneration"]         = (CombatStat.Liferegeneration, ModificationType.Flat, true),
        ["FlatMana"]                     = (CombatStat.Mana, ModificationType.Flat, false),
        ["FlatMeleeBlock"]               = (CombatStat.MeleeBlock, ModificationType.Flat, false),
        ["FlatSpellBlock"]               = (CombatStat.SpellBlock, ModificationType.Flat, false),
        ["FlatStrength"]                 = (CombatStat.Strength, ModificationType.Flat, false),
        ["FrostResistance"]              = (CombatStat.FrostResistance, ModificationType.Flat, false),
        ["IncreasedArmor"]               = (CombatStat.Armor, ModificationType.Percentage, false),
        ["IncreasedAttackspeed"]         = (CombatStat.Attackspeed, ModificationType.Percentage, false),
        ["IncreasedCriticalHitChance"]   = (CombatStat.CriticalHitChance, ModificationType.Percentage, false),
        ["IncreasedElementalDamage"]     = (CombatStat.ElementalAttackDamage, ModificationType.Percentage, false),
        ["IncreasedFireDamage"]          = (CombatStat.FireDamage, ModificationType.Percentage, false),
        ["IncreasedFrostDamage"]         = (CombatStat.FrostDamage, ModificationType.Percentage, false),
        ["IncreasedLifeRegeneration"]    = (CombatStat.Liferegeneration, ModificationType.Percentage, false),
        ["IncreasedLightningDamage"]     = (CombatStat.LightningDamage, ModificationType.Percentage, false),
        ["IncreasedManaRegeneration"]    = (CombatStat.Manaregeneration, ModificationType.Percentage, false),
        ["IncreasedPhysicalDamage"]      = (CombatStat.PhysicalDamage, ModificationType.Percentage, false),
        ["IncreasedSpellDamage"]         = (CombatStat.SpellDamage, ModificationType.Percentage, false),
        ["LifeLeech"]                    = (CombatStat.Leech, ModificationType.Flat, true),
        ["LifeOnHit"]                    = (CombatStat.LifeOnHit, ModificationType.Flat, false),
        ["LifeOnKill"]                   = (CombatStat.LifeOnKill, ModificationType.Flat, false),
        ["LightningResistance"]          = (CombatStat.LightningResistance, ModificationType.Flat, false),
        ["ManaLeech"]                    = (CombatStat.ManaLeech, ModificationType.Flat, true),
        ["ManaOnKill"]                   = (CombatStat.ManaOnKill, ModificationType.Flat, false),
        ["ReducedAttributeRequirements"] = (CombatStat.AttributeRequirements, ModificationType.Percentage, false)
    };

    //Lokal verändert ein Affix das Item selbst. Angriffstempo und Krit sind das nur auf einer Waffe, auf Handschuhen gelten sie dem Helden
    private static readonly HashSet<string> LocalOnWeapons = ["AddedFireDamage", "AddedFrostDamage", "AddedLightningDamage", "AddedPhysicalDamage", "AdditionalArrows", "IncreasedAttackspeed", "IncreasedCriticalHitChance", "IncreasedPhysicalDamage", "ReducedAttributeRequirements"];
    private static readonly HashSet<string> LocalOnArmour  = ["FlatArmor", "FlatArmorAndLife", "IncreasedArmor", "IncreasedBlockChance", "ReducedAttributeRequirements"];

    //Der zweite Stat der hybriden Affixe, beide global
    private static readonly Dictionary<string, (CombatStat Stat, ModificationType Modification)> Hybrids = new()
    {
        ["FlatArmorAndLife"]            = (CombatStat.Life, ModificationType.Flat),
        ["IncreasedSpellDamageAndMana"] = (CombatStat.Mana, ModificationType.Flat)
    };

    private static IEnumerable<TestCaseData> ClassCases()
        => Classes.Select(portedClass => new TestCaseData(portedClass).SetName($"Klasse_{portedClass.Folder.Replace('/', '_')}"));

    private static IEnumerable<TestCaseData> FileCases()
        => Classes.SelectMany(portedClass => portedClass.Files().Select(file => new TestCaseData(portedClass, file).SetName($"Datei_{portedClass.Folder.Replace('/', '_')}_{Path.GetFileNameWithoutExtension(file)}")));

    private static IEnumerable<string> AllPortedFiles()
        => Classes.SelectMany(portedClass => portedClass.Files());

    [TestCaseSource(nameof(ClassCases))]
    public void Klasse_HatGenauDieErwartetenAffixe(PortedClass portedClass)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Names(portedClass.PrefixFolder), Is.EquivalentTo(portedClass.Prefixes));
            Assert.That(Names(portedClass.SuffixFolder), Is.EquivalentTo(portedClass.Suffixes));
        });
    }

    [TestCaseSource(nameof(FileCases))]
    public void Affix_HatStatArtUndOrtWieErwartet(PortedClass portedClass, string path)
    {
        var name     = Path.GetFileNameWithoutExtension(path);
        var family   = Families[name];
        var tres     = TresFile.Read(path);
        var values   = tres.Resource;
        var isPrefix = path.Contains($"{Path.DirectorySeparatorChar}Prefixes{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
        var isLocal  = (portedClass.WeaponType is null ? LocalOnArmour : LocalOnWeapons).Contains(name);
        var script   = isPrefix ? "res://Resources/Affixes/Prefixes/Prefix.cs" : "res://Resources/Affixes/Suffixes/Suffix.cs";
        var weapons  = portedClass.WeaponType is { } weaponType ? new[] { (int)weaponType } : [];

        Assert.Multiple(() =>
        {
            Assert.That(tres.ScriptPath, Is.EqualTo(script));
            Assert.That(values.Enum("AffectedCombatStat", CombatStat.Life), Is.EqualTo(family.Stat));
            Assert.That(values.Enum("ModificationType", ModificationType.Flat), Is.EqualTo(family.Modification));
            Assert.That(values.Bool("AllowFractions", false), Is.EqualTo(family.AllowsFractions));
            Assert.That(values.Bool("IsInherentMod", false), Is.EqualTo(isLocal));
            Assert.That(IntArray(values.String("AffectableItemTypes")), Is.EqualTo(new[] { (int)portedClass.Slot }));
            Assert.That(IntArray(values.String("AffectableWeaponTypes")), Is.EqualTo(weapons));
            Assert.That(values.Bool("IsHybrid", false), Is.EqualTo(Hybrids.ContainsKey(name)));

            if (Hybrids.TryGetValue(name, out var hybrid))
            {
                Assert.That(values.Enum("HybridCombatStat", CombatStat.Life), Is.EqualTo(hybrid.Stat));
                Assert.That(values.Enum("HybridModificationType", ModificationType.Flat), Is.EqualTo(hybrid.Modification));
                Assert.That(values.Bool("HybridIsInherentMod", false), Is.False);
            }
        });
    }

    //Höhere Stufen kommen später und sind stärker. Bei "reduced" zählt der Betrag, bei "Adds X to Y" beide Werte
    [TestCaseSource(nameof(FileCases))]
    public void Stufen_SindVollstaendigUndSteigen(PortedClass portedClass, string path)
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

            if (Hybrids.ContainsKey(Path.GetFileNameWithoutExtension(path)))
            {
                Assert.That(tier.HybridMinValue, Is.GreaterThan(0f), $"Stufe {tier.Tier}");
                Assert.That(tier.HybridMinValue, Is.LessThanOrEqualTo(tier.HybridMaxValue), $"Stufe {tier.Tier}");
            }
            else
            {
                Assert.That(tier.HybridMaxValue, Is.Zero, $"Stufe {tier.Tier}");
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
        var added = AllPortedFiles().Where(path => Path.GetFileName(path).StartsWith("Added", StringComparison.Ordinal)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(added, Has.Count.EqualTo(19), "vier je Waffe, vier für Angriffe auf Handschuhen, drei für Zauber auf dem Stab");
            Assert.That(added.SelectMany(Tiers), Has.All.Matches<AffixTierDefinition>(tier => tier.HasRange));
            Assert.That(AllPortedFiles().Except(added).SelectMany(Tiers), Has.None.Matches<AffixTierDefinition>(tier => tier.HasRange));
        });
    }

    //Ein Name aus der Vorlage steht in jedem Affix des Spiels für dieselbe Familie, wie dort dieselbe Stufe überall gleich heißt.
    //Die alten Affixe teilen sich untereinander zwei Namen ("of Quickness", "of the Lizard"), das bereinigt Etappe 11
    [Test]
    public void JederName_GehoertZuGenauEinerFamilie()
    {
        var ported = AllPortedFiles().SelectMany(Tiers).Select(tier => tier.NameAddition.Trim()).ToHashSet();

        var families = Directory.GetFiles(GameData.ToFile(AffixFolder), "*.tres", SearchOption.AllDirectories)
                                .SelectMany(path => Tiers(path).Select(tier => (Name: tier.NameAddition.Trim(), Family: FamilyOf(path))))
                                .Where(entry => ported.Contains(entry.Name))
                                .GroupBy(entry => entry.Name)
                                .Where(group => group.Select(entry => entry.Family).Distinct().Count() > 1)
                                .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.Family).Distinct())}");

        Assert.That(families, Is.Empty);
    }

    [TestCaseSource(nameof(FileCases))]
    public void NamenInnerhalbEinerDatei_SindVerschieden(PortedClass portedClass, string path)
        => Assert.That(Tiers(path).Select(tier => tier.NameAddition), Is.Unique);

    [TestCaseSource(nameof(ClassCases))]
    public void Itemlevel1_KannPrefixUndSuffixWuerfeln(PortedClass portedClass)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Files(portedClass.PrefixFolder).Select(path => Tiers(path).Min(tier => tier.MinItemLevel)), Has.Some.EqualTo(1));
            Assert.That(Files(portedClass.SuffixFolder).Select(path => Tiers(path).Min(tier => tier.MinItemLevel)), Has.Some.EqualTo(1));
        });
    }

    //"Adds X to Y Fire Damage" heißt in der Vorlage gleich, ob auf der Waffe, für alle Angriffe oder für Zauber. Hier sind das drei Stats einer Familie
    private static readonly Dictionary<CombatStat, CombatStat> SameFamilyAs = new()
    {
        [CombatStat.AddedPhysicalToAttacks]  = CombatStat.PhysicalDamage,
        [CombatStat.AddedFireToAttacks]      = CombatStat.FireDamage,
        [CombatStat.AddedFireToSpells]       = CombatStat.FireDamage,
        [CombatStat.AddedFrostToAttacks]     = CombatStat.FrostDamage,
        [CombatStat.AddedFrostToSpells]      = CombatStat.FrostDamage,
        [CombatStat.AddedLightningToAttacks] = CombatStat.LightningDamage,
        [CombatStat.AddedLightningToSpells]  = CombatStat.LightningDamage
    };

    private static string FamilyOf(string path)
    {
        var values = TresFile.Read(path).Resource;
        var type   = path.Contains($"{Path.DirectorySeparatorChar}Prefixes{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ? AffixType.Prefix : AffixType.Suffix;
        var stat   = values.Enum("AffectedCombatStat", CombatStat.Life);

        return $"{type} {SameFamilyAs.GetValueOrDefault(stat, stat)} {values.Enum("ModificationType", ModificationType.Flat)}";
    }

    private static float Strength(AffixTierDefinition tier)
        => Math.Abs(tier.MaxValue) + tier.MaxValueTo;

    private static IEnumerable<string> Names(string resFolder)
        => Files(resFolder).Select(Path.GetFileNameWithoutExtension);

    private static IEnumerable<string> Files(string resFolder)
        => Directory.GetFiles(GameData.ToFile(resFolder), "*.tres").OrderBy(path => path, StringComparer.Ordinal);

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
                                                           tier.Float("MaxValueTo", 0f),
                                                           tier.Float("HybridMinValue", 0f),
                                                           tier.Float("HybridMaxValue", 0f)))
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

    public sealed record PortedClass(string Folder, ItemSlot Slot, WeaponType? WeaponType, string[] Prefixes, string[] Suffixes)
    {
        public string PrefixFolder => $"{AffixFolder}/Prefixes/{Folder}";
        public string SuffixFolder => $"{AffixFolder}/Suffixes/{Folder}";

        public IEnumerable<string> Files()
            => PortedAffixDataTests.Files(PrefixFolder).Concat(PortedAffixDataTests.Files(SuffixFolder));

        public override string ToString()
            => Folder;
    }
}
