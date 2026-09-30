using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Saving;

[TestFixture]
public class LoadoutSaveTests
{
    private const string Version3Json = "{\"Version\":3,\"Character\":{\"Name\":\"Old\"},\"Loadout\":[\"attack\",null,\"fireball\"]}";

    private static readonly ItemDefinition ManaPotion = CharacterItemsConsumableTests.ManaPotion;

    private static readonly TestCatalog Catalog = new(TestItems.Potion, ManaPotion, TestItems.Sword);

    private static SaveGame Reload(SaveGame save)
    {
        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var loaded), Is.True);

        return loaded;
    }

    [Test]
    public void TraenkeUndSkills_UeberstehenDasSpeichern()
    {
        var loadout = new SkillLoadout(5);
        var save    = new SaveGame();

        loadout.Assign(0, "attack");
        loadout.AssignConsumable(1, TestItems.Potion.Id);
        loadout.AssignConsumable(3, ManaPotion.Id);
        loadout.AssignConsumable(4, TestItems.Potion.Id);

        SaveGameMapper.CaptureLoadout(loadout, save);

        var restored = new SkillLoadout(5);

        restored.Assign(2, "frost_nova");
        restored.Assign(3, "fireball");

        SaveGameMapper.RestoreLoadout(Reload(save), restored, Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(restored.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(restored.GetConsumableId(0), Is.Null);
            Assert.That(restored.GetConsumableId(1), Is.EqualTo(TestItems.Potion.Id));
            Assert.That(restored.GetSkillId(1), Is.Null);
            Assert.That(restored.IsEmpty(2), Is.True);
            Assert.That(restored.GetConsumableId(3), Is.EqualTo(ManaPotion.Id));
            Assert.That(restored.GetSkillId(3), Is.Null);
            Assert.That(restored.GetConsumableId(4), Is.EqualTo(TestItems.Potion.Id));
        });
    }

    [Test]
    public void CaptureLoadout_SchreibtBeideListenParallel()
    {
        var loadout = new SkillLoadout(3);
        var save    = new SaveGame();

        loadout.Assign(0, "attack");
        loadout.AssignConsumable(2, TestItems.Potion.Id);

        SaveGameMapper.CaptureLoadout(loadout, save);

        Assert.Multiple(() =>
        {
            Assert.That(save.Loadout, Is.EqualTo(new[] { "attack", null, null }));
            Assert.That(save.LoadoutConsumables, Is.EqualTo(new[] { null, null, TestItems.Potion.Id }));
        });
    }

    [Test]
    public void EinSpielstandDerVersion3_BehaeltSeineSkills()
    {
        Assert.That(SaveGameSerializer.TryDeserialize(Version3Json, out var loaded), Is.True);

        var loadout = new SkillLoadout(4);

        loadout.AssignConsumable(3, TestItems.Potion.Id);

        SaveGameMapper.RestoreLoadout(loaded, loadout, Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Version, Is.EqualTo(3));
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.IsEmpty(1), Is.True);
            Assert.That(loadout.GetSkillId(2), Is.EqualTo("fireball"));
            Assert.That(loadout.IsEmpty(3), Is.True);
        });
    }

    [Test]
    public void FehlendeListeDerTraenke_BehaeltDieSkills()
    {
        var loadout = new SkillLoadout(2);

        SaveGameMapper.RestoreLoadout(new SaveGame { Loadout = ["attack", "fireball"], LoadoutConsumables = null }, loadout, Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.GetSkillId(1), Is.EqualTo("fireball"));
        });
    }

    [Test]
    public void UnbekannteOderFalscheItemBasis_FaelltWeg()
    {
        var loadout = new SkillLoadout(4);
        var save    = new SaveGame
        {
            Loadout            = [null, "attack", null, null],
            LoadoutConsumables = ["gone_potion", "gone_potion", TestItems.Sword.Id, " "]
        };

        loadout.AssignConsumable(3, TestItems.Potion.Id);

        SaveGameMapper.RestoreLoadout(save, loadout, Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.IsEmpty(0), Is.True);
            Assert.That(loadout.GetSkillId(1), Is.EqualTo("attack"));
            Assert.That(loadout.GetConsumableId(1), Is.Null);
            Assert.That(loadout.IsEmpty(2), Is.True);
            Assert.That(loadout.IsEmpty(3), Is.True);
        });
    }

    [Test]
    public void OhneKatalog_BleibenNurDieSkills()
    {
        var loadout = new SkillLoadout(2);

        SaveGameMapper.RestoreLoadout(new SaveGame { Loadout = ["attack", null], LoadoutConsumables = [null, TestItems.Potion.Id] }, loadout);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.IsEmpty(1), Is.True);
        });
    }

    [Test]
    public void LaengereOderKuerzereListen_PassenSichDerLeisteAn()
    {
        var loadout = new SkillLoadout(3);
        var save    = new SaveGame
        {
            Loadout            = ["attack"],
            LoadoutConsumables = [null, TestItems.Potion.Id, null, ManaPotion.Id, TestItems.Potion.Id]
        };

        loadout.Assign(2, "fireball");

        SaveGameMapper.RestoreLoadout(save, loadout, Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.GetConsumableId(1), Is.EqualTo(TestItems.Potion.Id));
            Assert.That(loadout.IsEmpty(2), Is.True);
        });
    }
}
