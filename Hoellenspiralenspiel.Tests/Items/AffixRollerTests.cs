using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class AffixRollerTests
{
    private static readonly AffixDefinition FlatDamage = new(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat)
    {
        AllowedSlots = [ItemSlot.PhysicalWeapon],
        IsLocal      = true,
        Tiers =
        [
            new AffixTierDefinition(2, 1, 100, 1, 6, "Rough"),
            new AffixTierDefinition(1, 15, 80, 7, 12, "Brutal")
        ]
    };

    private static readonly AffixDefinition IncreasedSpeed = new(AffixType.Suffix, CombatStat.Attackspeed, ModificationType.Percentage)
    {
        AllowedSlots = [ItemSlot.PhysicalWeapon, ItemSlot.SpellWeapon],
        IsLocal      = true,
        Tiers        = [new AffixTierDefinition(1, 1, 100, 5, 15, "of Quickness")]
    };

    private static readonly AffixDefinition FlatSpeed = new(AffixType.Suffix, CombatStat.Attackspeed, ModificationType.Flat)
    {
        AllowedSlots    = [ItemSlot.PhysicalWeapon],
        IsLocal         = true,
        AllowsFractions = true,
        Tiers           = [new AffixTierDefinition(1, 1, 100, 0.05f, 0.15f, "of Expediency")]
    };

    private static readonly AffixDefinition Strength = new(AffixType.Suffix, CombatStat.Strength, ModificationType.Flat)
    {
        AllowedSlots = [ItemSlot.Helmet, ItemSlot.PhysicalWeapon],
        Tiers        = [new AffixTierDefinition(1, 1, 100, 1, 2, "of the Wrestler")]
    };

    private static readonly AffixDefinition Life = new(AffixType.Prefix, CombatStat.Life, ModificationType.Flat)
    {
        AllowedSlots = [ItemSlot.Helmet, ItemSlot.PhysicalWeapon],
        Tiers        = [new AffixTierDefinition(1, 1, 100, 3, 9, "Hearty")]
    };

    private static readonly AffixDefinition SwordDamage = new(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Percentage)
    {
        AllowedSlots       = [ItemSlot.PhysicalWeapon],
        AllowedWeaponTypes = [WeaponType.Sword],
        IsLocal            = true,
        Tiers              = [new AffixTierDefinition(1, 1, 100, 40, 49, "Weighty")]
    };

    private static readonly AffixDefinition[] AllAffixes = [FlatDamage, IncreasedSpeed, FlatSpeed, Strength, Life];

    [TestCase(1, 3)]
    [TestCase(10, 3)]
    [TestCase(11, 5)]
    [TestCase(40, 6)]
    [TestCase(90, 11)]
    [TestCase(100, 12)]
    [TestCase(250, 12)]
    public void Itemlevel_BestimmtDieHoechstzahlDerAffixe(int itemLevel, int expected)
        => Assert.That(AffixRoller.GetAffixCountCeiling(itemLevel), Is.EqualTo(expected));

    [Test]
    public void KeinAffix_KommtDoppeltVor()
    {
        var roller = new AffixRoller(AllAffixes);

        for (var seed = 0; seed < 300; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword, itemLevel: 100);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            var kinds = sword.Affixes.Select(affix => (affix.Type, affix.Stat, affix.Modification)).ToArray();

            Assert.That(kinds, Is.Unique, $"Seed {seed}");
        }
    }

    [Test]
    public void Affixe_PassenZumSlotDesItems()
    {
        var roller = new AffixRoller(AllAffixes);

        for (var seed = 0; seed < 200; seed++)
        {
            var helmet = TestItems.Create(TestItems.Helmet, itemLevel: 100);

            roller.RollAffixesFor(helmet, new SeededRandom(seed));

            Assert.That(helmet.Affixes.Select(affix => affix.Stat), Is.SubsetOf(new[] { CombatStat.Strength, CombatStat.Life }), $"Seed {seed}");
        }
    }

    [Test]
    public void AffixMitWaffentyp_ErscheintNurAufDiesemTyp()
    {
        var roller = new AffixRoller([SwordDamage, FlatDamage]);
        var names  = new HashSet<string>();

        for (var seed = 0; seed < 200; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword, itemLevel: 1);
            var bow   = TestItems.Create(TestItems.Bow, itemLevel: 1);

            roller.RollAffixesFor(sword, new SeededRandom(seed));
            roller.RollAffixesFor(bow, new SeededRandom(seed));

            names.UnionWith(sword.Affixes.Select(affix => affix.NameAddition));

            Assert.That(bow.Affixes.Select(affix => affix.NameAddition), Has.None.EqualTo("Weighty"), $"Seed {seed}");
        }

        Assert.That(names, Does.Contain("Weighty"));
    }

    [Test]
    public void Waffentyp_PrueftAuchDenSlot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SwordDamage.CanAppearOn(TestItems.Sword), Is.True);
            Assert.That(SwordDamage.CanAppearOn(TestItems.Bow), Is.False);
            Assert.That(SwordDamage.CanAppearOn(TestItems.Helmet), Is.False);
            Assert.That(FlatDamage.CanAppearOn(TestItems.Bow), Is.True);
        });
    }

    [Test]
    public void Itemlevel_SchliesstHoehereStufenAus()
    {
        var roller = new AffixRoller([FlatDamage]);

        for (var seed = 0; seed < 200; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword, itemLevel: 14);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            Assert.That(sword.Affixes.Select(affix => affix.NameAddition), Has.None.EqualTo("Brutal"), $"Seed {seed}");
        }
    }

    [Test]
    public void HoheStufen_ErscheinenAbIhremItemlevel()
    {
        var roller = new AffixRoller([FlatDamage]);

        var names = Enumerable.Range(0, 200)
                              .SelectMany(seed =>
                              {
                                  var sword = TestItems.Create(TestItems.Sword, itemLevel: 15);

                                  roller.RollAffixesFor(sword, new SeededRandom(seed));

                                  return sword.Affixes.Select(affix => affix.NameAddition);
                              })
                              .Distinct()
                              .ToArray();

        Assert.That(names, Is.EquivalentTo(new[] { "Rough", "Brutal" }));
    }

    [Test]
    public void GanzzahligeWerte_LiegenInDerSpanneDerStufe()
    {
        var roller = new AffixRoller([Strength]);

        var values = Enumerable.Range(0, 300)
                               .SelectMany(seed =>
                               {
                                   var helmet = TestItems.Create(TestItems.Helmet);

                                   roller.RollAffixesFor(helmet, new SeededRandom(seed));

                                   return helmet.Affixes.Select(affix => affix.Value);
                               })
                               .Distinct()
                               .ToArray();

        Assert.That(values, Is.EquivalentTo(new[] { 1f, 2f }));
    }

    [Test]
    public void BruchWerte_LiegenInDerSpanneDerStufe()
    {
        var roller = new AffixRoller([FlatSpeed]);

        for (var seed = 0; seed < 200; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            Assert.That(sword.Affixes.Select(affix => affix.Value), Is.All.InRange(0.05f, 0.15f), $"Seed {seed}");
        }
    }

    [Test]
    public void ProzentWerte_WerdenZuBruechen()
    {
        var roller = new AffixRoller([IncreasedSpeed]);

        for (var seed = 0; seed < 200; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            Assert.That(sword.Affixes.Select(affix => affix.Value), Is.All.InRange(0.05f, 0.15f), $"Seed {seed}");
        }
    }

    [Test]
    public void Affix_UebernimmtArtNameUndWirkungsort()
    {
        var roller = new AffixRoller([FlatDamage]);
        var sword  = TestItems.Create(TestItems.Sword);

        //Würfe: Anzahl 3, Beginn mit Prefix, Stufe, Wert
        roller.RollAffixesFor(sword, new FixedRandom(0.5f, 0.99f, 0f, 0f, 0.99f));

        var expected = new ItemAffix(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat, 6, "Rough", true);

        Assert.That(sword.Affixes, Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void Traenke_BekommenKeineAffixe()
    {
        var roller = new AffixRoller(AllAffixes);
        var random = new FixedRandom(0.99f);
        var potion = TestItems.Create(TestItems.Potion);

        roller.RollAffixesFor(potion, random);

        Assert.Multiple(() =>
        {
            Assert.That(potion.Affixes, Is.Empty);
            Assert.That(random.Draws, Is.Zero);
        });
    }

    [Test]
    public void Obergrenze_BegrenztDieZahlDerAffixe()
    {
        var roller = new AffixRoller(AllAffixes, 1);

        for (var seed = 0; seed < 100; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword, itemLevel: 100);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            Assert.That(sword.Affixes, Has.Count.LessThanOrEqualTo(1), $"Seed {seed}");
        }
    }

    [Test]
    public void RareItems_BekommenEinenNamen_AndereNicht()
    {
        var roller = new AffixRoller(AllAffixes);

        for (var seed = 0; seed < 200; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword, itemLevel: 100);

            roller.RollAffixesFor(sword, new SeededRandom(seed));

            if (sword.Rarity == ItemRarity.Rare)
                Assert.That(sword.RareName, Is.Not.Null.And.Not.Empty, $"Seed {seed}");
            else
                Assert.That(sword.RareName, Is.Null, $"Seed {seed}");
        }
    }

    [Test]
    public void AlleSeltenheiten_KommenVor()
    {
        var roller = new AffixRoller(AllAffixes);

        var rarities = Enumerable.Range(0, 200)
                                 .Select(seed =>
                                 {
                                     var sword = TestItems.Create(TestItems.Sword, itemLevel: 100);

                                     roller.RollAffixesFor(sword, new SeededRandom(seed));

                                     return sword.Rarity;
                                 })
                                 .Distinct()
                                 .ToArray();

        Assert.That(rarities, Is.EquivalentTo(new[] { ItemRarity.Normal, ItemRarity.Magic, ItemRarity.Rare }));
    }

    [Test]
    public void GleicherSeed_ErgibtGleicheAffixe()
    {
        var roller = new AffixRoller(AllAffixes);
        var first  = TestItems.Create(TestItems.Sword, itemLevel: 100);
        var second = TestItems.Create(TestItems.Sword, itemLevel: 100);

        roller.RollAffixesFor(first, new SeededRandom(4711));
        roller.RollAffixesFor(second, new SeededRandom(4711));

        Assert.Multiple(() =>
        {
            Assert.That(second.Affixes, Is.EqualTo(first.Affixes));
            Assert.That(second.RareName, Is.EqualTo(first.RareName));
        });
    }

    [Test]
    public void OhnePassendeAffixe_BleibtDasItemNormal()
    {
        var roller = new AffixRoller(AllAffixes);
        var shield = TestItems.Create(TestItems.Shield, itemLevel: 100);

        roller.RollAffixesFor(shield, new FixedRandom(0.99f));

        Assert.That(shield.Rarity, Is.EqualTo(ItemRarity.Normal));
    }

    [Test]
    public void Namen_PassenZurArtDesItems()
    {
        var random = new SeededRandom(1);

        Assert.Multiple(() =>
        {
            Assert.That(ItemNameGenerator.Generate(TestItems.Sword, random).Split(' '), Has.Length.EqualTo(2));
            Assert.That(ItemNameGenerator.Generate(TestItems.Shield, new FixedRandom(0f)), Is.EqualTo("Imp Guard"));
            Assert.That(ItemNameGenerator.Generate(TestItems.Helmet, new FixedRandom(0f)), Is.EqualTo("Imp Hood"));
            Assert.That(ItemNameGenerator.Generate(TestItems.Sword, new FixedRandom(0f)), Is.EqualTo("Imp Scratch"));
        });
    }
}
