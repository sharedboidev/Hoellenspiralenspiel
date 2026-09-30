using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class VendorStockRollerTests
{
    private static readonly ItemSlot[] AllSlots = [ItemSlot.PhysicalWeapon, ItemSlot.SpellWeapon, ItemSlot.Offhand, ItemSlot.Helmet];

    private static readonly AffixDefinition[] Affixes =
    [
        Create(AffixType.Prefix, CombatStat.Life, "Hearty"),
        Create(AffixType.Prefix, CombatStat.Armor, "Sturdy"),
        Create(AffixType.Prefix, CombatStat.Mana, "Wise"),
        Create(AffixType.Suffix, CombatStat.Strength, "of the Wrestler"),
        Create(AffixType.Suffix, CombatStat.Dexterity, "of the Fox"),
        Create(AffixType.Suffix, CombatStat.Intelligence, "of the Owl")
    ];

    private static readonly ItemDefinition[] Bases = [TestItems.Sword, TestItems.Staff, TestItems.Shield, TestItems.Helmet];

    private static readonly float OneInTwenty = VendorStockRoller.ChanceOfOneIn(20);
    private static readonly float OneInThirty = VendorStockRoller.ChanceOfOneIn(30);

    private static AffixDefinition Create(AffixType type, CombatStat stat, string name)
        => new(type, stat, ModificationType.Flat)
        {
            AllowedSlots = AllSlots,
            Tiers        = [new AffixTierDefinition(1, 1, 100, 1, 9, name)]
        };

    private static VendorStockRoller CreateRoller()
        => new(new AffixRoller(Affixes));

    private static List<ItemInstance> Roll(int count, int seed, int itemLevel = 1)
        => CreateRoller().Roll(Bases, count, itemLevel, OneInTwenty, OneInThirty, new SeededRandom(seed));

    [Test]
    public void DerBestandHatDieVerlangteGroesse()
        => Assert.That(Roll(20, 1), Has.Count.EqualTo(20));

    [Test]
    public void JedesStueckTraegtDasItemlevel()
        => Assert.That(Roll(20, 1, 7).Select(item => item.ItemLevel), Is.All.EqualTo(7));

    [Test]
    public void DieWareKommtAusDenBasen()
    {
        var offered = Roll(200, 5).Select(item => item.Definition.Id).Distinct();

        Assert.That(offered, Is.EquivalentTo(Bases.Select(definition => definition.Id)));
    }

    [Test]
    public void DieMeisteWareIstWeiss()
    {
        var stock  = Roll(30000, 4242);
        var magic  = stock.Count(item => item.Rarity == ItemRarity.Magic);
        var rare   = stock.Count(item => item.Rarity == ItemRarity.Rare);
        var normal = stock.Count(item => item.Rarity == ItemRarity.Normal);

        Assert.Multiple(() =>
        {
            Assert.That(magic, Is.InRange(1350, 1650), "jedes zwanzigste");
            Assert.That(rare, Is.InRange(850, 1150), "jedes dreissigste");
            Assert.That(normal, Is.EqualTo(30000 - magic - rare));
        });
    }

    [Test]
    public void Magic_TraegtEinBisZweiAffixe()
    {
        var magic = Roll(5000, 77).Where(item => item.Rarity == ItemRarity.Magic).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(magic, Is.Not.Empty);
            Assert.That(magic.Select(item => item.Affixes.Count), Is.All.InRange(1, 2));
            Assert.That(magic.Select(item => item.Affixes.Count).Distinct().Count(), Is.EqualTo(2));
        });
    }

    [Test]
    public void Rare_TraegtMindestensDreiAffixeUndEinenNamen()
    {
        var rare = Roll(5000, 77).Where(item => item.Rarity == ItemRarity.Rare).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rare, Is.Not.Empty);
            Assert.That(rare.Select(item => item.Affixes.Count), Is.All.GreaterThanOrEqualTo(3));
            Assert.That(rare.Select(item => item.RareName), Is.All.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void Rare_TraegtBeiHohemItemlevelAuchMehrAlsDreiAffixe()
    {
        var rare = Roll(5000, 77, 60).Where(item => item.Rarity == ItemRarity.Rare).ToList();

        Assert.That(rare.Max(item => item.Affixes.Count), Is.GreaterThan(3));
    }

    [Test]
    public void GleicherSeed_ErgibtGleichenBestand()
    {
        string Describe(ItemInstance item)
            => $"{item.Definition.Id}:{string.Join(",", item.Affixes.Select(affix => $"{affix.Stat}{affix.Value}"))}";

        var first  = Roll(200, 31337).Select(Describe).ToList();
        var second = Roll(200, 31337).Select(Describe).ToList();

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void OhneChancen_IstAllesWeiss()
    {
        var stock = CreateRoller().Roll(Bases, 500, 1, 0f, 0f, new SeededRandom(3));

        Assert.That(stock.Select(item => item.Rarity), Is.All.EqualTo(ItemRarity.Normal));
    }

    [Test]
    public void OhneBasen_BleibtDerBestandLeer()
        => Assert.That(CreateRoller().Roll([], 20, 1, OneInTwenty, OneInThirty, new SeededRandom(3)), Is.Empty);

    [TestCase(0)]
    [TestCase(-3)]
    public void OhneAnzahl_BleibtDerBestandLeer(int count)
        => Assert.That(Roll(count, 3), Is.Empty);

    [Test]
    public void DerWurfEntscheidetDieSeltenheit()
    {
        var roller = CreateRoller();

        ItemRarity RarityFor(float roll)
            => roller.Roll([TestItems.Sword], 1, 1, 0.05f, 0.03f, new FixedRandom(0.5f, 0f, roll)).Single().Rarity;

        Assert.Multiple(() =>
        {
            Assert.That(RarityFor(0.01f), Is.EqualTo(ItemRarity.Rare));
            Assert.That(RarityFor(0.05f), Is.EqualTo(ItemRarity.Magic));
            Assert.That(RarityFor(0.09f), Is.EqualTo(ItemRarity.Normal));
        });
    }

    [TestCase(20, 0.05f)]
    [TestCase(30, 1f / 30f)]
    [TestCase(1, 1f)]
    [TestCase(0, 0f)]
    [TestCase(-5, 0f)]
    public void ChanceOfOneIn_RechnetJedesNteInEineChanceUm(int every, float expected)
        => Assert.That(VendorStockRoller.ChanceOfOneIn(every), Is.EqualTo(expected).Within(0.0001f));

    [Test]
    public void AffixRoller_EineFesteAnzahl_WirdEingehalten()
    {
        var roller = new AffixRoller(Affixes);

        for (var seed = 0; seed < 200; seed++)
        {
            var item = TestItems.Create(TestItems.Helmet);

            roller.RollAffixesFor(item, new SeededRandom(seed), 3, 3);

            Assert.That(item.Affixes, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public void AffixRoller_OhneAngabe_WuerfeltWieBisher()
    {
        var roller = new AffixRoller(Affixes);
        var plain  = TestItems.Create(TestItems.Helmet, itemLevel: 30);
        var ranged = TestItems.Create(TestItems.Helmet, itemLevel: 30);

        roller.RollAffixesFor(plain, new SeededRandom(11));
        roller.RollAffixesFor(ranged, new SeededRandom(11), 0, AffixRoller.GetAffixCountCeiling(30));

        Assert.That(plain.Affixes.Select(affix => (affix.Stat, affix.Value)), Is.EqualTo(ranged.Affixes.Select(affix => (affix.Stat, affix.Value))));
    }

    [Test]
    public void AffixRoller_Verbrauchsgueter_BleibenOhneAffixe()
    {
        var potion = TestItems.Create(TestItems.Potion);

        new AffixRoller(Affixes).RollAffixesFor(potion, new SeededRandom(1), 3, 3);

        Assert.That(potion.Affixes, Is.Empty);
    }
}
