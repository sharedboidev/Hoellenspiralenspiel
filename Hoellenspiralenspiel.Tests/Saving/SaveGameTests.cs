using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Saving;

[TestFixture]
public class SaveGameTests
{
    private static CharacterItems CreateEquippedCharacter()
    {
        var items  = TestItems.CreateCharacterItems();
        var sword  = TestItems.Create(TestItems.Sword, itemLevel: 12);
        var shield = TestItems.Create(TestItems.Shield);
        var helmet = TestItems.Create(TestItems.Helmet, itemLevel: 40);

        sword.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat, 4, "Rough", true));
        sword.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.Attackspeed, ModificationType.Percentage, 0.07f, "of Quickness", true));

        helmet.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.FireResistance, ModificationType.Flat, 11, "of the Whelp", false));
        helmet.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.Strength, ModificationType.Flat, 2, "of the Wrestler", false));

        helmet.RareName = "Grim Visor";

        items.Equipment.Put(sword);
        items.Equipment.Put(shield);

        items.Inventory.TryPlace(helmet, new GridCell(3, 1));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Potion, 3), new GridCell(0, 0));
        items.Inventory.TryPlace(TestItems.Create(TestItems.Staff), new GridCell(5, 0));

        return items;
    }

    private static SaveGame Capture(CharacterItems items)
    {
        var save = new SaveGame
        {
            Character = new CharacterSave
            {
                Level           = 7,
                XpTotal         = 12345,
                AttributePoints = 2,
                Strength        = 4,
                Dexterity       = 2,
                Intelligence    = 3,
                Constitution    = 1,
                Awareness       = 1
            }
        };

        SaveGameMapper.CaptureItems(items, save);

        return save;
    }

    [Test]
    public void GespeicherterCharakter_WirdIdentischGeladen()
    {
        var original = Capture(CreateEquippedCharacter());
        var json     = SaveGameSerializer.Serialize(original);

        var wasRead  = SaveGameSerializer.TryDeserialize(json, out var loaded);
        var restored = TestItems.CreateCharacterItems();
        var missing  = SaveGameMapper.RestoreItems(loaded, restored, TestItems.Catalog);

        var saveAfterLoading = Capture(restored);

        saveAfterLoading.Character = loaded.Character;

        Assert.Multiple(() =>
        {
            Assert.That(wasRead, Is.True);
            Assert.That(missing, Is.Empty);
            Assert.That(SaveGameSerializer.Serialize(saveAfterLoading), Is.EqualTo(json));
        });
    }

    [Test]
    public void GeladeneItems_HabenDieselbenWerte()
    {
        var json = SaveGameSerializer.Serialize(Capture(CreateEquippedCharacter()));

        SaveGameSerializer.TryDeserialize(json, out var loaded);

        var restored = TestItems.CreateCharacterItems();

        SaveGameMapper.RestoreItems(loaded, restored, TestItems.Catalog);

        var sword  = restored.Equipment.MainHand;
        var helmet = restored.Inventory.GetItemAt(new GridCell(4, 2));

        Assert.Multiple(() =>
        {
            Assert.That(sword.AffixedName, Is.EqualTo("Rough Sword of Quickness"));
            Assert.That(sword.ItemLevel, Is.EqualTo(12));
            Assert.That(sword.MinDamage, Is.EqualTo(8));
            Assert.That(sword.AttacksPerSecond, Is.EqualTo(1.5).Within(0.001));
            Assert.That(restored.Equipment.Offhand.Definition, Is.SameAs(TestItems.Shield));
            Assert.That(helmet.Rarity, Is.EqualTo(ItemRarity.Rare));
            Assert.That(helmet.RareName, Is.EqualTo("Grim Visor"));
            Assert.That(helmet.ItemLevel, Is.EqualTo(40));
            Assert.That(restored.Inventory.GetItemAt(new GridCell(0, 0)).StackSize, Is.EqualTo(3));
            Assert.That(restored.Inventory.Count, Is.EqualTo(3));
        });
    }

    [Test]
    public void GeladenesItem_LoestAusruestungsEreignisseAus()
    {
        var json = SaveGameSerializer.Serialize(Capture(CreateEquippedCharacter()));

        SaveGameSerializer.TryDeserialize(json, out var loaded);

        var restored = TestItems.CreateCharacterItems();
        var equipped = 0;

        restored.Equipment.Equipped += _ => equipped++;

        SaveGameMapper.RestoreItems(loaded, restored, TestItems.Catalog);

        Assert.That(equipped, Is.EqualTo(2));
    }

    [Test]
    public void Charakterwerte_UeberstehenDasSpeichern()
    {
        var json = SaveGameSerializer.Serialize(Capture(TestItems.CreateCharacterItems()));

        SaveGameSerializer.TryDeserialize(json, out var loaded);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Version, Is.EqualTo(SaveGame.CurrentVersion));
            Assert.That(loaded.Character.Level, Is.EqualTo(7));
            Assert.That(loaded.Character.XpTotal, Is.EqualTo(12345));
            Assert.That(loaded.Character.AttributePoints, Is.EqualTo(2));
            Assert.That(loaded.Character.Strength, Is.EqualTo(4));
            Assert.That(loaded.Character.Intelligence, Is.EqualTo(3));
        });
    }

    [Test]
    public void ItemInDerHand_LandetBeimLadenImInventar()
    {
        var items = TestItems.CreateCharacterItems();
        var sword = TestItems.Create(TestItems.Sword);

        items.PickUp(sword);
        items.TakeFromInventory(sword);

        var save     = Capture(items);
        var restored = TestItems.CreateCharacterItems();

        SaveGameMapper.RestoreItems(save, restored, TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(save.Unplaced, Has.Count.EqualTo(1));
            Assert.That(save.Inventory, Is.Empty);
            Assert.That(restored.HeldItem, Is.Null);
            Assert.That(restored.Inventory.GetItemsInReadingOrder().Single().Definition, Is.SameAs(TestItems.Sword));
        });
    }

    [Test]
    public void UnbekannteItemBasis_WirdGemeldetUndUebersprungen()
    {
        var save = Capture(CreateEquippedCharacter());

        save.Inventory[0].Item.BaseId = "removed_item";

        var restored = TestItems.CreateCharacterItems();
        var missing  = SaveGameMapper.RestoreItems(save, restored, TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.EqualTo(new[] { "removed_item" }));
            Assert.That(restored.Inventory.Count, Is.EqualTo(2));
            Assert.That(restored.Equipment.Items, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Enums_StehenAlsNamenInDerDatei()
    {
        var json = SaveGameSerializer.Serialize(Capture(CreateEquippedCharacter()));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"PhysicalDamage\""));
            Assert.That(json, Does.Contain("\"Percentage\""));
            Assert.That(json, Does.Contain("\"Offhand\""));
            Assert.That(json, Does.Contain("\"Prefix\""));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("kein json")]
    [TestCase("{ \"Version\": 1, ")]
    [TestCase("null")]
    [TestCase("{ \"Version\": 0 }")]
    [TestCase("{ \"Version\": 1, \"Character\": null }")]
    public void KaputteDatei_WirdAbgelehnt(string json)
    {
        Assert.Multiple(() =>
        {
            Assert.That(SaveGameSerializer.TryDeserialize(json, out var save), Is.False);
            Assert.That(save is null || save.Character is null || save.Version <= 0, Is.True);
        });
    }

    [Test]
    public void SpielstandEinerNeuerenVersion_WirdAbgelehnt()
    {
        var json = SaveGameSerializer.Serialize(new SaveGame { Version = SaveGame.CurrentVersion + 1 });

        Assert.That(SaveGameSerializer.TryDeserialize(json, out _), Is.False);
    }

    [Test]
    public void BelegungDerLeiste_UeberstehtDasSpeichern()
    {
        var loadout = new SkillLoadout(4);
        var save    = new SaveGame();

        loadout.Assign(0, "attack");
        loadout.Assign(3, "fireball");

        SaveGameMapper.CaptureLoadout(loadout, save);

        SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var loaded);

        var restored = new SkillLoadout(4);

        restored.Assign(1, "frost_nova");

        SaveGameMapper.RestoreLoadout(loaded, restored);

        Assert.Multiple(() =>
        {
            Assert.That(restored.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(restored.IsEmpty(1), Is.True);
            Assert.That(restored.IsEmpty(2), Is.True);
            Assert.That(restored.GetSkillId(3), Is.EqualTo("fireball"));
        });
    }

    [Test]
    public void KuerzereBelegung_LeertDieUebrigenPlaetze()
    {
        var loadout = new SkillLoadout(3);

        loadout.Assign(2, "fireball");

        SaveGameMapper.RestoreLoadout(new SaveGame { Loadout = ["attack"] }, loadout);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.IsEmpty(2), Is.True);
        });
    }
}
