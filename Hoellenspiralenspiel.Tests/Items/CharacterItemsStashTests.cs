using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class CharacterItemsStashTests
{
    private static CharacterItems CreateItems(int stashWidth = 4, int stashHeight = 4)
        => new(6, 4, _ => 10, stashWidth, stashHeight);

    [Test]
    public void DieTruheHatOhneAngabeVierzehnMalZehnFelder()
    {
        var items = TestItems.CreateCharacterItems();

        Assert.Multiple(() =>
        {
            Assert.That(items.Stash.Width, Is.EqualTo(14));
            Assert.That(items.Stash.Height, Is.EqualTo(10));
        });
    }

    [Test]
    public void TakeFrom_NimmtEinItemAusDerTruheInDieHand()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.Stash.TryAdd(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.TakeFrom(items.Stash, sword), Is.True);
            Assert.That(items.HeldItem, Is.SameAs(sword));
            Assert.That(items.Stash.Contains(sword), Is.False);
        });
    }

    [Test]
    public void TakeFrom_EinFremdesGitter_WirdAbgelehnt()
    {
        var items   = CreateItems();
        var foreign = new InventoryGrid(4, 4);
        var sword   = TestItems.Create(TestItems.Sword);

        foreign.TryAdd(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.TakeFrom(foreign, sword), Is.False);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(foreign.Contains(sword), Is.True);
        });
    }

    [Test]
    public void PlaceHeldAt_LegtDasItemInDieTruhe()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(items.Stash, new GridCell(2, 1)), Is.True);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(items.Stash.GetPositionOf(sword), Is.EqualTo(new GridCell(2, 1)));
            Assert.That(items.Inventory.Contains(sword), Is.False);
        });
    }

    [Test]
    public void PlaceHeldAt_InDerTruhe_TauschtMitDemItemDarunter()
    {
        var items  = CreateItems();
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);

        items.Stash.TryPlace(helmet, new GridCell(0, 0));
        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(items.Stash, new GridCell(0, 0)), Is.True);
            Assert.That(items.HeldItem, Is.SameAs(helmet));
            Assert.That(items.Stash.Contains(sword), Is.True);
            Assert.That(items.Stash.Contains(helmet), Is.False);
        });
    }

    [Test]
    public void PlaceHeldAt_InDerTruhe_StapeltTraenke()
    {
        var items   = CreateItems();
        var stashed = TestItems.Create(TestItems.Potion, 3);
        var held    = TestItems.Create(TestItems.Potion, 4);

        items.Stash.TryPlace(stashed, new GridCell(1, 1));
        items.PickUp(held);
        items.TakeFromInventory(held);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(items.Stash, new GridCell(1, 1)), Is.True);
            Assert.That(stashed.StackSize, Is.EqualTo(5));
            Assert.That(items.HeldItem, Is.SameAs(held));
            Assert.That(held.StackSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlaceHeldAt_EinFremdesGitter_WirdAbgelehnt()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(new InventoryGrid(4, 4), new GridCell(0, 0)), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(sword));
        });
    }

    [Test]
    public void Transfer_LagertEinItemEin()
    {
        var items   = CreateItems();
        var sword   = TestItems.Create(TestItems.Sword);
        var changes = 0;

        items.PickUp(sword);

        items.Changed += () => changes++;

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(sword, items.Inventory, items.Stash), Is.True);
            Assert.That(items.Inventory.Contains(sword), Is.False);
            Assert.That(items.Stash.GetPositionOf(sword), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void Transfer_HoltEinItemZurueck()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.Stash.TryAdd(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(sword, items.Stash, items.Inventory), Is.True);
            Assert.That(items.Stash.Contains(sword), Is.False);
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void Transfer_OhnePlatz_BleibtDasItemWoEsIst()
    {
        var items   = CreateItems(1, 1);
        var sword   = TestItems.Create(TestItems.Sword);
        var changes = 0;

        items.PickUp(sword);

        items.Changed += () => changes++;

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(sword, items.Inventory, items.Stash), Is.False);
            Assert.That(items.Inventory.Contains(sword), Is.True);
            Assert.That(items.Stash.Count, Is.Zero);
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void Transfer_Traenke_FuellenDenStapelDrueben()
    {
        var items   = CreateItems();
        var stashed = TestItems.Create(TestItems.Potion, 2);
        var carried = TestItems.Create(TestItems.Potion, 3);

        items.Stash.TryAdd(stashed);
        items.PickUp(carried);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(carried, items.Inventory, items.Stash), Is.True);
            Assert.That(stashed.StackSize, Is.EqualTo(5));
            Assert.That(items.Inventory.Count, Is.Zero);
            Assert.That(items.Stash.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void Transfer_Traenke_DerRestBekommtEinenEigenenPlatz()
    {
        var items   = CreateItems();
        var stashed = TestItems.Create(TestItems.Potion, 4);
        var carried = TestItems.Create(TestItems.Potion, 3);

        items.Stash.TryAdd(stashed);
        items.PickUp(carried);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(carried, items.Inventory, items.Stash), Is.True);
            Assert.That(stashed.StackSize, Is.EqualTo(5));
            Assert.That(carried.StackSize, Is.EqualTo(2));
            Assert.That(items.Stash.Contains(carried), Is.True);
            Assert.That(items.Inventory.Contains(carried), Is.False);
        });
    }

    [Test]
    public void Transfer_Traenke_OhneFreiesFeldBleibtDerRestZurueck()
    {
        var items   = CreateItems(1, 1);
        var stashed = TestItems.Create(TestItems.Potion, 4);
        var carried = TestItems.Create(TestItems.Potion, 3);

        items.Stash.TryAdd(stashed);
        items.PickUp(carried);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(carried, items.Inventory, items.Stash), Is.True);
            Assert.That(stashed.StackSize, Is.EqualTo(5));
            Assert.That(carried.StackSize, Is.EqualTo(2));
            Assert.That(items.Inventory.Contains(carried), Is.True);
            Assert.That(items.Stash.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void Transfer_EinItemDasNichtImGitterLiegt_WirdAbgelehnt()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(sword, items.Inventory, items.Stash), Is.False);
            Assert.That(items.Transfer(null, items.Inventory, items.Stash), Is.False);
            Assert.That(items.Stash.Count, Is.Zero);
        });
    }

    [Test]
    public void Transfer_InsSelbeOderEinFremdesGitter_WirdAbgelehnt()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Transfer(sword, items.Inventory, items.Inventory), Is.False);
            Assert.That(items.Transfer(sword, items.Inventory, new InventoryGrid(4, 4)), Is.False);
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void ReturnHeld_LegtDasItemZurueckInsInventar()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.Stash.TryAdd(sword);
        items.TakeFrom(items.Stash, sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.ReturnHeld(), Is.True);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void ReturnHeld_OhneItemInDerHand_GiltAlsErledigt()
    {
        var items   = CreateItems();
        var changes = 0;

        items.Changed += () => changes++;

        Assert.Multiple(() =>
        {
            Assert.That(items.ReturnHeld(), Is.True);
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void ReturnHeld_BeiVollemInventar_BleibtDasItemInDerHand()
    {
        var items = new CharacterItems(1, 3, _ => 10, 4, 4);
        var sword = TestItems.Create(TestItems.Sword);
        var staff = TestItems.Create(TestItems.Staff);

        items.PickUp(sword);
        items.Stash.TryAdd(staff);
        items.TakeFrom(items.Stash, staff);

        Assert.Multiple(() =>
        {
            Assert.That(items.ReturnHeld(), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(staff));
        });
    }

    [Test]
    public void StowHeld_LegtDasItemInDieTruhe()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.StowHeld(items.Stash), Is.True);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(items.Stash.Contains(sword), Is.True);
            Assert.That(items.Inventory.Contains(sword), Is.False);
        });
    }

    [Test]
    public void StowHeld_EinFremdesGitter_WirdAbgelehnt()
    {
        var items   = CreateItems();
        var foreign = new InventoryGrid(4, 4);
        var sword   = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.StowHeld(foreign), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(sword));
            Assert.That(foreign.Count, Is.Zero);
        });
    }

    [Test]
    public void StowHeld_Traenke_FuellenErstDieStapelUndDerRestBleibtInDerHand()
    {
        var items   = CreateItems(1, 1);
        var stashed = TestItems.Create(TestItems.Potion, 4);
        var held    = TestItems.Create(TestItems.Potion, 3);

        items.Stash.TryAdd(stashed);
        items.PickUp(held);
        items.TakeFromInventory(held);

        Assert.Multiple(() =>
        {
            Assert.That(items.StowHeld(items.Stash), Is.False);
            Assert.That(stashed.StackSize, Is.EqualTo(5));
            Assert.That(items.HeldItem, Is.SameAs(held));
            Assert.That(held.StackSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void Release_GibtEinItemAusDemInventarAb()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Release(sword), Is.True);
            Assert.That(items.Inventory.Contains(sword), Is.False);
            Assert.That(items.HeldItem, Is.Null);
        });
    }

    [Test]
    public void Release_GibtDasItemInDerHandAb()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Release(sword), Is.True);
            Assert.That(items.HeldItem, Is.Null);
        });
    }

    [Test]
    public void Release_EinItemAusDerTruhe_WirdAbgelehnt()
    {
        var items = CreateItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.Stash.TryAdd(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Release(sword), Is.False);
            Assert.That(items.Stash.Contains(sword), Is.True);
        });
    }

    [Test]
    public void HasRoomFor_KenntFreieFelderUndFreieStapel()
    {
        var items = new CharacterItems(1, 1, _ => 10, 4, 4);
        var stack = TestItems.Create(TestItems.Potion, 3);

        Assert.That(items.HasRoomFor(TestItems.Create(TestItems.Potion)), Is.True);

        items.PickUp(stack);

        Assert.Multiple(() =>
        {
            Assert.That(items.HasRoomFor(TestItems.Create(TestItems.Potion, 2)), Is.True);
            Assert.That(items.HasRoomFor(TestItems.Create(TestItems.Potion, 3)), Is.False);
            Assert.That(items.HasRoomFor(TestItems.Create(TestItems.Pebble)), Is.False);
            Assert.That(items.HasRoomFor(null), Is.False);
        });
    }

    [Test]
    public void Restore_LegtDieTruheWiederAn()
    {
        var items  = CreateItems();
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);

        items.Restore([], [], [], [(sword, new GridCell(3, 0)), (helmet, new GridCell(0, 2))]);

        Assert.Multiple(() =>
        {
            Assert.That(items.Stash.GetPositionOf(sword), Is.EqualTo(new GridCell(3, 0)));
            Assert.That(items.Stash.GetPositionOf(helmet), Is.EqualTo(new GridCell(0, 2)));
            Assert.That(items.Inventory.Count, Is.Zero);
        });
    }

    [Test]
    public void Restore_LeertDieTruheVorher()
    {
        var items = CreateItems();

        items.Stash.TryAdd(TestItems.Create(TestItems.Sword));

        items.Restore([], [], []);

        Assert.That(items.Stash.Count, Is.Zero);
    }

    [Test]
    public void Restore_WasAnSeinemPlatzNichtPasst_SuchtSichEinenAnderenInDerTruhe()
    {
        var items  = CreateItems();
        var first  = TestItems.Create(TestItems.Helmet);
        var second = TestItems.Create(TestItems.Helmet);

        items.Restore([], [], [], [(first, new GridCell(0, 0)), (second, new GridCell(1, 1))]);

        Assert.Multiple(() =>
        {
            Assert.That(items.Stash.Contains(first), Is.True);
            Assert.That(items.Stash.Contains(second), Is.True);
            Assert.That(items.Stash.GetPositionOf(second), Is.EqualTo(new GridCell(2, 0)));
        });
    }

    [Test]
    public void Restore_EineZuKleineTruhe_GibtIhreItemsInsInventar()
    {
        var items   = CreateItems(2, 2);
        var first   = TestItems.Create(TestItems.Helmet);
        var second  = TestItems.Create(TestItems.Helmet);
        var dropped = new List<ItemInstance>();

        items.Dropped += dropped.Add;

        items.Restore([], [], [], [(first, new GridCell(0, 0)), (second, new GridCell(4, 4))]);

        Assert.Multiple(() =>
        {
            Assert.That(items.Stash.Contains(first), Is.True);
            Assert.That(items.Inventory.Contains(second), Is.True);
            Assert.That(dropped, Is.Empty);
        });
    }

    [Test]
    public void Restore_OhnePlatzInTruheUndInventar_FaelltDasItemZuBoden()
    {
        var items   = new CharacterItems(1, 1, _ => 10, 1, 1);
        var helmet  = TestItems.Create(TestItems.Helmet);
        var dropped = new List<ItemInstance>();

        items.Dropped += dropped.Add;

        items.Restore([], [], [], [(helmet, new GridCell(0, 0))]);

        Assert.That(dropped, Is.EqualTo(new[] { helmet }));
    }
}
