using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class CharacterItemsConsumableTests
{
    internal static readonly ItemDefinition ManaPotion = ItemDefinition.ForConsumable("mana_potion", "Mana Potion", new ConsumableEffect(ConsumableEffectKind.RestoreMana, 20), 5);

    [Test]
    public void CountInInventory_ZaehltAlleStapelDerArt()
    {
        var items = TestItems.CreateCharacterItems();

        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion, 5), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion, 2), new GridCell(3, 2));
        items.Inventory.TryPlace(TestItems.Create(ManaPotion, 4), new GridCell(1, 0));

        Assert.Multiple(() =>
        {
            Assert.That(items.CountInInventory(TestItems.Potion.Id), Is.EqualTo(7));
            Assert.That(items.CountInInventory(ManaPotion.Id), Is.EqualTo(4));
            Assert.That(items.CountInInventory("unknown"), Is.Zero);
            Assert.That(items.CountInInventory(null), Is.Zero);
        });
    }

    [Test]
    public void CountInInventory_TruheUndHandZaehlenNicht()
    {
        var items = TestItems.CreateCharacterItems();
        var held  = TestItems.Create(TestItems.Potion, 3);

        items.Inventory.TryPlace(held, new GridCell(0, 0));
        items.TakeFromInventory(held);
        items.Stash.TryPlace(TestItems.Create(TestItems.Potion, 4), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion, 1), new GridCell(2, 0));

        Assert.That(items.CountInInventory(TestItems.Potion.Id), Is.EqualTo(1));
    }

    [Test]
    public void FindStackToConsume_NimmtDenKleinstenStapel()
    {
        var items = TestItems.CreateCharacterItems();
        var full  = TestItems.Create(TestItems.Potion, 5);
        var small = TestItems.Create(TestItems.Potion, 2);

        items.Inventory.TryPlace(full, new GridCell(0, 0));
        items.Inventory.TryPlace(small, new GridCell(4, 3));

        Assert.That(items.FindStackToConsume(TestItems.Potion.Id), Is.SameAs(small));
    }

    [Test]
    public void FindStackToConsume_BeiGleichenStapelnGiltDieLesereihenfolge()
    {
        var items  = TestItems.CreateCharacterItems();
        var first  = TestItems.Create(TestItems.Potion, 3);
        var second = TestItems.Create(TestItems.Potion, 3);

        items.Inventory.TryPlace(second, new GridCell(0, 2));
        items.Inventory.TryPlace(first, new GridCell(5, 0));

        Assert.That(items.FindStackToConsume(TestItems.Potion.Id), Is.SameAs(first));
    }

    [Test]
    public void FindStackToConsume_SuchtNurDieseArtImInventar()
    {
        var items = TestItems.CreateCharacterItems();
        var held  = TestItems.Create(TestItems.Potion);

        items.Inventory.TryPlace(held, new GridCell(0, 0));
        items.TakeFromInventory(held);
        items.Stash.TryPlace(TestItems.Create(TestItems.Potion), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(ManaPotion), new GridCell(1, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Sword), new GridCell(2, 0));

        Assert.Multiple(() =>
        {
            Assert.That(items.FindStackToConsume(TestItems.Potion.Id), Is.Null);
            Assert.That(items.FindStackToConsume(TestItems.Sword.Id), Is.Null);
            Assert.That(items.FindStackToConsume(ManaPotion.Id)?.Definition, Is.SameAs(ManaPotion));
        });
    }

    [Test]
    public void DerLetzteTrank_GibtDenPlatzFreiUndDerNaechsteStapelFolgt()
    {
        var items = TestItems.CreateCharacterItems();
        var last  = TestItems.Create(TestItems.Potion);
        var full  = TestItems.Create(TestItems.Potion, 5);

        items.Inventory.TryPlace(full, new GridCell(0, 0));
        items.Inventory.TryPlace(last, new GridCell(3, 0));

        items.Consume(items.FindStackToConsume(TestItems.Potion.Id));

        Assert.Multiple(() =>
        {
            Assert.That(items.Inventory.GetItemAt(new GridCell(3, 0)), Is.Null);
            Assert.That(items.CountInInventory(TestItems.Potion.Id), Is.EqualTo(5));
            Assert.That(items.FindStackToConsume(TestItems.Potion.Id), Is.SameAs(full));
        });
    }
}
