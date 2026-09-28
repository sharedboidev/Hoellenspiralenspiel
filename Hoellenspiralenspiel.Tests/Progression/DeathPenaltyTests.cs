using Hoellenspiralenspiel.Scripts.Core.Progression;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Progression;

[TestFixture]
public class DeathPenaltyTests
{
    private const long LevelFloor = 1760;
    private const long NextLevel  = 3781;

    [Test]
    public void Verlust_IstEinZehntelDerLevelspanne()
        => Assert.That(DeathPenalty.GetXpLoss(3000, LevelFloor, NextLevel), Is.EqualTo(202));

    [Test]
    public void Verlust_KostetKeinLevel()
        => Assert.That(DeathPenalty.GetXpLoss(1800, LevelFloor, NextLevel), Is.EqualTo(40));

    [Test]
    public void Verlust_IstNullAmAnfangDesLevels()
        => Assert.That(DeathPenalty.GetXpLoss(LevelFloor, LevelFloor, NextLevel), Is.Zero);

    [Test]
    public void Verlust_IstNullAufDemHoechstenLevel()
        => Assert.That(DeathPenalty.GetXpLoss(5_000_000_000, 4_250_334_444, 4_250_334_444), Is.Zero);

    [TestCase(0f, 0)]
    [TestCase(0.5f, 1010)]
    [TestCase(1f, 1240)]
    [TestCase(3f, 1240)]
    public void Anteil_IstEinstellbar(float fraction, long expected)
        => Assert.That(DeathPenalty.GetXpLoss(3000, LevelFloor, NextLevel, fraction), Is.EqualTo(expected));
}
