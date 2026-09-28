using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class CharacterItemsTests
{
    [Test]
    public void PickUp_LegtDasItemAufDenErstenFreienPlatz()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PickUp(sword), Is.True);
            Assert.That(items.Inventory.GetPositionOf(sword), Is.EqualTo(new GridCell(0, 0)));
        });
    }

    [Test]
    public void PickUp_Traenke_LandenAufDemVorhandenenStapel()
    {
        var items  = TestItems.CreateCharacterItems();
        var stack  = TestItems.Create(TestItems.Potion, 2);
        var looted = TestItems.Create(TestItems.Potion, 3);

        items.PickUp(stack);

        Assert.Multiple(() =>
        {
            Assert.That(items.PickUp(looted), Is.True);
            Assert.That(stack.StackSize, Is.EqualTo(5));
            Assert.That(items.Inventory.Count, Is.EqualTo(1));
            Assert.That(items.Inventory.Contains(looted), Is.False);
        });
    }

    [Test]
    public void PickUp_Traenke_FuellenAufUndDerRestBekommtEinenEigenenPlatz()
    {
        var items  = TestItems.CreateCharacterItems();
        var stack  = TestItems.Create(TestItems.Potion, 4);
        var looted = TestItems.Create(TestItems.Potion, 3);

        items.PickUp(stack);
        items.PickUp(looted);

        Assert.Multiple(() =>
        {
            Assert.That(stack.StackSize, Is.EqualTo(5));
            Assert.That(looted.StackSize, Is.EqualTo(2));
            Assert.That(items.Inventory.GetPositionOf(looted), Is.EqualTo(new GridCell(1, 0)));
        });
    }

    [Test]
    public void PickUp_Traenke_VerteilenSichAufMehrereStapel()
    {
        var items  = TestItems.CreateCharacterItems();
        var first  = TestItems.Create(TestItems.Potion, 4);
        var second = TestItems.Create(TestItems.Potion, 3);

        items.Inventory.TryPlace(first, new GridCell(0, 0));
        items.Inventory.TryPlace(second, new GridCell(3, 0));

        var wasTaken = items.PickUp(TestItems.Create(TestItems.Potion, 3));

        Assert.Multiple(() =>
        {
            Assert.That(wasTaken, Is.True);
            Assert.That(first.StackSize, Is.EqualTo(5));
            Assert.That(second.StackSize, Is.EqualTo(5));
            Assert.That(items.Inventory.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void PickUp_VollesInventar_LaesstDenRestLiegen()
    {
        var items  = TestItems.CreateCharacterItems(2, 1);
        var stack  = TestItems.Create(TestItems.Potion, 4);
        var looted = TestItems.Create(TestItems.Potion, 3);
        var events = 0;

        items.PickUp(stack);
        items.PickUp(TestItems.Create(TestItems.Pebble));

        items.Changed += () => events++;

        Assert.Multiple(() =>
        {
            Assert.That(items.PickUp(looted), Is.False);
            Assert.That(stack.StackSize, Is.EqualTo(5));
            Assert.That(looted.StackSize, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(1));
        });
    }

    [Test]
    public void PickUp_OhnePlatz_AendertNichts()
    {
        var items  = TestItems.CreateCharacterItems(2, 2);
        var events = 0;

        items.PickUp(TestItems.Create(TestItems.Helmet));

        items.Changed += () => events++;

        Assert.Multiple(() =>
        {
            Assert.That(items.PickUp(TestItems.Create(TestItems.Sword)), Is.False);
            Assert.That(events, Is.Zero);
        });
    }

    [Test]
    public void TakeFromInventory_NimmtDasItemInDieHand()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.TakeFromInventory(sword), Is.True);
            Assert.That(items.HeldItem, Is.SameAs(sword));
            Assert.That(items.Inventory.Contains(sword), Is.False);
        });
    }

    [Test]
    public void TakeFromInventory_MitVollerHand_SchlaegtFehl()
    {
        var items  = TestItems.CreateCharacterItems();
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);

        items.PickUp(sword);
        items.PickUp(helmet);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.TakeFromInventory(helmet), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(sword));
            Assert.That(items.Inventory.Contains(helmet), Is.True);
        });
    }

    [Test]
    public void PlaceHeldAt_LegtDasItemAufFreieFelder()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(new GridCell(4, 1)), Is.True);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(items.Inventory.GetPositionOf(sword), Is.EqualTo(new GridCell(4, 1)));
        });
    }

    [Test]
    public void PlaceHeldAt_AmRand_RuecktDasItemInsRaster()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);
        items.PlaceHeldAt(new GridCell(5, 3));

        Assert.That(items.Inventory.GetPositionOf(sword), Is.EqualTo(new GridCell(5, 1)));
    }

    [Test]
    public void PlaceHeldAt_AufEinItem_TauschtBeide()
    {
        var items  = TestItems.CreateCharacterItems();
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);

        items.Inventory.TryPlace(helmet, new GridCell(2, 0));
        items.Inventory.TryPlace(sword, new GridCell(0, 0));
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(new GridCell(3, 1)), Is.True);
            Assert.That(items.HeldItem, Is.SameAs(helmet));
            Assert.That(items.Inventory.GetPositionOf(sword), Is.EqualTo(new GridCell(3, 1)));
            Assert.That(items.Inventory.Contains(helmet), Is.False);
        });
    }

    [Test]
    public void PlaceHeldAt_AufZweiItems_SchlaegtFehl()
    {
        var items  = TestItems.CreateCharacterItems();
        var helmet = TestItems.Create(TestItems.Helmet);

        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion), new GridCell(1, 1));
        items.Inventory.TryPlace(helmet, new GridCell(3, 0));
        items.TakeFromInventory(helmet);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(new GridCell(0, 0)), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(helmet));
            Assert.That(items.Inventory.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlaceHeldAt_AufGleichenStapel_FuehrtZusammen()
    {
        var items = TestItems.CreateCharacterItems();
        var stack = TestItems.Create(TestItems.Potion, 2);
        var held  = TestItems.Create(TestItems.Potion, 2);

        items.Inventory.TryPlace(stack, new GridCell(0, 0));
        items.Inventory.TryPlace(held, new GridCell(3, 0));
        items.TakeFromInventory(held);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldAt(new GridCell(0, 0)), Is.True);
            Assert.That(stack.StackSize, Is.EqualTo(4));
            Assert.That(items.HeldItem, Is.Null);
        });
    }

    [Test]
    public void PlaceHeldAt_StapelWirdVoll_DerRestBleibtInDerHand()
    {
        var items = TestItems.CreateCharacterItems();
        var stack = TestItems.Create(TestItems.Potion, 4);
        var held  = TestItems.Create(TestItems.Potion, 3);

        items.Inventory.TryPlace(stack, new GridCell(0, 0));
        items.Inventory.TryPlace(held, new GridCell(3, 0));
        items.TakeFromInventory(held);
        items.PlaceHeldAt(new GridCell(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(stack.StackSize, Is.EqualTo(5));
            Assert.That(items.HeldItem, Is.SameAs(held));
            Assert.That(held.StackSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlaceHeldAt_AufVollenStapel_TauschtDieStapel()
    {
        var items = TestItems.CreateCharacterItems();
        var stack = TestItems.Create(TestItems.Potion, 5);
        var held  = TestItems.Create(TestItems.Potion, 3);

        items.Inventory.TryPlace(stack, new GridCell(0, 0));
        items.Inventory.TryPlace(held, new GridCell(3, 0));
        items.TakeFromInventory(held);
        items.PlaceHeldAt(new GridCell(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(items.HeldItem, Is.SameAs(stack));
            Assert.That(items.Inventory.GetPositionOf(held), Is.EqualTo(new GridCell(0, 0)));
        });
    }

    [Test]
    public void EquipFromInventory_LegtDasItemAn()
    {
        var items    = TestItems.CreateCharacterItems();
        var sword    = TestItems.Create(TestItems.Sword);
        var equipped = new List<ItemInstance>();

        items.Equipment.Equipped += equipped.Add;

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.EquipFromInventory(sword), Is.True);
            Assert.That(items.Equipment.MainHand, Is.SameAs(sword));
            Assert.That(items.Inventory.Contains(sword), Is.False);
            Assert.That(equipped, Is.EqualTo(new[] { sword }));
        });
    }

    [Test]
    public void EquipFromInventory_DasAlteItemNimmtDenFreienPlatzEin()
    {
        var items      = TestItems.CreateCharacterItems();
        var oldSword   = TestItems.Create(TestItems.Sword);
        var newSword   = TestItems.Create(TestItems.Sword);
        var unequipped = new List<ItemInstance>();

        items.Equipment.Unequipped += unequipped.Add;

        items.PickUp(oldSword);
        items.EquipFromInventory(oldSword);
        items.Inventory.TryPlace(newSword, new GridCell(4, 1));
        items.EquipFromInventory(newSword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Equipment.MainHand, Is.SameAs(newSword));
            Assert.That(items.Inventory.GetPositionOf(oldSword), Is.EqualTo(new GridCell(4, 1)));
            Assert.That(unequipped, Is.EqualTo(new[] { oldSword }));
        });
    }

    [Test]
    public void EquipFromInventory_DasAlteItemPasstNirgends_FaelltZuBoden()
    {
        var items   = TestItems.CreateCharacterItems(2, 3);
        var staff   = TestItems.Create(TestItems.Staff);
        var sword   = TestItems.Create(TestItems.Sword);
        var dropped = new List<ItemInstance>();

        items.Dropped += dropped.Add;

        items.Equipment.Put(staff);
        items.PickUp(sword);
        items.EquipFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Equipment.MainHand, Is.SameAs(sword));
            Assert.That(dropped, Is.EqualTo(new[] { staff }));
            Assert.That(items.Inventory.Count, Is.Zero);
        });
    }

    [Test]
    public void EquipFromInventory_OhneErfuellteAnforderung_SchlaegtFehl()
    {
        var items = TestItems.CreateCharacterItems(strength: 1);
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.EquipFromInventory(sword), Is.False);
            Assert.That(items.GetUnmetRequirements(sword), Is.EqualTo(new[] { Requirement.Strength }));
            Assert.That(items.Equipment.MainHand, Is.Null);
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void EquipFromInventory_Trank_SchlaegtFehl()
    {
        var items  = TestItems.CreateCharacterItems();
        var potion = TestItems.Create(TestItems.Potion);

        items.PickUp(potion);

        Assert.That(items.EquipFromInventory(potion), Is.False);
    }

    [Test]
    public void Zauberwaffe_TeiltSichDieHaupthandMitDerPhysischenWaffe()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);
        var staff = TestItems.Create(TestItems.Staff);

        items.PickUp(sword);
        items.PickUp(staff);
        items.EquipFromInventory(sword);
        items.EquipFromInventory(staff);

        Assert.Multiple(() =>
        {
            Assert.That(items.Equipment.MainHand, Is.SameAs(staff));
            Assert.That(items.Equipment.Get(ItemSlot.SpellWeapon), Is.SameAs(staff));
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void Zweihandwaffe_SchicktDenSchildInsInventar()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var bow    = TestItems.Create(TestItems.Bow);

        items.PickUp(shield);
        items.PickUp(bow);
        items.EquipFromInventory(shield);

        Assert.Multiple(() =>
        {
            Assert.That(items.EquipFromInventory(bow), Is.True);
            Assert.That(items.Equipment.MainHand, Is.SameAs(bow));
            Assert.That(items.Equipment.Offhand, Is.Null);
            Assert.That(items.Inventory.Contains(shield), Is.True);
        });
    }

    [Test]
    public void Zweihandwaffe_OhnePlatzFuerDenSchild_LaesstSichNichtAnlegen()
    {
        var items  = TestItems.CreateCharacterItems(1, 3);
        var shield = TestItems.Create(TestItems.Shield);
        var bow    = TestItems.Create(TestItems.Bow);

        items.Equipment.Put(shield);
        items.Inventory.TryPlace(bow, new GridCell(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(items.EquipFromInventory(bow), Is.False);
            Assert.That(items.Equipment.Offhand, Is.SameAs(shield));
            Assert.That(items.Equipment.MainHand, Is.Null);
            Assert.That(items.Inventory.GetPositionOf(bow), Is.EqualTo(new GridCell(0, 0)));
        });
    }

    [Test]
    public void Schild_LaesstSichNebenZweihandwaffeNichtAnlegen()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var staff  = TestItems.Create(TestItems.Staff);

        items.PickUp(shield);
        items.PickUp(staff);
        items.EquipFromInventory(staff);

        Assert.Multiple(() =>
        {
            Assert.That(items.CanEquip(shield), Is.False);
            Assert.That(items.EquipFromInventory(shield), Is.False);
            Assert.That(items.Equipment.Offhand, Is.Null);
        });
    }

    [Test]
    public void Schild_NebenEinhandwaffe_IstErlaubt()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var sword  = TestItems.Create(TestItems.Sword);

        items.PickUp(shield);
        items.PickUp(sword);
        items.EquipFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.EquipFromInventory(shield), Is.True);
            Assert.That(items.Equipment.Offhand, Is.SameAs(shield));
            Assert.That(items.Equipment.MainHand, Is.SameAs(sword));
        });
    }

    [Test]
    public void MitTalent_TraegtDerHeldSchildUndZweihandwaffe()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var bow    = TestItems.Create(TestItems.Bow);

        items.AllowsOffhandWithTwoHander = true;

        items.PickUp(shield);
        items.PickUp(bow);
        items.EquipFromInventory(bow);
        items.EquipFromInventory(shield);

        Assert.Multiple(() =>
        {
            Assert.That(items.Equipment.MainHand, Is.SameAs(bow));
            Assert.That(items.Equipment.Offhand, Is.SameAs(shield));
        });
    }

    [Test]
    public void TakeFromEquipment_NimmtDasItemInDieHand()
    {
        var items      = TestItems.CreateCharacterItems();
        var helmet     = TestItems.Create(TestItems.Helmet);
        var unequipped = new List<ItemInstance>();

        items.Equipment.Unequipped += unequipped.Add;

        items.PickUp(helmet);
        items.EquipFromInventory(helmet);

        Assert.Multiple(() =>
        {
            Assert.That(items.TakeFromEquipment(ItemSlot.Helmet), Is.True);
            Assert.That(items.HeldItem, Is.SameAs(helmet));
            Assert.That(items.Equipment.Get(ItemSlot.Helmet), Is.Null);
            Assert.That(unequipped, Is.EqualTo(new[] { helmet }));
            Assert.That(items.TakeFromEquipment(ItemSlot.Helmet), Is.False);
        });
    }

    [Test]
    public void PlaceHeldInEquipment_TauschtMitDemAngelegtenItem()
    {
        var items    = TestItems.CreateCharacterItems();
        var oldSword = TestItems.Create(TestItems.Sword);
        var newSword = TestItems.Create(TestItems.Sword);

        items.Equipment.Put(oldSword);
        items.PickUp(newSword);
        items.TakeFromInventory(newSword);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldInEquipment(ItemSlot.PhysicalWeapon), Is.True);
            Assert.That(items.Equipment.MainHand, Is.SameAs(newSword));
            Assert.That(items.HeldItem, Is.SameAs(oldSword));
        });
    }

    [Test]
    public void PlaceHeldInEquipment_FalscherPlatz_SchlaegtFehl()
    {
        var items  = TestItems.CreateCharacterItems();
        var helmet = TestItems.Create(TestItems.Helmet);

        items.PickUp(helmet);
        items.TakeFromInventory(helmet);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldInEquipment(ItemSlot.Torso), Is.False);
            Assert.That(items.PlaceHeldInEquipment(ItemSlot.PhysicalWeapon), Is.False);
            Assert.That(items.HeldItem, Is.SameAs(helmet));
        });
    }

    [Test]
    public void PlaceHeldInEquipment_OhneItemInDerHand_SchlaegtFehl()
        => Assert.That(TestItems.CreateCharacterItems().PlaceHeldInEquipment(ItemSlot.Helmet), Is.False);

    [Test]
    public void PlaceHeldInEquipment_Zweihandwaffe_SchicktDenSchildInsInventar()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var staff  = TestItems.Create(TestItems.Staff);

        items.Equipment.Put(shield);
        items.PickUp(staff);
        items.TakeFromInventory(staff);

        Assert.Multiple(() =>
        {
            Assert.That(items.PlaceHeldInEquipment(ItemSlot.PhysicalWeapon), Is.True);
            Assert.That(items.Equipment.MainHand, Is.SameAs(staff));
            Assert.That(items.Equipment.Offhand, Is.Null);
            Assert.That(items.Inventory.Contains(shield), Is.True);
            Assert.That(items.HeldItem, Is.Null);
        });
    }

    [Test]
    public void Consume_VerbrauchtEinenTrankDesStapels()
    {
        var items   = TestItems.CreateCharacterItems();
        var potions = TestItems.Create(TestItems.Potion, 2);

        items.PickUp(potions);

        var effect = items.Consume(potions);

        Assert.Multiple(() =>
        {
            Assert.That(effect, Is.EqualTo(new ConsumableEffect(ConsumableEffectKind.RestoreLife, 20)));
            Assert.That(potions.StackSize, Is.EqualTo(1));
            Assert.That(items.Inventory.Contains(potions), Is.True);
        });
    }

    [Test]
    public void Consume_DerLetzteTrank_GibtDenPlatzFrei()
    {
        var items   = TestItems.CreateCharacterItems();
        var potions = TestItems.Create(TestItems.Potion);

        items.PickUp(potions);
        items.Consume(potions);

        Assert.Multiple(() =>
        {
            Assert.That(items.Inventory.Contains(potions), Is.False);
            Assert.That(items.Inventory.GetItemAt(new GridCell(0, 0)), Is.Null);
            Assert.That(items.Consume(potions), Is.Null);
        });
    }

    [Test]
    public void Consume_EinSchwert_BewirktNichts()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.Consume(sword), Is.Null);
            Assert.That(items.Inventory.Contains(sword), Is.True);
        });
    }

    [Test]
    public void DropHeld_MeldetDasItemUndLeertDieHand()
    {
        var items   = TestItems.CreateCharacterItems();
        var sword   = TestItems.Create(TestItems.Sword);
        var dropped = new List<ItemInstance>();

        items.Dropped += dropped.Add;

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        Assert.Multiple(() =>
        {
            Assert.That(items.DropHeld(), Is.True);
            Assert.That(items.HeldItem, Is.Null);
            Assert.That(dropped, Is.EqualTo(new[] { sword }));
            Assert.That(items.DropHeld(), Is.False);
        });
    }

    [Test]
    public void JederVorgang_MeldetGenauEineAenderung()
    {
        var items  = TestItems.CreateCharacterItems();
        var shield = TestItems.Create(TestItems.Shield);
        var bow    = TestItems.Create(TestItems.Bow);
        var events = 0;

        items.Changed += () => events++;

        items.PickUp(shield);
        items.PickUp(bow);
        items.EquipFromInventory(shield);
        items.EquipFromInventory(bow);
        items.TakeFromEquipment(ItemSlot.PhysicalWeapon);
        items.PlaceHeldAt(new GridCell(5, 0));

        Assert.That(events, Is.EqualTo(6));
    }

    [Test]
    public void Restore_StelltInventarUndAusruestungHer()
    {
        var items  = TestItems.CreateCharacterItems();
        var sword  = TestItems.Create(TestItems.Sword);
        var helmet = TestItems.Create(TestItems.Helmet);
        var potion = TestItems.Create(TestItems.Potion, 3);

        items.PickUp(TestItems.Create(TestItems.Shield));
        items.Equipment.Put(TestItems.Create(TestItems.Staff));

        items.Restore([(helmet, new GridCell(3, 1))], [sword], [potion]);

        Assert.Multiple(() =>
        {
            Assert.That(items.Inventory.GetItemsInReadingOrder().ToArray(), Is.EqualTo(new[] { potion, helmet }));
            Assert.That(items.Inventory.GetPositionOf(helmet), Is.EqualTo(new GridCell(3, 1)));
            Assert.That(items.Equipment.Items.Values, Is.EqualTo(new[] { sword }));
            Assert.That(items.HeldItem, Is.Null);
        });
    }

    [Test]
    public void Restore_WasNichtPasst_FaelltZuBoden()
    {
        var items   = TestItems.CreateCharacterItems(2, 2);
        var helmet  = TestItems.Create(TestItems.Helmet);
        var sword   = TestItems.Create(TestItems.Sword);
        var dropped = new List<ItemInstance>();

        items.Dropped += dropped.Add;

        items.Restore([(helmet, new GridCell(0, 0)), (sword, new GridCell(0, 0))], [], []);

        Assert.Multiple(() =>
        {
            Assert.That(items.Inventory.Contains(helmet), Is.True);
            Assert.That(dropped, Is.EqualTo(new[] { sword }));
        });
    }
}
