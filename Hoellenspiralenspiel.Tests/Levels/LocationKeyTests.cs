using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class LocationKeyTests
{
    [Test]
    public void AlsText_HeisstEineFlaecheF2_UndEineEbeneImDungeonF2D0L1()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LocationKey.Of(2).ToString(), Is.EqualTo("f2"));
            Assert.That(LocationKey.Of(2).InDungeon(0, 1).ToString(), Is.EqualTo("f2/d0/l1"));
        });
    }

    [TestCase("f1")]
    [TestCase("f12")]
    [TestCase("f2/d0/l1")]
    [TestCase("f3/d4/l2")]
    public void GeschriebenUndGelesen_ErgibtDenselbenOrt(string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(LocationKey.TryParse(text, out var key), Is.True);
            Assert.That(key.ToString(), Is.EqualTo(text));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("2")]
    [TestCase("f")]
    [TestCase("f-1")]
    [TestCase("x2")]
    [TestCase("f2/d0")]
    [TestCase("f2/d0/l0")]
    [TestCase("f2/x0/l1")]
    [TestCase("f2/d0/l1/x")]
    public void FremderText_IstKeinOrt(string text)
        => Assert.That(LocationKey.TryParse(text, out _), Is.False);

    [Test]
    public void GleicheOrteSindGleich_UndEinDungeonIstEinAndererOrt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LocationKey.Of(2), Is.EqualTo(new LocationKey(2)));
            Assert.That(LocationKey.Of(2).GetHashCode(), Is.EqualTo(new LocationKey(2).GetHashCode()));
            Assert.That(LocationKey.Of(2).InDungeon(0, 1), Is.Not.EqualTo(LocationKey.Of(2)));
            Assert.That(LocationKey.Of(2).InDungeon(0, 1), Is.Not.EqualTo(LocationKey.Of(2).InDungeon(0, 2)));
        });
    }

    [Test]
    public void OhneDungeon_GibtEsKeineDungeonEbene_AuchNichtAlsStandardwert()
    {
        var withoutDungeon = new LocationKey(3, -5, 4);
        var empty          = default(LocationKey);

        Assert.Multiple(() =>
        {
            Assert.That(withoutDungeon.IsInDungeon, Is.False);
            Assert.That(withoutDungeon.DungeonLevel, Is.Zero);
            Assert.That(withoutDungeon, Is.EqualTo(LocationKey.Of(3)));
            Assert.That(empty.IsInDungeon, Is.False);
            Assert.That(empty.Dungeon, Is.EqualTo(LocationKey.NoDungeon));
            Assert.That(empty.IsValid, Is.False, "Tiefe 0 ist kein Ort im Kreis");
        });
    }
}
