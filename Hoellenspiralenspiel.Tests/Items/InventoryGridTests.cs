using System;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class InventoryGridTests
{
    [Test]
    public void TryPlace_BelegtAlleFelderDesItems()
    {
        var grid   = new InventoryGrid(6, 4);
        var helmet = TestItems.Create(TestItems.Helmet);

        var wasPlaced = grid.TryPlace(helmet, new GridCell(2, 1));

        Assert.Multiple(() =>
        {
            Assert.That(wasPlaced, Is.True);
            Assert.That(grid.GetPositionOf(helmet), Is.EqualTo(new GridCell(2, 1)));
            Assert.That(grid.GetItemAt(new GridCell(2, 1)), Is.SameAs(helmet));
            Assert.That(grid.GetItemAt(new GridCell(3, 2)), Is.SameAs(helmet));
            Assert.That(grid.GetItemAt(new GridCell(4, 1)), Is.Null);
            Assert.That(grid.GetItemAt(new GridCell(1, 1)), Is.Null);
        });
    }

    [TestCase(5, 0)]
    [TestCase(0, 3)]
    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    public void TryPlace_UeberDenRand_SchlaegtFehl(int x, int y)
    {
        var grid = new InventoryGrid(6, 4);

        Assert.That(grid.TryPlace(TestItems.Create(TestItems.Helmet), new GridCell(x, y)), Is.False);
    }

    [Test]
    public void TryPlace_AufBelegteFelder_SchlaegtFehl()
    {
        var grid = new InventoryGrid(6, 4);

        grid.TryPlace(TestItems.Create(TestItems.Helmet), new GridCell(0, 0));

        var sword = TestItems.Create(TestItems.Sword);

        Assert.Multiple(() =>
        {
            Assert.That(grid.TryPlace(sword, new GridCell(1, 1)), Is.False);
            Assert.That(grid.Contains(sword), Is.False);
            Assert.That(grid.TryPlace(sword, new GridCell(2, 0)), Is.True);
        });
    }

    [Test]
    public void TryPlace_EinesLiegendenItems_VerschiebtEs()
    {
        var grid   = new InventoryGrid(6, 4);
        var helmet = TestItems.Create(TestItems.Helmet);

        grid.TryPlace(helmet, new GridCell(0, 0));

        var wasMoved = grid.TryPlace(helmet, new GridCell(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(wasMoved, Is.True);
            Assert.That(grid.Count, Is.EqualTo(1));
            Assert.That(grid.GetItemAt(new GridCell(0, 0)), Is.Null);
            Assert.That(grid.GetItemAt(new GridCell(2, 2)), Is.SameAs(helmet));
        });
    }

    [Test]
    public void Remove_GibtAlleFelderFrei()
    {
        var grid  = new InventoryGrid(6, 4);
        var sword = TestItems.Create(TestItems.Sword);

        grid.TryPlace(sword, new GridCell(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(grid.Remove(sword), Is.True);
            Assert.That(grid.Remove(sword), Is.False);
            Assert.That(grid.GetItemAt(new GridCell(0, 2)), Is.Null);
            Assert.That(grid.Count, Is.Zero);
        });
    }

    [Test]
    public void TryAdd_NimmtDenErstenFreienPlatzZeileFuerZeile()
    {
        var grid = new InventoryGrid(4, 4);

        grid.TryPlace(TestItems.Create(TestItems.Helmet), new GridCell(0, 0));

        var sword  = TestItems.Create(TestItems.Sword);
        var potion = TestItems.Create(TestItems.Potion);
        var helmet = TestItems.Create(TestItems.Helmet);

        grid.TryAdd(sword);
        grid.TryAdd(potion);
        grid.TryAdd(helmet);

        Assert.Multiple(() =>
        {
            Assert.That(grid.GetPositionOf(sword), Is.EqualTo(new GridCell(2, 0)));
            Assert.That(grid.GetPositionOf(potion), Is.EqualTo(new GridCell(3, 0)));
            Assert.That(grid.GetPositionOf(helmet), Is.EqualTo(new GridCell(0, 2)));
        });
    }

    [Test]
    public void TryAdd_OhnePlatz_SchlaegtFehl()
    {
        var grid = new InventoryGrid(2, 2);

        grid.TryAdd(TestItems.Create(TestItems.Helmet));

        Assert.Multiple(() =>
        {
            Assert.That(grid.TryAdd(TestItems.Create(TestItems.Potion)), Is.False);
            Assert.That(grid.FindFreeCellFor(TestItems.Create(TestItems.Potion)), Is.Null);
        });
    }

    [Test]
    public void TryAdd_ItemGroesserAlsDasRaster_SchlaegtFehl()
        => Assert.That(new InventoryGrid(3, 2).TryAdd(TestItems.Create(TestItems.Sword)), Is.False);

    [Test]
    public void GetItemsUnder_NenntJedesVerdeckteItemEinmal()
    {
        var grid   = new InventoryGrid(6, 4);
        var helmet = TestItems.Create(TestItems.Helmet);
        var potion = TestItems.Create(TestItems.Potion);

        grid.TryPlace(helmet, new GridCell(0, 0));
        grid.TryPlace(potion, new GridCell(2, 1));

        var covered = grid.GetItemsUnder(TestItems.Create(TestItems.Helmet), new GridCell(1, 0));

        Assert.That(covered, Is.EquivalentTo(new[] { helmet, potion }));
    }

    [Test]
    public void ClampIntoGrid_SchiebtDasItemInsRaster()
    {
        var grid  = new InventoryGrid(6, 4);
        var sword = TestItems.Create(TestItems.Sword);

        Assert.Multiple(() =>
        {
            Assert.That(grid.ClampIntoGrid(sword, new GridCell(5, 3)), Is.EqualTo(new GridCell(5, 1)));
            Assert.That(grid.ClampIntoGrid(sword, new GridCell(-2, -2)), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(grid.ClampIntoGrid(sword, new GridCell(2, 1)), Is.EqualTo(new GridCell(2, 1)));
        });
    }

    [Test]
    public void GetItemsInReadingOrder_GehtZeileFuerZeile()
    {
        var grid   = new InventoryGrid(6, 4);
        var first  = TestItems.Create(TestItems.Potion);
        var second = TestItems.Create(TestItems.Helmet);
        var third  = TestItems.Create(TestItems.Potion);

        grid.TryPlace(third, new GridCell(0, 1));
        grid.TryPlace(second, new GridCell(3, 0));
        grid.TryPlace(first, new GridCell(1, 0));

        Assert.That(grid.GetItemsInReadingOrder().ToArray(), Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void Clear_LeertDasRaster()
    {
        var grid = new InventoryGrid(6, 4);

        grid.TryAdd(TestItems.Create(TestItems.Helmet));
        grid.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(grid.Count, Is.Zero);
            Assert.That(grid.GetItemAt(new GridCell(0, 0)), Is.Null);
        });
    }

    [Test]
    public void RasterOhneFelder_IstNichtErlaubt()
        => Assert.That(() => new InventoryGrid(0, 4), Throws.InstanceOf<ArgumentOutOfRangeException>());
}
