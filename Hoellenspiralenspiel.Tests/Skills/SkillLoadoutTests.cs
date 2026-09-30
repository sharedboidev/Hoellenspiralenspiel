using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class SkillLoadoutTests
{
    [Test]
    public void NeueLeiste_IstLeer()
    {
        var loadout = new SkillLoadout(10);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.SlotCount, Is.EqualTo(10));

            for (var slot = 0; slot < loadout.SlotCount; slot++)
                Assert.That(loadout.IsEmpty(slot), Is.True);
        });
    }

    [Test]
    public void Assign_LegtDenSkillAufDenPlatz()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(3, "fireball");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(3), Is.EqualTo("fireball"));
            Assert.That(loadout.IsEmpty(2), Is.True);
            Assert.That(loadout.FindSlotOf("fireball"), Is.EqualTo(3));
        });
    }

    [Test]
    public void Assign_ErsetztDenVorherigenSkill()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(0, "attack");
        loadout.Assign(0, "lightning_strike");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("lightning_strike"));
            Assert.That(loadout.FindSlotOf("attack"), Is.EqualTo(-1));
        });
    }

    [Test]
    public void DerselbeSkill_DarfAufMehrerenPlaetzenLiegen()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(0, "attack");
        loadout.Assign(1, "attack");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.EqualTo("attack"));
            Assert.That(loadout.GetSkillId(1), Is.EqualTo("attack"));
        });
    }

    [Test]
    public void Clear_LeertDenPlatz()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(5, "frost_nova");
        loadout.Clear(5);

        Assert.That(loadout.IsEmpty(5), Is.True);
    }

    [Test]
    public void SlotChanged_FeuertNurBeiEchterAenderung()
    {
        var loadout = new SkillLoadout(10);
        var changed = new List<int>();

        loadout.SlotChanged += changed.Add;

        loadout.Assign(2, "fireball");
        loadout.Assign(2, "fireball");
        loadout.Clear(2);
        loadout.Clear(2);

        Assert.That(changed, Is.EqualTo(new[] { 2, 2 }));
    }

    [Test]
    public void Assign_AufUnbekanntenPlatz_WirftAusnahme()
    {
        var loadout = new SkillLoadout(10);

        Assert.Multiple(() =>
        {
            Assert.That(() => loadout.Assign(10, "fireball"), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => loadout.Assign(-1, "fireball"), Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void GetSkillId_AufUnbekanntemPlatz_LiefertNichts()
    {
        var loadout = new SkillLoadout(10);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(10), Is.Null);
            Assert.That(loadout.GetSkillId(-1), Is.Null);
        });
    }

    [Test]
    public void LeereId_LeertDenPlatz()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(1, "fireball");
        loadout.Assign(1, " ");

        Assert.That(loadout.IsEmpty(1), Is.True);
    }

    [Test]
    public void LeisteOhnePlaetze_IstNichtErlaubt()
        => Assert.That(() => new SkillLoadout(0), Throws.InstanceOf<ArgumentOutOfRangeException>());

    [Test]
    public void AssignConsumable_LegtDenTrankAufDenPlatz()
    {
        var loadout = new SkillLoadout(10);

        loadout.AssignConsumable(6, "health_potion");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetConsumableId(6), Is.EqualTo("health_potion"));
            Assert.That(loadout.GetSkillId(6), Is.Null);
            Assert.That(loadout.IsEmpty(6), Is.False);
            Assert.That(loadout.GetConsumableId(5), Is.Null);
        });
    }

    [Test]
    public void AssignConsumable_ErsetztDenSkill()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(0, "attack");
        loadout.AssignConsumable(0, "health_potion");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(0), Is.Null);
            Assert.That(loadout.GetConsumableId(0), Is.EqualTo("health_potion"));
            Assert.That(loadout.FindSlotOf("attack"), Is.EqualTo(-1));
        });
    }

    [Test]
    public void Assign_ErsetztDenTrank()
    {
        var loadout = new SkillLoadout(10);

        loadout.AssignConsumable(1, "mana_potion");
        loadout.Assign(1, "fireball");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(1), Is.EqualTo("fireball"));
            Assert.That(loadout.GetConsumableId(1), Is.Null);
        });
    }

    [Test]
    public void Clear_LeertAuchEinenTrankplatz()
    {
        var loadout = new SkillLoadout(10);

        loadout.AssignConsumable(7, "health_potion");
        loadout.Assign(8, "fireball");
        loadout.AssignConsumable(8, "health_potion");
        loadout.Clear(7);
        loadout.Assign(8, null);

        Assert.Multiple(() =>
        {
            Assert.That(loadout.IsEmpty(7), Is.True);
            Assert.That(loadout.IsEmpty(8), Is.True);
            Assert.That(loadout.GetConsumableId(7), Is.Null);
            Assert.That(loadout.GetConsumableId(8), Is.Null);
        });
    }

    [Test]
    public void GleicheIdAlsSkillOderTrank_SindVerschiedeneBelegungen()
    {
        var loadout = new SkillLoadout(10);

        loadout.Assign(2, "health_potion");
        loadout.AssignConsumable(2, "health_potion");

        Assert.Multiple(() =>
        {
            Assert.That(loadout.GetSkillId(2), Is.Null);
            Assert.That(loadout.GetConsumableId(2), Is.EqualTo("health_potion"));
            Assert.That(loadout.FindSlotOf("health_potion"), Is.EqualTo(-1));
        });
    }

    [Test]
    public void SlotChanged_FeuertBeimTrankNurBeiEchterAenderung()
    {
        var loadout = new SkillLoadout(10);
        var changed = new List<int>();

        loadout.SlotChanged += changed.Add;

        loadout.AssignConsumable(4, "health_potion");
        loadout.AssignConsumable(4, "health_potion");
        loadout.AssignConsumable(4, "mana_potion");
        loadout.Assign(4, "mana_potion");
        loadout.AssignConsumable(4, " ");
        loadout.Clear(4);

        Assert.That(changed, Is.EqualTo(new[] { 4, 4, 4, 4 }));
    }

    [Test]
    public void AssignConsumable_AufUnbekanntenPlatz_WirftAusnahme()
    {
        var loadout = new SkillLoadout(10);

        Assert.Multiple(() =>
        {
            Assert.That(() => loadout.AssignConsumable(10, "health_potion"), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(loadout.GetConsumableId(10), Is.Null);
            Assert.That(loadout.GetConsumableId(-1), Is.Null);
        });
    }
}
