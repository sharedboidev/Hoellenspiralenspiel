using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Rückmeldung des Users vom 07.10.2026: Ein Schlag muss den Gegner vor dem Helden treffen, auch wenn er nicht angeklickt wurde
[TestFixture]
public class StrikeHitboxTests
{
    private const float Reach        = 125f;
    private const float CasterRadius = 30f;
    private const float BodyRadius   = 35f;

    private sealed record Dummy(string Name, float X, float Y, float Radius = BodyRadius);

    private static Dummy Pick(params Dummy[] candidates)
        => StrikeHitbox.PickNearest(new List<Dummy>(candidates), dummy => (dummy.X, dummy.Y, dummy.Radius), 1f, 0f, Reach, CasterRadius);

    [Test]
    public void DerKegelIstNeunzigGradBreit()
        => Assert.That(StrikeHitbox.ArcDegrees, Is.EqualTo(90f));

    [Test]
    public void VorDemHelden_InReichweite_WirdGetroffen()
        => Assert.That(StrikeHitbox.Reaches(150f, 0f, 1f, 0f, Reach, CasterRadius, BodyRadius), Is.True);

    [Test]
    public void AusserReichweite_WirdNichtGetroffen()
        => Assert.That(StrikeHitbox.Reaches(200f, 0f, 1f, 0f, Reach, CasterRadius, BodyRadius), Is.False, "200 - 30 - 35 = 135 px von Rand zu Rand, die Waffe reicht 125");

    [Test]
    public void SeitlichUndHinten_WirdNichtGetroffen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StrikeHitbox.Reaches(0f, 120f, 1f, 0f, Reach, CasterRadius, BodyRadius), Is.False, "90° zur Seite liegt außerhalb des Kegels von ±45°");
            Assert.That(StrikeHitbox.Reaches(-120f, 0f, 1f, 0f, Reach, CasterRadius, BodyRadius), Is.False, "hinter dem Helden");
            Assert.That(StrikeHitbox.Reaches(100f, 90f, 1f, 0f, Reach, CasterRadius, BodyRadius), Is.True, "knapp über 45°, doch der Körper ragt in den Kegel");
        });
    }

    [Test]
    public void DerNaechsteInDerZone_Gewinnt()
    {
        var near = new Dummy("near", 90f, 20f);
        var far  = new Dummy("far", 150f, -10f);

        Assert.Multiple(() =>
        {
            Assert.That(Pick(far, near), Is.SameAs(near));
            Assert.That(Pick(near, far), Is.SameAs(near), "die Reihenfolge spielt keine Rolle");
        });
    }

    [Test]
    public void WerHinterOderNebenDemHeldenSteht_ZaehltNicht()
    {
        var behind = new Dummy("behind", -80f, 0f);
        var aside  = new Dummy("aside", 10f, 110f);
        var ahead  = new Dummy("ahead", 160f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(Pick(behind, aside, ahead), Is.SameAs(ahead));
            Assert.That(Pick(behind, aside), Is.Null, "niemand vor dem Helden");
            Assert.That(Pick(), Is.Null);
        });
    }

    [Test]
    public void EinGrosserKoerper_ZaehltMitSeinemRand()
    {
        var big   = new Dummy("big", 180f, 0f, 60f);
        var small = new Dummy("small", 180f, 0f, 20f);

        Assert.Multiple(() =>
        {
            Assert.That(Pick(big), Is.SameAs(big), "180 - 30 - 60 = 90 px bis zum Rand");
            Assert.That(Pick(small), Is.Null, "180 - 30 - 20 = 130 px, knapp zu weit");
        });
    }

    [Test]
    public void OhneKandidatenliste_WirdGeworfen()
        => Assert.That(() => StrikeHitbox.PickNearest<Dummy>(null, dummy => (dummy.X, dummy.Y, dummy.Radius), 1f, 0f, Reach, CasterRadius), Throws.ArgumentNullException);
}
