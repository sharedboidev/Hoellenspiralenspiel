using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class ProjectileSettingsTests
{
    [Test]
    public void Reach_IstGeschwindigkeitMalLebenszeit()
        => Assert.That(new ProjectileSettings(800, 2.5f).Reach, Is.EqualTo(2000f));

    [Test]
    public void OhneFork_BleibtEsBeiEinemProjektil()
    {
        var settings = new ProjectileSettings(800, 2.5f);

        Assert.Multiple(() =>
        {
            Assert.That(settings.CanFork, Is.False);
            Assert.That(settings.MaxProjectiles, Is.EqualTo(1));
        });
    }

    [TestCase(2, 1, 3)]
    [TestCase(2, 2, 7)]
    [TestCase(3, 2, 13)]
    [TestCase(2, 5, 63)]
    public void MaxProjectiles_ZaehltAlleGenerationen(int forkCount, int generations, int expected)
        => Assert.That(new ProjectileSettings(800, 2.5f, forkCount, generations, 600).MaxProjectiles, Is.EqualTo(expected));

    [Test]
    public void ForkOhneReichweite_ForktNicht()
        => Assert.That(new ProjectileSettings(800, 2.5f, 2, 2).CanFork, Is.False);

    [Test]
    public void Nahkampfwaffe_HatKeinProjektil()
        => Assert.That(WeaponProfile.Unarmed.GetProjectile(), Is.Null);

    [Test]
    public void Fernkampfwaffe_SchiesstSoWeitWieSieReicht()
    {
        var bow = new WeaponProfile(5, 11, 1.2f, 6, DamageType.Pierce, 700, true, 1400);

        var projectile = bow.GetProjectile();

        Assert.Multiple(() =>
        {
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.Speed, Is.EqualTo(1400f));
            Assert.That(projectile.Reach, Is.EqualTo(bow.Reach).Within(0.01f));
            Assert.That(projectile.CanFork, Is.False);
        });
    }

    [Test]
    public void Reichweite_DerWaffe_HatEtwasSpielraum()
        => Assert.That(WeaponProfile.Unarmed.Reach, Is.EqualTo(WeaponProfile.DefaultMeleeRange * WeaponProfile.RangeTolerance));
}
