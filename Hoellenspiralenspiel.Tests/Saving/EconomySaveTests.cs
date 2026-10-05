using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Tests.Economy;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Saving;

[TestFixture]
public class EconomySaveTests
{
    private const string Version2Json = "{\"Version\":2,\"Character\":{\"Name\":\"Old\",\"Level\":4},\"Inventory\":[{\"X\":1,\"Y\":0,\"Item\":{\"BaseId\":\"sword\"}}],\"Journey\":{\"UnlockedCircles\":1}}";

    private static SaveGame Reload(SaveGame save)
    {
        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var loaded), Is.True);

        return loaded;
    }

    //Seit M8, Etappe 3b: Version 6 nennt die Orte eines Abstiegs als Text, etwa "f2".
    //Seit den Affixen der Schwerter: Version 7 kennt das Y eines Affixes "Adds X to Y" und neue Stats
    [Test]
    public void DerSpielstandHatVersion7()
        => Assert.That(SaveGame.CurrentVersion, Is.EqualTo(7));

    [Test]
    public void Gold_UeberstehtDasSpeichern()
    {
        var loaded = Reload(new SaveGame { Character = new CharacterSave { Gold = 1234, StashGold = 56789 } });

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Character.Gold, Is.EqualTo(1234));
            Assert.That(loaded.Character.StashGold, Is.EqualTo(56789));
        });
    }

    [Test]
    public void DieTruhe_UeberstehtDasSpeichern()
    {
        var items = TestItems.CreateCharacterItems();
        var rare  = PriceRuleTests.CreateRare(TestItems.Sword);
        var save  = new SaveGame();

        items.Stash.TryPlace(rare, new GridCell(9, 4));
        items.Stash.TryPlace(TestItems.Create(TestItems.Potion, 4), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Helmet), new GridCell(2, 1));

        SaveGameMapper.CaptureItems(items, save);

        var restored = TestItems.CreateCharacterItems();
        var missing  = SaveGameMapper.RestoreItems(Reload(save), restored, TestItems.Catalog);
        var sword    = restored.Stash.GetItemAt(new GridCell(9, 4));

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.Empty);
            Assert.That(restored.Stash.Count, Is.EqualTo(2));
            Assert.That(sword.Definition, Is.SameAs(TestItems.Sword));
            Assert.That(sword.Rarity, Is.EqualTo(ItemRarity.Rare));
            Assert.That(sword.RareName, Is.EqualTo("Grim Edge"));
            Assert.That(sword.Affixes, Has.Count.EqualTo(3));
            Assert.That(restored.Stash.GetItemAt(new GridCell(0, 0)).StackSize, Is.EqualTo(4));
            Assert.That(restored.Inventory.Count, Is.EqualTo(1));
            Assert.That(restored.Inventory.GetItemAt(new GridCell(2, 1)).Definition, Is.SameAs(TestItems.Helmet));
        });
    }

    [Test]
    public void TruheUndInventar_BleibenGetrennt()
    {
        var items = TestItems.CreateCharacterItems();
        var save  = new SaveGame();

        items.Stash.TryPlace(TestItems.Create(TestItems.Sword), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Shield), new GridCell(0, 0));

        SaveGameMapper.CaptureItems(items, save);

        Assert.Multiple(() =>
        {
            Assert.That(save.Stash.Select(placed => placed.Item.BaseId), Is.EqualTo(new[] { "sword" }));
            Assert.That(save.Inventory.Select(placed => placed.Item.BaseId), Is.EqualTo(new[] { "shield" }));
        });
    }

    [Test]
    public void EineUnbekannteBasisInDerTruhe_WirdGemeldet()
    {
        var save = new SaveGame
        {
            Stash =
            [
                new PlacedItemSave { Item = new ItemSave { BaseId = "gone" } },
                new PlacedItemSave { X = 3, Item = new ItemSave { BaseId = "helmet" } }
            ]
        };

        var restored = TestItems.CreateCharacterItems();
        var missing  = SaveGameMapper.RestoreItems(save, restored, TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.EqualTo(new[] { "gone" }));
            Assert.That(restored.Stash.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void EinSpielstandDerVersion2_LaedtOhneGoldTruheUndHaendler()
    {
        var restored = TestItems.CreateCharacterItems();
        var vendor   = new Vendor();

        Assert.That(SaveGameSerializer.TryDeserialize(Version2Json, out var loaded), Is.True);

        SaveGameMapper.RestoreItems(loaded, restored, TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Character.Gold, Is.Zero);
            Assert.That(loaded.Character.StashGold, Is.Zero);
            Assert.That(loaded.Vendor, Is.Null);
            Assert.That(restored.Stash.Count, Is.Zero);
            Assert.That(restored.Inventory.Count, Is.EqualTo(1));
            Assert.That(SaveGameMapper.RestoreVendor(loaded, vendor, TestItems.Catalog), Is.False);
            Assert.That(vendor.IsStocked, Is.False);
        });
    }

    [Test]
    public void EinSpielstandOhneTruhe_LaedtMitLeererTruhe()
    {
        var restored = TestItems.CreateCharacterItems();

        restored.Stash.TryAdd(TestItems.Create(TestItems.Sword));

        SaveGameMapper.RestoreItems(new SaveGame { Stash = null }, restored, TestItems.Catalog);

        Assert.That(restored.Stash.Count, Is.Zero);
    }

    [Test]
    public void UnbekannteFelder_WerdenUeberlesen()
    {
        const string json = "{\"Version\":3,\"Character\":{\"Gold\":7,\"Karma\":99},\"Bank\":{\"Vault\":[1,2,3]}}";

        Assert.Multiple(() =>
        {
            Assert.That(SaveGameSerializer.TryDeserialize(json, out var loaded), Is.True);
            Assert.That(loaded.Character.Gold, Is.EqualTo(7));
        });
    }

    [Test]
    public void DerBestandDesHaendlers_UeberstehtDasSpeichern()
    {
        var vendor = new Vendor();
        var magic  = PriceRuleTests.CreateMagic(TestItems.Helmet);
        var save   = new SaveGame();

        vendor.RestoreStock([(TestItems.Create(TestItems.Sword, itemLevel: 6), new GridCell(5, 2)), (magic, new GridCell(0, 0))], 6);

        SaveGameMapper.CaptureVendor(vendor, save);

        var restored = new Vendor();
        var wasFound = SaveGameMapper.RestoreVendor(Reload(save), restored, TestItems.Catalog);
        var helmet   = restored.Stock.GetItemAt(new GridCell(0, 0));
        var sword    = restored.Stock.GetItemAt(new GridCell(5, 2));

        Assert.Multiple(() =>
        {
            Assert.That(wasFound, Is.True);
            Assert.That(restored.IsStocked, Is.True);
            Assert.That(restored.ItemLevel, Is.EqualTo(6));
            Assert.That(restored.Stock.Count, Is.EqualTo(2));
            Assert.That(helmet.Rarity, Is.EqualTo(ItemRarity.Magic));
            Assert.That(sword.Definition, Is.SameAs(TestItems.Sword));
            Assert.That(sword.ItemLevel, Is.EqualTo(6));
        });
    }

    [Test]
    public void EinLeergekaufterBestand_BleibtLeer()
    {
        var vendor = new Vendor();
        var save   = new SaveGame();

        vendor.Restock([], 4);

        SaveGameMapper.CaptureVendor(vendor, save);

        var restored = new Vendor();

        Assert.Multiple(() =>
        {
            Assert.That(SaveGameMapper.RestoreVendor(Reload(save), restored, TestItems.Catalog), Is.True);
            Assert.That(restored.IsStocked, Is.True);
            Assert.That(restored.Stock.Count, Is.Zero);
            Assert.That(restored.ItemLevel, Is.EqualTo(4));
        });
    }

    [Test]
    public void EinHaendlerOhneBestand_StehtNichtImSpielstand()
    {
        var save = new SaveGame { Vendor = new VendorSave() };

        SaveGameMapper.CaptureVendor(new Vendor(), save);

        Assert.That(save.Vendor, Is.Null);
    }

    [Test]
    public void RueckkaufUndWaren_StehenNichtImSpielstand()
    {
        var vendor = new Vendor();
        var save   = new SaveGame();

        vendor.Restock([], 1);
        vendor.SetWares([TestItems.Create(TestItems.Potion)]);
        vendor.AddToBuyback(TestItems.Create(TestItems.Sword));

        SaveGameMapper.CaptureVendor(vendor, save);

        Assert.That(save.Vendor.Stock, Is.Empty);
    }

    [Test]
    public void EineUnbekannteBasisImBestand_FehltNachDemLaden()
    {
        var save = new SaveGame
        {
            Vendor = new VendorSave
            {
                ItemLevel = 2,
                Stock =
                [
                    new PlacedItemSave { Item = new ItemSave { BaseId = "gone" } },
                    new PlacedItemSave { X = 2, Item = new ItemSave { BaseId = "shield" } }
                ]
            }
        };

        var vendor = new Vendor();

        Assert.Multiple(() =>
        {
            Assert.That(SaveGameMapper.RestoreVendor(save, vendor, TestItems.Catalog), Is.True);
            Assert.That(vendor.Stock.Count, Is.EqualTo(1));
            Assert.That(vendor.Stock.GetItemAt(new GridCell(2, 0)).Definition, Is.SameAs(TestItems.Shield));
        });
    }

    [Test]
    public void EinBestandAusVersion3_WirdNachItemtypNeuAusgelegt()
    {
        var save = new SaveGame
        {
            Version = 3,
            Vendor = new VendorSave
            {
                ItemLevel = 5,
                Stock =
                [
                    new PlacedItemSave { Item = new ItemSave { BaseId = "shield" } },
                    new PlacedItemSave { X = 6, Y = 2, Item = new ItemSave { BaseId = "sword" } }
                ]
            }
        };

        var vendor = new Vendor();

        Assert.Multiple(() =>
        {
            Assert.That(SaveGameMapper.RestoreVendor(save, vendor, TestItems.Catalog), Is.True);
            Assert.That(vendor.Stock.Count, Is.EqualTo(2));
            Assert.That(vendor.Stock.GetItemAt(new GridCell(0, 0)).Definition, Is.SameAs(TestItems.Sword));
            Assert.That(vendor.Stock.GetItemAt(new GridCell(6, 2)), Is.Null);
            Assert.That(vendor.ItemLevel, Is.EqualTo(5));
        });
    }
}
