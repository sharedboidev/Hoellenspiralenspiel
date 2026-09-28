using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Stats;

[TestFixture]
public class StatFormulasTests
{
    [Test]
    public void Combine_OhneModifier_LiefertDenGrundwert()
        => Assert.That(StatFormulas.Combine(50, 0, 0, 1), Is.EqualTo(50f));

    [Test]
    public void Combine_Flat_WirdVorDenMultiplikatorenAddiert()
        => Assert.That(StatFormulas.Combine(50, 10, 0.5f, 1), Is.EqualTo(90f).Within(0.0001f));

    [Test]
    public void Combine_IncreasedUndMore_WerdenMiteinanderMultipliziert()
    {
        //Das Beispiel aus dem Spiel: 14 % und 22 % increased, dazu 2,97 % more
        var multiplier = StatFormulas.Combine(1, 0, 0.14f + 0.22f, 1.0297f);

        Assert.That(multiplier, Is.EqualTo(1.4004f).Within(0.0001f));
    }

    [TestCase(1, 1, 9)]
    [TestCase(10, 1, 18)]
    [TestCase(1, 10, 36)]
    [TestCase(26, 1, 34)]
    public void GetLifeBase_FolgtDerFormelAusDemDesigndokument(int strength, int constitution, float expected)
        => Assert.That(StatFormulas.GetLifeBase(strength, constitution), Is.EqualTo(expected));

    [TestCase(1, 1, 9)]
    [TestCase(1, 4, 24)]
    [TestCase(10, 1, 18)]
    public void GetManaBase_FolgtDerFormelAusDemDesigndokument(int awareness, int intelligence, float expected)
        => Assert.That(StatFormulas.GetManaBase(awareness, intelligence), Is.EqualTo(expected));

    [TestCase(1, 1, 0)]
    [TestCase(26, 1, 5)]
    [TestCase(5, 3, 2)]
    public void GetLiferegenerationBase_SchneidetNachkommastellenAb(int strength, int constitution, float expected)
        => Assert.That(StatFormulas.GetLiferegenerationBase(strength, constitution), Is.EqualTo(expected));

    [TestCase(3.99f, 3f)]
    [TestCase(-3.99f, -3f)]
    [TestCase(4f, 4f)]
    public void TruncateAttribute_RundetZurNullHin(float value, float expected)
        => Assert.That(StatFormulas.TruncateAttribute(value), Is.EqualTo(expected));
}
