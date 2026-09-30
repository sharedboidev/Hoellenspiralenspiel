using Hoellenspiralenspiel.Scripts.Core.Economy;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class PurseTests
{
    [Test]
    public void EinNeuerBeutelIstLeer()
        => Assert.That(new Purse().Amount, Is.Zero);

    [Test]
    public void Add_ErhoehtDenBetragUndMeldetSich()
    {
        var purse   = new Purse();
        var changes = 0;

        purse.Changed += () => changes++;

        purse.Add(12);
        purse.Add(30);

        Assert.Multiple(() =>
        {
            Assert.That(purse.Amount, Is.EqualTo(42));
            Assert.That(changes, Is.EqualTo(2));
        });
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Add_OhneBetrag_AendertNichts(int amount)
    {
        var purse   = new Purse();
        var changes = 0;

        purse.Add(10);

        purse.Changed += () => changes++;

        purse.Add(amount);

        Assert.Multiple(() =>
        {
            Assert.That(purse.Amount, Is.EqualTo(10));
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void Add_LaeuftNichtUeber()
    {
        var purse = new Purse();

        purse.Add(int.MaxValue);
        purse.Add(1000);

        Assert.That(purse.Amount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void TrySpend_ZiehtDenPreisAb()
    {
        var purse = new Purse();

        purse.Add(50);

        Assert.Multiple(() =>
        {
            Assert.That(purse.TrySpend(20), Is.True);
            Assert.That(purse.Amount, Is.EqualTo(30));
        });
    }

    [Test]
    public void TrySpend_DerLetzteRestGehtAuf()
    {
        var purse = new Purse();

        purse.Add(50);

        Assert.Multiple(() =>
        {
            Assert.That(purse.TrySpend(50), Is.True);
            Assert.That(purse.Amount, Is.Zero);
        });
    }

    [Test]
    public void TrySpend_ZuTeuer_LaesstDenBetragStehen()
    {
        var purse   = new Purse();
        var changes = 0;

        purse.Add(50);

        purse.Changed += () => changes++;

        Assert.Multiple(() =>
        {
            Assert.That(purse.TrySpend(51), Is.False);
            Assert.That(purse.Amount, Is.EqualTo(50));
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void TrySpend_NegativerPreis_IstKeinGewinn()
    {
        var purse = new Purse();

        purse.Add(50);

        Assert.Multiple(() =>
        {
            Assert.That(purse.TrySpend(-10), Is.False);
            Assert.That(purse.Amount, Is.EqualTo(50));
        });
    }

    [Test]
    public void TakeAll_LeertDenBeutel()
    {
        var purse = new Purse();

        purse.Add(77);

        Assert.Multiple(() =>
        {
            Assert.That(purse.TakeAll(), Is.EqualTo(77));
            Assert.That(purse.Amount, Is.Zero);
            Assert.That(purse.TakeAll(), Is.Zero);
        });
    }

    [Test]
    public void Restore_SetztDenBetragUndNieUnterNull()
    {
        var purse = new Purse();

        purse.Restore(500);

        Assert.That(purse.Amount, Is.EqualTo(500));

        purse.Restore(-3);

        Assert.That(purse.Amount, Is.Zero);
    }

    [Test]
    public void Move_BewegtDenBetragZwischenZweiBeuteln()
    {
        var carried = new Purse();
        var stashed = new Purse();

        carried.Add(100);

        Assert.Multiple(() =>
        {
            Assert.That(Purse.Move(carried, stashed, 40), Is.EqualTo(40));
            Assert.That(carried.Amount, Is.EqualTo(60));
            Assert.That(stashed.Amount, Is.EqualTo(40));
        });
    }

    [Test]
    public void Move_BewegtHoechstensWasDaIst()
    {
        var carried = new Purse();
        var stashed = new Purse();

        carried.Add(30);

        Assert.Multiple(() =>
        {
            Assert.That(Purse.Move(carried, stashed, 1000), Is.EqualTo(30));
            Assert.That(carried.Amount, Is.Zero);
            Assert.That(stashed.Amount, Is.EqualTo(30));
        });
    }

    [Test]
    public void Move_EinVollesZielNimmtNichtsMehr()
    {
        var carried = new Purse();
        var stashed = new Purse();

        carried.Add(30);
        stashed.Add(int.MaxValue - 10);

        Assert.Multiple(() =>
        {
            Assert.That(Purse.Move(carried, stashed, 30), Is.EqualTo(10));
            Assert.That(carried.Amount, Is.EqualTo(20));
            Assert.That(stashed.Amount, Is.EqualTo(int.MaxValue));
        });
    }

    [TestCase(0)]
    [TestCase(-20)]
    public void Move_OhneBetrag_BewegtNichts(int amount)
    {
        var carried = new Purse();
        var stashed = new Purse();

        carried.Add(30);

        Assert.Multiple(() =>
        {
            Assert.That(Purse.Move(carried, stashed, amount), Is.Zero);
            Assert.That(carried.Amount, Is.EqualTo(30));
            Assert.That(stashed.Amount, Is.Zero);
        });
    }

    [Test]
    public void Move_InDenselbenBeutel_BewegtNichts()
    {
        var purse = new Purse();

        purse.Add(30);

        Assert.Multiple(() =>
        {
            Assert.That(Purse.Move(purse, purse, 10), Is.Zero);
            Assert.That(purse.Amount, Is.EqualTo(30));
        });
    }
}
