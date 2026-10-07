using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Seit dem 07.10.2026 läuft der Held zu keinem Gegner mehr hin: Jeder Angriff geht aus dem Stand los
[TestFixture]
public class AttackOrdersTests
{
    [Test]
    public void MitZiel_GreiftJederAn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AttackOrders.IsAllowed(true, true, false), Is.True);
            Assert.That(AttackOrders.IsAllowed(false, true, false), Is.True);
        });
    }

    [Test]
    public void NahkampfOhneZiel_PassiertNichts()
        => Assert.That(AttackOrders.IsAllowed(true, false, false), Is.False);

    [Test]
    public void FernkampfOhneZiel_SchiesstRichtungMaus()
        => Assert.That(AttackOrders.IsAllowed(false, false, false), Is.True);

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Stehenbleiben_SchlaegtImmer_AuchInsLeere(bool isMelee, bool hasTarget)
        => Assert.That(AttackOrders.IsAllowed(isMelee, hasTarget, true), Is.True);

    [Test]
    public void OhneAngriff_LaeuftDerHeldMitDerRichtung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AttackOrders.MovementCancels(true, false, false), Is.True);
            Assert.That(AttackOrders.MovementCancels(true, false, true), Is.True);
            Assert.That(AttackOrders.MovementCancels(false, false, false), Is.False, "ohne Richtung gibt es nichts abzubrechen");
        });
    }

    [Test]
    public void GehalteneRichtung_WartetAufDenAngriff()
        => Assert.That(AttackOrders.MovementCancels(true, true, false), Is.False);

    [Test]
    public void NeuGedrueckteRichtung_BrichtDenAngriffAb()
        => Assert.That(AttackOrders.MovementCancels(true, true, true), Is.True);

    //Wunsch des Users vom 07.10.2026: Während jeder Skill läuft, läuft der Held langsamer, der Anteil ist am Helden einstellbar
    [Test]
    public void WaehrendEinesSkills_LaeuftDerHeldMitDemEingestelltenAnteil()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AttackOrders.GetSkillWalkFactor(50f), Is.EqualTo(0.5f));
            Assert.That(AttackOrders.GetSkillWalkFactor(0f), Is.Zero, "0 heißt stehen wie früher");
            Assert.That(AttackOrders.GetSkillWalkFactor(150f), Is.EqualTo(1f), "schneller als sonst läuft er nicht");
            Assert.That(AttackOrders.GetSkillWalkFactor(-5f), Is.Zero);
        });
    }
}
