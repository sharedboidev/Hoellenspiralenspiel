using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class PriceRuleTests
{
    private static readonly PriceRule Prices = new(3f, 8f, 0.25f);

    internal static ItemInstance CreateMagic(ItemDefinition definition)
    {
        var item = TestItems.Create(definition);

        item.AddAffix(TestItems.Global(CombatStat.Strength, ModificationType.Flat, 2));

        return item;
    }

    internal static ItemInstance CreateRare(ItemDefinition definition)
    {
        var item = TestItems.Create(definition);

        item.AddAffix(TestItems.Global(CombatStat.Strength, ModificationType.Flat, 2));
        item.AddAffix(TestItems.Global(CombatStat.Life, ModificationType.Flat, 9));
        item.AddAffix(TestItems.Local(CombatStat.PhysicalDamage, ModificationType.Flat, 3));

        item.RareName = "Grim Edge";

        return item;
    }

    [Test]
    public void EinWeissesItem_KostetSeinenGrundpreis()
        => Assert.That(Prices.GetBuyPrice(TestItems.Create(TestItems.Sword)), Is.EqualTo(20));

    [Test]
    public void Magic_KostetDasDreifache()
        => Assert.That(Prices.GetBuyPrice(CreateMagic(TestItems.Sword)), Is.EqualTo(60));

    [Test]
    public void Rare_KostetDasAchtfache()
        => Assert.That(Prices.GetBuyPrice(CreateRare(TestItems.Sword)), Is.EqualTo(160));

    [Test]
    public void DerHaendlerZahltEinViertel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Prices.GetSellPrice(TestItems.Create(TestItems.Sword)), Is.EqualTo(5));
            Assert.That(Prices.GetSellPrice(CreateMagic(TestItems.Sword)), Is.EqualTo(15));
            Assert.That(Prices.GetSellPrice(CreateRare(TestItems.Sword)), Is.EqualTo(40));
        });
    }

    [Test]
    public void DerAnkaufRundetAb()
        => Assert.That(Prices.GetSellPrice(TestItems.Create(TestItems.Potion)), Is.EqualTo(2));

    [Test]
    public void EinStapel_KostetJeStueck()
    {
        var potions = TestItems.Create(TestItems.Potion, 4);

        Assert.Multiple(() =>
        {
            Assert.That(Prices.GetUnitBuyPrice(potions), Is.EqualTo(10));
            Assert.That(Prices.GetBuyPrice(potions), Is.EqualTo(40));
            Assert.That(Prices.GetUnitSellPrice(potions), Is.EqualTo(2));
            Assert.That(Prices.GetSellPrice(potions), Is.EqualTo(8));
        });
    }

    [Test]
    public void OhneGrundpreis_IstDasItemNichtsWert()
    {
        var pebble = TestItems.Create(TestItems.Pebble);

        Assert.Multiple(() =>
        {
            Assert.That(Prices.GetBuyPrice(pebble), Is.Zero);
            Assert.That(Prices.GetSellPrice(pebble), Is.Zero);
        });
    }

    [Test]
    public void WasEinenPreisHat_BringtMindestensEineMuenze()
    {
        var cheap = ItemDefinition.ForArmor("rag", "Rag", ItemSlot.Helmet, 1) with { Price = 1 };

        Assert.That(Prices.GetSellPrice(TestItems.Create(cheap)), Is.EqualTo(1));
    }

    [Test]
    public void DerAnkaufKostetNieMehrAlsDerKauf()
    {
        var generous = new PriceRule(SellShare: 4f);

        Assert.That(generous.GetSellPrice(TestItems.Create(TestItems.Sword)), Is.EqualTo(20));
    }

    [Test]
    public void EinNegativerAnteil_ZahltDasMindeste()
    {
        var stingy = new PriceRule(SellShare: -1f);

        Assert.That(stingy.GetSellPrice(TestItems.Create(TestItems.Sword)), Is.EqualTo(1));
    }

    [Test]
    public void GrossePreiseLaufenNichtUeber()
    {
        var treasure = ItemDefinition.ForConsumable("gem", "Gem", new ConsumableEffect(ConsumableEffectKind.RestoreLife, 1), 100) with { Price = int.MaxValue / 2 };

        Assert.That(new PriceRule().GetBuyPrice(TestItems.Create(treasure, 100)), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void DieStandardwerteSindDreiAchtUndEinViertel()
    {
        var prices = new PriceRule();

        Assert.Multiple(() =>
        {
            Assert.That(prices.MagicFactor, Is.EqualTo(3f));
            Assert.That(prices.RareFactor, Is.EqualTo(8f));
            Assert.That(prices.SellShare, Is.EqualTo(0.25f));
        });
    }
}
