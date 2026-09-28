using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class ProjectileSpreadTests
{
    private static float[] GetFan(int count)
        => Enumerable.Range(0, count).Select(index => ProjectileSpread.GetOffsetDegrees(index, count)).ToArray();

    [Test]
    public void EinProjektil_FliegtGeradeaus()
        => Assert.That(GetFan(1), Is.EqualTo(new[] { 0f }));

    [Test]
    public void ZweiProjektile_LiegenBeiderseitsDerZielrichtung()
        => Assert.That(GetFan(2), Is.EqualTo(new[] { -6f, 6f }));

    [Test]
    public void DreiProjektile_HabenEinesInDerMitte()
        => Assert.That(GetFan(3), Is.EqualTo(new[] { -12f, 0f, 12f }));

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(13)]
    public void DerFaecher_IstSymmetrisch(int count)
    {
        var fan = GetFan(count);

        Assert.That(fan.Sum(), Is.EqualTo(0f).Within(0.001f));
    }

    [TestCase(6)]
    [TestCase(20)]
    public void DerFaecher_WirdNichtBreiterAlsDieObergrenze(int count)
    {
        var fan = GetFan(count);

        Assert.Multiple(() =>
        {
            Assert.That(fan.First(), Is.EqualTo(-ProjectileSpread.MaxTotalDegrees / 2f).Within(0.001f));
            Assert.That(fan.Last(), Is.EqualTo(ProjectileSpread.MaxTotalDegrees / 2f).Within(0.001f));
        });
    }

    [Test]
    public void KeinOderNegativeAnzahl_FliegtGeradeaus()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ProjectileSpread.GetOffsetDegrees(0, 0), Is.Zero);
            Assert.That(ProjectileSpread.GetOffsetDegrees(0, -3), Is.Zero);
        });
    }
}
