using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class TradeTests
{
    private sealed record Shop(Trade Trade, CharacterItems Items, Purse Gold, Vendor Vendor);

    private static Shop Open(int gold = 100, int inventoryWidth = 6, int inventoryHeight = 4, int vendorWidth = 6, int vendorHeight = 4)
    {
        var items  = TestItems.CreateCharacterItems(inventoryWidth, inventoryHeight);
        var purse  = new Purse();
        var vendor = new Vendor(vendorWidth, vendorHeight);

        purse.Add(gold);

        return new Shop(new Trade(items, purse, vendor, new PriceRule()), items, purse, vendor);
    }

    private static ItemInstance Stock(Shop shop, ItemInstance item)
    {
        shop.Vendor.Restock(shop.Vendor.Stock.GetItemsInReadingOrder().Append(item).ToList(), 1);

        return item;
    }

    private static ItemInstance OfferPotions(Shop shop)
    {
        var ware = TestItems.Create(TestItems.Potion);

        shop.Vendor.SetWares([ware]);

        return ware;
    }

    [Test]
    public void BuyStock_KostetGoldUndLegtDasItemInsInventar()
    {
        var shop  = Open();
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyStock(sword), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(80));
            Assert.That(shop.Items.Inventory.Contains(sword), Is.True);
            Assert.That(shop.Items.HeldItem, Is.Null);
            Assert.That(shop.Vendor.Stock.Contains(sword), Is.False);
        });
    }

    [Test]
    public void BuyStock_OhneGenugGold_BleibtAllesWieEsWar()
    {
        var shop  = Open(19);
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyStock(sword), Is.EqualTo(TradeResult.NotEnoughGold));
            Assert.That(shop.Gold.Amount, Is.EqualTo(19));
            Assert.That(shop.Items.Inventory.Count, Is.Zero);
            Assert.That(shop.Vendor.Stock.Contains(sword), Is.True);
        });
    }

    [Test]
    public void BuyStock_OhnePlatz_BleibtAllesWieEsWar()
    {
        var shop  = Open(inventoryWidth: 1, inventoryHeight: 2);
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyStock(sword), Is.EqualTo(TradeResult.NoRoom));
            Assert.That(shop.Gold.Amount, Is.EqualTo(100));
            Assert.That(shop.Vendor.Stock.Contains(sword), Is.True);
        });
    }

    [Test]
    public void BuyStock_EinSeltenesItemKostetMehr()
    {
        var shop = Open(200);
        var rare = Stock(shop, PriceRuleTests.CreateRare(TestItems.Sword));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.GetBuyPrice(rare), Is.EqualTo(160));
            Assert.That(shop.Trade.BuyStock(rare), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(40));
        });
    }

    [Test]
    public void BuyStock_WasNichtImBestandLiegt_IstNichtZuHaben()
    {
        var shop = Open();

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyStock(TestItems.Create(TestItems.Sword)), Is.EqualTo(TradeResult.NotForSale));
            Assert.That(shop.Trade.BuyStock(null), Is.EqualTo(TradeResult.NotForSale));
            Assert.That(shop.Gold.Amount, Is.EqualTo(100));
        });
    }

    [Test]
    public void BuyStock_ZweimalDasselbeItem_GehtNurEinmal()
    {
        var shop  = Open();
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        shop.Trade.BuyStock(sword);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyStock(sword), Is.EqualTo(TradeResult.NotForSale));
            Assert.That(shop.Gold.Amount, Is.EqualTo(80));
            Assert.That(shop.Items.Inventory.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuyWare_KauftEinStueckUndDieWareBleibt()
    {
        var shop = Open();
        var ware = OfferPotions(shop);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(90));
            Assert.That(shop.Vendor.Wares.Contains(ware), Is.True);
            Assert.That(ware.StackSize, Is.EqualTo(1));
            Assert.That(shop.Items.Inventory.Count, Is.EqualTo(1));
            Assert.That(shop.Items.Inventory.Contains(ware), Is.False);
        });
    }

    [Test]
    public void BuyWare_FuelltVorhandeneStapel()
    {
        var shop  = Open();
        var ware  = OfferPotions(shop);
        var owned = TestItems.Create(TestItems.Potion, 2);

        shop.Items.PickUp(owned);

        shop.Trade.BuyWare(ware);

        Assert.Multiple(() =>
        {
            Assert.That(owned.StackSize, Is.EqualTo(3));
            Assert.That(shop.Items.Inventory.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuyWare_MehrereStueck_KostetJedesEinzeln()
    {
        var shop = Open();
        var ware = OfferPotions(shop);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware, 5), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(50));
            Assert.That(shop.Items.Inventory.GetItemsInReadingOrder().Sum(item => item.StackSize), Is.EqualTo(5));
            Assert.That(shop.Items.Inventory.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuyWare_MehrereStueck_HoertAufWennDasGoldAusgeht()
    {
        var shop = Open(25);
        var ware = OfferPotions(shop);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware, 5), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(5));
            Assert.That(shop.Items.Inventory.GetItemsInReadingOrder().Sum(item => item.StackSize), Is.EqualTo(2));
        });
    }

    [Test]
    public void BuyWare_MehrereStueck_HoertAufWennDerPlatzAusgeht()
    {
        var shop = Open(inventoryWidth: 1, inventoryHeight: 1);
        var ware = OfferPotions(shop);

        shop.Items.PickUp(TestItems.Create(TestItems.Potion, 3));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware, 5), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.EqualTo(80));
            Assert.That(shop.Items.Inventory.GetItemsInReadingOrder().Sum(item => item.StackSize), Is.EqualTo(5));
        });
    }

    [Test]
    public void BuyWare_OhneGold_NenntDenGrund()
    {
        var shop = Open(9);
        var ware = OfferPotions(shop);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware, 5), Is.EqualTo(TradeResult.NotEnoughGold));
            Assert.That(shop.Gold.Amount, Is.EqualTo(9));
            Assert.That(shop.Items.Inventory.Count, Is.Zero);
        });
    }

    [Test]
    public void BuyWare_OhnePlatz_NenntDenGrund()
    {
        var shop = Open(inventoryWidth: 1, inventoryHeight: 1);
        var ware = OfferPotions(shop);

        shop.Items.PickUp(TestItems.Create(TestItems.Pebble));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(ware), Is.EqualTo(TradeResult.NoRoom));
            Assert.That(shop.Gold.Amount, Is.EqualTo(100));
        });
    }

    [Test]
    public void BuyWare_WasKeineWareIst_IstNichtZuHaben()
    {
        var shop = Open();

        OfferPotions(shop);

        Assert.That(shop.Trade.BuyWare(TestItems.Create(TestItems.Potion)), Is.EqualTo(TradeResult.NotForSale));
    }

    [Test]
    public void BuyWare_OhnePreis_IstNichtZuHaben()
    {
        var shop   = Open();
        var pebble = TestItems.Create(TestItems.Pebble);

        shop.Vendor.SetWares([pebble]);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyWare(pebble), Is.EqualTo(TradeResult.NotForSale));
            Assert.That(shop.Items.Inventory.Count, Is.Zero);
        });
    }

    [Test]
    public void Sell_BringtEinViertelUndDasItemLiegtImRueckkauf()
    {
        var shop  = Open(0);
        var sword = TestItems.Create(TestItems.Sword);

        shop.Items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.Sell(sword), Is.True);
            Assert.That(shop.Gold.Amount, Is.EqualTo(5));
            Assert.That(shop.Items.Inventory.Contains(sword), Is.False);
            Assert.That(shop.Vendor.Buyback.Contains(sword), Is.True);
        });
    }

    [Test]
    public void Sell_VerkauftAuchDasItemInDerHand()
    {
        var shop  = Open(0);
        var sword = TestItems.Create(TestItems.Sword);

        shop.Items.PickUp(sword);
        shop.Items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.Sell(sword), Is.True);
            Assert.That(shop.Items.HeldItem, Is.Null);
            Assert.That(shop.Gold.Amount, Is.EqualTo(5));
        });
    }

    [Test]
    public void Sell_EinStapel_GehtAlsGanzes()
    {
        var shop    = Open(0);
        var potions = TestItems.Create(TestItems.Potion, 4);

        shop.Items.PickUp(potions);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.Sell(potions), Is.True);
            Assert.That(shop.Gold.Amount, Is.EqualTo(8));
            Assert.That(shop.Vendor.Buyback.Contains(potions), Is.True);
            Assert.That(potions.StackSize, Is.EqualTo(4));
        });
    }

    [Test]
    public void Sell_WasDerHeldNichtHat_GehtNicht()
    {
        var shop     = Open(0);
        var stashed  = TestItems.Create(TestItems.Sword);
        var equipped = TestItems.Create(TestItems.Shield);

        shop.Items.Stash.TryAdd(stashed);
        shop.Items.Equipment.Put(equipped);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.Sell(stashed), Is.False);
            Assert.That(shop.Trade.Sell(equipped), Is.False);
            Assert.That(shop.Trade.Sell(TestItems.Create(TestItems.Sword)), Is.False);
            Assert.That(shop.Trade.Sell(null), Is.False);
            Assert.That(shop.Gold.Amount, Is.Zero);
        });
    }

    [Test]
    public void Sell_OhnePreis_NimmtDerHaendlerNichts()
    {
        var shop   = Open(0);
        var pebble = TestItems.Create(TestItems.Pebble);

        shop.Items.PickUp(pebble);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.CanSell(pebble), Is.False);
            Assert.That(shop.Trade.Sell(pebble), Is.False);
            Assert.That(shop.Items.Inventory.Contains(pebble), Is.True);
        });
    }

    [Test]
    public void BuyBack_KostetWasDerHaendlerGezahltHat()
    {
        var shop = Open(0);
        var rare = PriceRuleTests.CreateRare(TestItems.Sword);

        shop.Items.PickUp(rare);
        shop.Trade.Sell(rare);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Gold.Amount, Is.EqualTo(40));
            Assert.That(shop.Trade.GetBuyPrice(rare), Is.EqualTo(40));
            Assert.That(shop.Trade.BuyBack(rare), Is.EqualTo(TradeResult.Done));
            Assert.That(shop.Gold.Amount, Is.Zero);
            Assert.That(shop.Items.Inventory.Contains(rare), Is.True);
            Assert.That(shop.Vendor.Buyback.Count, Is.Zero);
        });
    }

    [Test]
    public void BuyBack_OhneGenugGold_BleibtDasItemBeimHaendler()
    {
        var shop  = Open(0);
        var sword = TestItems.Create(TestItems.Sword);

        shop.Items.PickUp(sword);
        shop.Trade.Sell(sword);
        shop.Gold.TrySpend(3);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyBack(sword), Is.EqualTo(TradeResult.NotEnoughGold));
            Assert.That(shop.Vendor.Buyback.Contains(sword), Is.True);
            Assert.That(shop.Gold.Amount, Is.EqualTo(2));
        });
    }

    [Test]
    public void BuyBack_OhnePlatz_BleibtDasItemBeimHaendler()
    {
        var shop  = Open(0, 1, 3);
        var sword = TestItems.Create(TestItems.Sword);

        shop.Items.PickUp(sword);
        shop.Trade.Sell(sword);
        shop.Items.PickUp(TestItems.Create(TestItems.Bow));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyBack(sword), Is.EqualTo(TradeResult.NoRoom));
            Assert.That(shop.Vendor.Buyback.Contains(sword), Is.True);
            Assert.That(shop.Gold.Amount, Is.EqualTo(5));
        });
    }

    [Test]
    public void BuyBack_EinItemAusDemBestand_IstKeinRueckkauf()
    {
        var shop  = Open();
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        Assert.Multiple(() =>
        {
            Assert.That(shop.Trade.BuyBack(sword), Is.EqualTo(TradeResult.NotForSale));
            Assert.That(shop.Gold.Amount, Is.EqualTo(100));
        });
    }

    [Test]
    public void KaufUndVerkauf_BringenKeinenGewinn()
    {
        var shop  = Open();
        var sword = Stock(shop, TestItems.Create(TestItems.Sword));

        shop.Trade.BuyStock(sword);
        shop.Trade.Sell(sword);
        shop.Trade.BuyBack(sword);
        shop.Trade.Sell(sword);

        Assert.That(shop.Gold.Amount, Is.EqualTo(85));
    }

    [Test]
    public void Vendor_EinVollerRueckkauf_VergisstDasAelteste()
    {
        var vendor = new Vendor(2, 2);
        var first  = TestItems.Create(TestItems.Helmet);
        var second = TestItems.Create(TestItems.Shield);

        vendor.AddToBuyback(first);
        vendor.AddToBuyback(second);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Buyback.Contains(first), Is.False);
            Assert.That(vendor.Buyback.Contains(second), Is.True);
        });
    }

    [Test]
    public void Vendor_EinZuGrossesItem_LeertDenRueckkaufNicht()
    {
        var vendor = new Vendor(2, 2);
        var helmet = TestItems.Create(TestItems.Helmet);
        var staff  = TestItems.Create(TestItems.Staff);

        vendor.AddToBuyback(helmet);
        vendor.AddToBuyback(staff);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Buyback.Contains(staff), Is.False);
            Assert.That(vendor.Buyback.Contains(helmet), Is.True);
        });
    }

    [Test]
    public void Vendor_ClearBuyback_LeertNurDenRueckkauf()
    {
        var shop  = Open(0);
        var sword = TestItems.Create(TestItems.Sword);
        var stock = Stock(shop, TestItems.Create(TestItems.Helmet));

        OfferPotions(shop);

        shop.Items.PickUp(sword);
        shop.Trade.Sell(sword);

        shop.Vendor.ClearBuyback();

        Assert.Multiple(() =>
        {
            Assert.That(shop.Vendor.Buyback.Count, Is.Zero);
            Assert.That(shop.Vendor.Stock.Contains(stock), Is.True);
            Assert.That(shop.Vendor.Wares.Count, Is.EqualTo(1));
            Assert.That(shop.Trade.BuyBack(sword), Is.EqualTo(TradeResult.NotForSale));
        });
    }

    [Test]
    public void Vendor_Restock_ErsetztDenBestandUndMerktDasLevel()
    {
        var vendor  = new Vendor(6, 4);
        var old     = TestItems.Create(TestItems.Sword);
        var fresh   = TestItems.Create(TestItems.Helmet);
        var changes = 0;

        vendor.Restock([old], 3);

        vendor.Changed += () => changes++;

        vendor.Restock([fresh], 7);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.Contains(old), Is.False);
            Assert.That(vendor.Stock.Contains(fresh), Is.True);
            Assert.That(vendor.ItemLevel, Is.EqualTo(7));
            Assert.That(vendor.IsStocked, Is.True);
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void Vendor_IstAmAnfangNichtBestueckt()
    {
        var vendor = new Vendor();

        Assert.Multiple(() =>
        {
            Assert.That(vendor.IsStocked, Is.False);
            Assert.That(vendor.ItemLevel, Is.EqualTo(1));
            Assert.That(vendor.Stock.Width, Is.EqualTo(14));
            Assert.That(vendor.Stock.Height, Is.EqualTo(10));
        });
    }

    [Test]
    public void Vendor_RestoreStock_LegtJedesItemAnSeinenPlatz()
    {
        var vendor = new Vendor(6, 4);
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);

        vendor.RestoreStock([(sword, new GridCell(4, 0)), (helmet, new GridCell(4, 0))], 5);

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Stock.GetPositionOf(sword), Is.EqualTo(new GridCell(4, 0)));
            Assert.That(vendor.Stock.GetPositionOf(helmet), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(vendor.ItemLevel, Is.EqualTo(5));
        });
    }
}
