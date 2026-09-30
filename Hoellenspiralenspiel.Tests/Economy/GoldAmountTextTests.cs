using Hoellenspiralenspiel.Scripts.Core.Economy;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Economy;

[TestFixture]
public class GoldAmountTextTests
{
    [TestCase("")]
    [TestCase(null)]
    public void Parse_OhneText_IstNull(string text)
        => Assert.That(GoldAmountText.Parse(text), Is.Zero);

    [TestCase("7", 7)]
    [TestCase("007", 7)]
    [TestCase("1250", 1250)]
    [TestCase("2147483646", 2147483646)]
    public void Parse_LiestDieZahl(string text, int expected)
        => Assert.That(GoldAmountText.Parse(text), Is.EqualTo(expected));

    [TestCase("2147483647")]
    [TestCase("2147483648")]
    [TestCase("9999999999")]
    [TestCase("99999999999999999999999")]
    public void Parse_ZuGross_IstDasHoechsteGold(string text)
        => Assert.That(GoldAmountText.Parse(text), Is.EqualTo(int.MaxValue));

    [Test]
    public void Parse_UeberliestAllesAusserZiffern()
        => Assert.That(GoldAmountText.Parse("1.000 Gold"), Is.EqualTo(1000));

    [Test]
    public void KeepDigits_LaesstReineZiffernUnveraendert()
        => Assert.That(GoldAmountText.KeepDigits("1250", 2), Is.EqualTo(("1250", 2)));

    [Test]
    public void KeepDigits_EingefuegterText_BehaeltNurDieZiffern()
        => Assert.That(GoldAmountText.KeepDigits("1.000 Gold", 10), Is.EqualTo(("1000", 4)));

    [Test]
    public void KeepDigits_DerCaretRuecktUmEntfernteZeichenDavorZurueck()
        => Assert.That(GoldAmountText.KeepDigits("1a2", 2), Is.EqualTo(("12", 1)));

    [Test]
    public void KeepDigits_EntfernteZeichenHinterDemCaret_VerschiebenIhnNicht()
        => Assert.That(GoldAmountText.KeepDigits("12x3", 2), Is.EqualTo(("123", 2)));

    [Test]
    public void KeepDigits_ZiffernAndererSchriften_FallenWeg()
    {
        var arabicTwoThree = new string([(char)0x0662, (char)0x0663]);

        Assert.That(GoldAmountText.KeepDigits("1" + arabicTwoThree, 3), Is.EqualTo(("1", 1)));
    }

    [Test]
    public void KeepDigits_KuerztNachDemFilternAufZehnZiffern()
    {
        var (text, caret) = GoldAmountText.KeepDigits("1.000.000.000.000", 17);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("1000000000"));
            Assert.That(text, Has.Length.EqualTo(GoldAmountText.MaxDigits));
            Assert.That(caret, Is.EqualTo(GoldAmountText.MaxDigits));
        });
    }

    [Test]
    public void KeepDigits_EineElfteZiffer_WirdAbgewiesen()
        => Assert.That(GoldAmountText.KeepDigits("92147483647", 1), Is.EqualTo(("2147483647", 0)));

    [Test]
    public void KeepDigits_EineElfteZifferInDerMitte_VerdraengtNichtDasEnde()
        => Assert.That(GoldAmountText.KeepDigits("12345678950", 10), Is.EqualTo(("1234567890", 9)));

    [TestCase("", 0)]
    [TestCase(null, 0)]
    [TestCase("abc", 3)]
    public void KeepDigits_OhneZiffern_BleibtNichts(string text, int caret)
        => Assert.That(GoldAmountText.KeepDigits(text, caret), Is.EqualTo((string.Empty, 0)));
}
