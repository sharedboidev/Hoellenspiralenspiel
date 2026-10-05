using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class SweepSettingsTests
{
    private const float CasterRadius = 30f;
    private const float TargetRadius = 30f;

    private static readonly WeaponProfile Sword = new(4, 9, 1.4f, 5, DamageType.Slash, WeaponProfile.DefaultMeleeRange);

    private static readonly SweepSettings Cleave = new(180f, 1.5f);

    private static float Reach => Cleave.GetReach(Sword);

    //Blick nach +X
    private static bool Hits(SweepSettings sweep, float offsetX, float offsetY, float targetRadius = TargetRadius)
        => sweep.Reaches(offsetX, offsetY, 1f, 0f, sweep.GetReach(Sword), CasterRadius, targetRadius);

    [Test]
    public void Radius_IstDieReichweiteDerWaffeMalFaktor()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cleave.GetReach(Sword), Is.EqualTo(100f * WeaponProfile.RangeTolerance * 1.5f));
            Assert.That(Cleave.GetEngageRange(Sword), Is.EqualTo(150f), "der Held läuft wie beim Schlag ohne Toleranz heran");
            Assert.That(Cleave.GetReach(WeaponProfile.Unarmed), Is.EqualTo(WeaponProfile.UnarmedRange * WeaponProfile.RangeTolerance * 1.5f));
        });
    }

    [Test]
    public void VorDemSchlagendenInReichweite_WirdGetroffen()
        => Assert.That(Hits(Cleave, 200f, 0f), Is.True);

    [Test]
    public void Reichweite_ZaehltVonRandZuRand()
    {
        var edgeToEdge = CasterRadius + TargetRadius + Reach;

        Assert.Multiple(() =>
        {
            Assert.That(Hits(Cleave, edgeToEdge - 0.5f, 0f), Is.True);
            Assert.That(Hits(Cleave, edgeToEdge + 0.5f, 0f), Is.False);
            Assert.That(Hits(Cleave, CasterRadius + 100f + Reach - 0.5f, 0f, 100f), Is.True, "ein großer Körper ragt weiter herein");
        });
    }

    [Test]
    public void HinterDemSchlagenden_WirdNichtGetroffen()
        => Assert.That(Hits(Cleave, -100f, 0f), Is.False);

    [Test]
    public void GenauSeitlich_WirdGetroffen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Hits(Cleave, 0f, 100f), Is.True);
            Assert.That(Hits(Cleave, 0f, -100f), Is.True);
        });
    }

    [Test]
    public void KnappHinterDerSeite_ZaehltNurMitDemKoerper()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Hits(Cleave, -20f, 100f), Is.True, "der Körper ragt 10 Pixel über die Linie");
            Assert.That(Hits(Cleave, -40f, 100f), Is.False, "der Körper endet 10 Pixel hinter der Linie");
        });
    }

    [Test]
    public void SchmalerBogen_TrifftNurInDerMitte()
    {
        var narrow = new SweepSettings(90f, 1.5f);

        Assert.Multiple(() =>
        {
            Assert.That(Hits(narrow, 100f, 100f), Is.True);
            Assert.That(Hits(narrow, 0f, 100f), Is.False);
        });
    }

    [Test]
    public void VollerKreis_TrifftAuchHinten()
        => Assert.That(Hits(new SweepSettings(SweepSettings.FullCircleDegrees, 1.5f), -100f, 0f), Is.True);

    [Test]
    public void ZielAufDemSchlagenden_WirdImmerGetroffen()
        => Assert.That(Cleave.Reaches(10f, 0f, -1f, 0f, Reach, CasterRadius, TargetRadius), Is.True);

    [Test]
    public void Blickrichtung_MussNichtNormiertSein()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cleave.Reaches(-20f, 100f, 5f, 0f, Reach, CasterRadius, TargetRadius), Is.True);
            Assert.That(Cleave.Reaches(-40f, 100f, 5f, 0f, Reach, CasterRadius, TargetRadius), Is.False);
        });
    }
}
