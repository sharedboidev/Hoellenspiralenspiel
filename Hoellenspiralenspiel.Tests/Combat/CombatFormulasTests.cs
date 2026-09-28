using Hoellenspiralenspiel.Scripts.Core.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class CombatFormulasTests
{
    [TestCase(10f, 0f, 10f)]
    [TestCase(10f, 10f, 8.3333f)]
    [TestCase(10f, 50f, 5f)]
    [TestCase(100f, 50f, 90.9091f)]
    [TestCase(0f, 50f, 0f)]
    public void Ruestung_MindertKleineTrefferStaerker(float damage, float armor, float expected)
        => Assert.That(CombatFormulas.MitigateByArmor(damage, armor), Is.EqualTo(expected).Within(0.001f));

    [Test]
    public void Ruestung_NegativeRuestungErhoehtDenSchadenNicht()
        => Assert.That(CombatFormulas.MitigateByArmor(10f, -20f), Is.EqualTo(10f));

    [TestCase(100f, 0f, 100f)]
    [TestCase(100f, 25f, 75f)]
    [TestCase(100f, 75f, 25f)]
    [TestCase(100f, 100f, 0f)]
    [TestCase(100f, 150f, 0f)]
    [TestCase(100f, -50f, 150f)]
    public void Resistenz_MindertProzentual(float damage, float resistance, float expected)
        => Assert.That(CombatFormulas.MitigateByResistance(damage, resistance), Is.EqualTo(expected).Within(0.001f));

    [TestCase(-5f, 0f)]
    [TestCase(42f, 42f)]
    [TestCase(180f, 100f)]
    public void Chancen_LiegenZwischenNullUndHundert(float chance, float expected)
        => Assert.That(CombatFormulas.ClampChance(chance), Is.EqualTo(expected));
}
