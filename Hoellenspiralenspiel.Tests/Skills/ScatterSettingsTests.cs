using System;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class ScatterSettingsTests
{
    private const float TargetRadius = 30f;
    private const float Tolerance    = 0.001f;

    private static readonly ScatterSettings MagmaBalls = new(3, 65f, 75f, 0.6f);

    private static float DistanceOf((float X, float Y) offset)
        => MathF.Sqrt(offset.X * offset.X + offset.Y * offset.Y);

    private static float DegreesOf((float X, float Y) offset)
        => (MathF.Atan2(offset.Y, offset.X) * 180f / MathF.PI + 360f) % 360f;

    [Test]
    public void JedeKugel_BekommtEinenEinschlag()
        => Assert.That(MagmaBalls.PickLandings(TargetRadius, new SeededRandom(1)), Has.Count.EqualTo(3));

    [Test]
    public void OhneKugeln_GibtEsKeineEinschlaege_UndKeinenWurf()
    {
        var random = new FixedRandom(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(new ScatterSettings(0, 65f, 75f, 0.6f).PickLandings(TargetRadius, random), Is.Empty);
            Assert.That(random.Draws, Is.Zero);
        });
    }

    //Wie SkillArea prüft: Der Einschlag reicht bis zum Körper des Ziels
    [Test]
    public void JederEinschlag_TrifftDasStehendeZiel([Values(0f, 30f, 120f)] float targetRadius)
    {
        var landings = Enumerable.Range(0, 500).SelectMany(seed => MagmaBalls.PickLandings(targetRadius, new SeededRandom(seed))).ToList();

        Assert.That(landings.Count(landing => !AreaSettings.Contains(landing.X, landing.Y, MagmaBalls.ImpactRadius + targetRadius)), Is.Zero);
    }

    [Test]
    public void Kugeln_LandenNebenDemKoerper_NichtDarin([Values(0f, 30f, 120f)] float targetRadius)
    {
        var landings = Enumerable.Range(0, 500).SelectMany(seed => MagmaBalls.PickLandings(targetRadius, new SeededRandom(seed))).ToList();

        Assert.That(landings.Min(DistanceOf), Is.GreaterThanOrEqualTo(targetRadius - Tolerance));
    }

    [Test]
    public void Kugeln_StreuenBisNaheAnDenRandDesErreichbaren()
    {
        var landings = Enumerable.Range(0, 500).SelectMany(seed => MagmaBalls.PickLandings(TargetRadius, new SeededRandom(seed))).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(landings.Max(DistanceOf), Is.GreaterThan(TargetRadius + MagmaBalls.ImpactRadius * 0.85f));
            Assert.That(landings.Min(DistanceOf), Is.LessThan(TargetRadius + 5f));
        });
    }

    //Erster Wurf: der Start. Danach je Kugel der Platz im Sektor und der Abstand
    [Test]
    public void JedeKugel_LandetInIhremEigenenDrittel()
    {
        var landings = MagmaBalls.PickLandings(TargetRadius, new FixedRandom(0f, 0f, 0f, 0f, 0.5f, 0f, 0.999f, 0f));

        Assert.Multiple(() =>
        {
            Assert.That(DegreesOf(landings[0]), Is.EqualTo(0f).Within(0.01f));
            Assert.That(DegreesOf(landings[1]), Is.EqualTo(180f).Within(0.01f));
            Assert.That(DegreesOf(landings[2]), Is.EqualTo(359.88f).Within(0.01f));
        });
    }

    [Test]
    public void Abstand_IstUeberDieFlaecheDesRingsVerteilt()
    {
        var inner    = TargetRadius;
        var outer    = TargetRadius + MagmaBalls.ImpactRadius * 0.9f;
        var landings = MagmaBalls.PickLandings(TargetRadius, new FixedRandom(0f, 0f, 0f, 0f, 0f, 0.5f, 0f, 0.999f));

        Assert.Multiple(() =>
        {
            Assert.That(DistanceOf(landings[0]), Is.EqualTo(inner).Within(Tolerance), "am Körper");
            Assert.That(DistanceOf(landings[1]), Is.EqualTo(MathF.Sqrt((inner * inner + outer * outer) / 2f)).Within(Tolerance), "die halbe Fläche liegt innen");
            Assert.That(DistanceOf(landings[2]), Is.EqualTo(outer).Within(0.1f), "am äußeren Rand");
        });
    }

    [Test]
    public void GleicherSeed_GibtDieselbenEinschlaege()
        => Assert.That(MagmaBalls.PickLandings(TargetRadius, new SeededRandom(42)), Is.EqualTo(MagmaBalls.PickLandings(TargetRadius, new SeededRandom(42))));

    [Test]
    public void Kugeln_SchlagenMitDemElementDesSchlagsEin_UndIhremEigenenAnteil()
    {
        var strike = new AttackDefinition("Magma Strike", 80f, DamageType.Fire);
        var balls  = MagmaBalls.GetAttack(strike);

        Assert.Multiple(() =>
        {
            Assert.That(balls.WeaponDamagePercent, Is.EqualTo(65f));
            Assert.That(balls.DealtAs, Is.EqualTo(DamageType.Fire));
            Assert.That(balls.Name, Is.EqualTo("Magma Strike"));
        });
    }
}
