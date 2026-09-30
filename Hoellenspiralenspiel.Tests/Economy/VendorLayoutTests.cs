using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class VendorLayoutTests
{
    private const int StockSize = 20;

    private static readonly ItemDefinition[] BasesWithRealSizes =
        [TestItems.Sword, TestItems.Bow, TestItems.Staff, TestItems.Shield, TestItems.Helmet, ItemTypeOrderTests.Tunic, ItemTypeOrderTests.Gloves];

    private static IEnumerable<TestCaseData> SingleBases
        => BasesWithRealSizes.Select(definition => new TestCaseData(definition).SetArgDisplayNames(definition.Id));

    private static List<ItemInstance> InColumnOrder(InventoryGrid grid)
        => grid.GetItemsInReadingOrder().OrderBy(item => grid.GetPositionOf(item).X).ThenBy(item => grid.GetPositionOf(item).Y).ToList();

    private static bool ReadsInTypeOrderDownEveryColumn(InventoryGrid grid)
    {
        for (var x = 0; x < grid.Width; x++)
        {
            var ranks = grid.GetItemsInReadingOrder()
                            .Where(item => grid.GetPositionOf(item).X <= x && x < grid.GetPositionOf(item).X + item.Definition.Width)
                            .OrderBy(item => grid.GetPositionOf(item).Y)
                            .Select(item => ItemTypeOrder.GetRank(item.Definition))
                            .ToList();

            if (ranks.Zip(ranks.Skip(1)).Any(pair => pair.First > pair.Second))
                return false;
        }

        return true;
    }

    private static int CountPlaced(IEnumerable<ItemDefinition> definitions)
    {
        var vendor = new Vendor();

        vendor.Restock(definitions.Select(definition => TestItems.Create(definition)).ToList(), 1);

        return vendor.Stock.Count;
    }

    [Test]
    public void Restock_LegtDenBestandSpalteFuerSpalteNachItemtypAus()
    {
        var vendor  = new Vendor();
        var sword   = TestItems.Create(TestItems.Sword);
        var bow     = TestItems.Create(TestItems.Bow);
        var staff   = TestItems.Create(TestItems.Staff);
        var shield  = TestItems.Create(TestItems.Shield);
        var helmet  = TestItems.Create(TestItems.Helmet);
        var tunic   = TestItems.Create(ItemTypeOrderTests.Tunic);
        var gloves  = TestItems.Create(ItemTypeOrderTests.Gloves);
        var changes = 0;

        vendor.Changed += () => changes++;

        vendor.Restock([gloves, helmet, staff, tunic, sword, shield, bow], 1);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.GetPositionOf(sword), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.Stock.GetPositionOf(bow), Is.EqualTo(new GridCell(0, 3)));
            Assert.That(vendor.Stock.GetPositionOf(staff), Is.EqualTo(new GridCell(0, 6)));
            Assert.That(vendor.Stock.GetPositionOf(shield), Is.EqualTo(new GridCell(1, 0)));
            Assert.That(vendor.Stock.GetPositionOf(helmet), Is.EqualTo(new GridCell(1, 2)));
            Assert.That(vendor.Stock.GetPositionOf(tunic), Is.EqualTo(new GridCell(1, 4)));
            Assert.That(vendor.Stock.GetPositionOf(gloves), Is.EqualTo(new GridCell(1, 7)));
            Assert.That(InColumnOrder(vendor.Stock), Is.EqualTo(new[] { sword, bow, staff, shield, helmet, tunic, gloves }));
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void SetWares_SortiertNachTyp()
    {
        var vendor = new Vendor();
        var mana   = TestItems.Create(ItemTypeOrderTests.ManaPotion);
        var health = TestItems.Create(TestItems.Potion);

        vendor.SetWares([mana, health]);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Wares.GetPositionOf(health), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.Wares.GetPositionOf(mana), Is.EqualTo(new GridCell(0, 1)));
        });
    }

    [Test]
    public void EinKauf_LaesstEineLuecke()
    {
        var vendor = new Vendor();
        var sword  = TestItems.Create(TestItems.Sword);
        var bow    = TestItems.Create(TestItems.Bow);
        var staff  = TestItems.Create(TestItems.Staff);

        vendor.Restock([staff, bow, sword], 1);
        vendor.TakeFromStock(bow);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.GetItemAt(new GridCell(0, 3)), Is.Null);
            Assert.That(vendor.Stock.GetPositionOf(sword), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.Stock.GetPositionOf(staff), Is.EqualTo(new GridCell(0, 6)));
        });
    }

    [Test]
    public void DerRueckkauf_BleibtInDerReihenfolgeDesVerkaufs()
    {
        var vendor = new Vendor(6, 4);
        var potion = TestItems.Create(TestItems.Potion);
        var helmet = TestItems.Create(TestItems.Helmet);
        var sword  = TestItems.Create(TestItems.Sword);

        vendor.AddToBuyback(potion);
        vendor.AddToBuyback(helmet);
        vendor.AddToBuyback(sword);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Buyback.GetPositionOf(potion), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.Buyback.GetPositionOf(helmet), Is.EqualTo(new GridCell(1, 0)));
            Assert.That(vendor.Buyback.GetPositionOf(sword), Is.EqualTo(new GridCell(3, 0)));
        });
    }

    [TestCaseSource(nameof(SingleBases))]
    public void ZwanzigGleicheStuecke_PassenInsGitter(ItemDefinition definition)
        => Assert.That(CountPlaced(Enumerable.Repeat(definition, StockSize)), Is.EqualTo(StockSize));

    [Test]
    public void ZwanzigStueckAusZweiBasen_PassenInJederMischung()
    {
        for (var i = 0; i < BasesWithRealSizes.Length; i++)
        {
            for (var j = i + 1; j < BasesWithRealSizes.Length; j++)
            {
                var first  = BasesWithRealSizes[i];
                var second = BasesWithRealSizes[j];

                for (var firstCount = 0; firstCount <= StockSize; firstCount++)
                {
                    var mix = Enumerable.Repeat(first, firstCount).Concat(Enumerable.Repeat(second, StockSize - firstCount));

                    Assert.That(CountPlaced(mix), Is.EqualTo(StockSize), $"{firstCount} {first.Id}, {StockSize - firstCount} {second.Id}");
                }
            }
        }
    }

    [Test]
    public void ZwanzigBeliebigeStuecke_PassenImmerInsGitter()
    {
        for (var seed = 0; seed < 2000; seed++)
        {
            var random = new SeededRandom(seed);
            var drawn  = Enumerable.Range(0, StockSize).Select(_ => BasesWithRealSizes[random.NextInt(0, BasesWithRealSizes.Length)]).ToList();

            Assert.That(CountPlaced(drawn), Is.EqualTo(StockSize), $"Seed {seed}");
        }
    }

    [Test]
    public void SpaetereTypen_RutschenNichtInLueckenFruehererSpalten()
    {
        var vendor = new Vendor();
        var first  = TestItems.Create(TestItems.Staff);
        var second = TestItems.Create(TestItems.Staff);
        var tunic  = TestItems.Create(ItemTypeOrderTests.Tunic);
        var gloves = TestItems.Create(ItemTypeOrderTests.Gloves);

        vendor.Restock([gloves, tunic, second, first], 1);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.Count, Is.EqualTo(4));
            Assert.That(InColumnOrder(vendor.Stock).Select(item => item.Definition), Is.EqualTo(new[] { TestItems.Staff, TestItems.Staff, ItemTypeOrderTests.Tunic, ItemTypeOrderTests.Gloves }));
            Assert.That(vendor.Stock.GetItemAt(new GridCell(0, 8)), Is.Null);
        });
    }

    [Test]
    public void ZwanzigBeliebigeStuecke_LiegenInSpaltenrichtungNachTyp()
    {
        for (var seed = 0; seed < 2000; seed++)
        {
            var random = new SeededRandom(seed);
            var drawn  = Enumerable.Range(0, StockSize).Select(_ => TestItems.Create(BasesWithRealSizes[random.NextInt(0, BasesWithRealSizes.Length)])).ToList();
            var vendor = new Vendor();

            vendor.Restock(drawn, 1);

            Assert.That(InColumnOrder(vendor.Stock), Is.EqualTo(ItemTypeOrder.Sort(drawn)), $"Seed {seed}");
            Assert.That(ReadsInTypeOrderDownEveryColumn(vendor.Stock), Is.True, $"Seed {seed}");
        }
    }

    [Test]
    public void BreiteItemsNachSchmalen_BildenKeineTreppe()
    {
        var vendor = new Vendor();
        var first  = TestItems.Create(TestItems.Sword);
        var second = TestItems.Create(TestItems.Sword);
        var shield = TestItems.Create(TestItems.Shield);
        var helmet = TestItems.Create(TestItems.Helmet);
        var tunic  = TestItems.Create(ItemTypeOrderTests.Tunic);

        vendor.Restock([tunic, helmet, shield, second, first], 1);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.GetPositionOf(shield), Is.EqualTo(new GridCell(1, 0)));
            Assert.That(vendor.Stock.GetPositionOf(helmet), Is.EqualTo(new GridCell(1, 2)));
            Assert.That(vendor.Stock.GetPositionOf(tunic), Is.EqualTo(new GridCell(1, 4)));
            Assert.That(ReadsInTypeOrderDownEveryColumn(vendor.Stock), Is.True);
        });
    }

    [Test]
    public void WasHinterDemLetztenKeinenPlatzHat_FuelltEineLuecke()
    {
        var pole = ItemDefinition.ForWeapon("pole", "Pole", ItemSlot.SpellWeapon, new WeaponStats(1, 2, 1f, 5, WeaponType.Staff, WieldStrategy.TwoHand)) with { Height = Vendor.DefaultHeight };
        var ring = ItemDefinition.ForArmor("ring", "Ring", ItemSlot.Ring1, 0);

        var vendor = new Vendor();
        var sword  = TestItems.Create(TestItems.Sword);
        var poles  = Enumerable.Range(0, Vendor.DefaultWidth - 1).Select(_ => TestItems.Create(pole));
        var jewel  = TestItems.Create(ring);

        vendor.Restock([jewel, sword, ..poles], 1);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.Count, Is.EqualTo(Vendor.DefaultWidth + 1));
            Assert.That(vendor.Stock.GetPositionOf(sword), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.Stock.GetPositionOf(jewel), Is.EqualTo(new GridCell(0, 3)));
        });
    }
}
