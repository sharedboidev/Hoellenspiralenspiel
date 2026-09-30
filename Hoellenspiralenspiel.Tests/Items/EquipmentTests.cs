using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class EquipmentTests
{
    private static Equipment Wearing(params ItemDefinition[] definitions)
    {
        var equipment = new Equipment();

        foreach (var definition in definitions)
            equipment.Put(TestItems.Create(definition));

        return equipment;
    }

    [Test]
    public void GetWornCounterpart_EinSchwert_ZeigtDasGetrageneSchwert()
    {
        var equipment = Wearing(TestItems.Sword);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Sword)), Is.SameAs(equipment.MainHand));
    }

    [Test]
    public void GetWornCounterpart_EinStab_ZeigtDasSchwertInDerHaupthand()
    {
        var equipment = Wearing(TestItems.Sword);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Staff)), Is.SameAs(equipment.MainHand));
    }

    [Test]
    public void GetWornCounterpart_EinSchildBeiLeererNebenhand_IstNull()
    {
        var equipment = Wearing(TestItems.Sword);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Shield)), Is.Null);
    }

    [Test]
    public void GetWornCounterpart_EinTrank_IstNull()
    {
        var equipment = Wearing(TestItems.Sword, TestItems.Shield, TestItems.Helmet);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Potion)), Is.Null);
    }

    [Test]
    public void GetWornCounterpart_EinBogenBeiSchwertUndSchild_ZeigtNurDasSchwert()
    {
        var equipment = Wearing(TestItems.Sword, TestItems.Shield);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Bow)), Is.SameAs(equipment.MainHand));
    }

    [Test]
    public void GetWornCounterpart_EinSchildNebenEinemBogen_IstNull()
    {
        var equipment = Wearing(TestItems.Bow);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(TestItems.Shield)), Is.Null);
    }

    [Test]
    public void GetWornCounterpart_EinItemOhnePlatz_IstNull()
    {
        var unplaced  = ItemDefinition.ForArmor("rag", "Rag", ItemSlot.Undefined, 1);
        var equipment = Wearing(unplaced);

        Assert.That(equipment.GetWornCounterpart(TestItems.Create(unplaced)), Is.Null);
    }

    [Test]
    public void GetWornCounterpart_KeinItem_IstNull()
    {
        var equipment = Wearing(TestItems.Sword);

        Assert.That(equipment.GetWornCounterpart(null), Is.Null);
    }
}
