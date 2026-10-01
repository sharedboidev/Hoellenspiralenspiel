using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class CircleUnlockRuleTests
{
    [Test]
    public void DerBossDerLetztenEbeneOeffnetDenNaechstenKreis()
    {
        Assert.That(CircleUnlockRule.NextCircle(1, 4, 4), Is.EqualTo(2));
        Assert.That(CircleUnlockRule.NextCircle(2, 4, 4), Is.EqualTo(3));
    }

    [Test]
    public void VorDerLetztenEbeneOeffnetSichNichts()
    {
        Assert.That(CircleUnlockRule.NextCircle(1, 3, 4), Is.Zero);
        Assert.That(CircleUnlockRule.NextCircle(1, 1, 4), Is.Zero);
    }

    [Test]
    public void TieferAlsDieLetzteEbeneZaehltAlsLetzte()
    {
        Assert.That(CircleUnlockRule.NextCircle(1, 5, 4), Is.EqualTo(2));
    }

    [Test]
    public void NachDemLetztenKreisKommtKeiner()
    {
        Assert.That(CircleUnlockRule.NextCircle(JourneyState.LastCircle, 4, 4), Is.Zero);
        Assert.That(CircleUnlockRule.NextCircle(0, 4, 4), Is.Zero);
    }

    [Test]
    public void DieReiseKenntHoechstensNeunKreise()
    {
        var journey = new JourneyState();

        journey.Unlock(12);

        Assert.That(journey.UnlockedCircles, Is.EqualTo(JourneyState.LastCircle));
        Assert.That(journey.IsUnlocked(9), Is.True);
        Assert.That(journey.IsUnlocked(10), Is.False);

        journey.Reset(20);

        Assert.That(journey.UnlockedCircles, Is.EqualTo(JourneyState.LastCircle));
    }

    [Test]
    public void FreischaltenGehtNieZurueck()
    {
        var journey = new JourneyState();

        journey.Unlock(3);
        journey.Unlock(2);

        Assert.That(journey.UnlockedCircles, Is.EqualTo(3));
    }
}
