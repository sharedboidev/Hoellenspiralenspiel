using System;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class RainSettingsTests
{
    private const float Tolerance = 0.001f;

    private static readonly RainSettings DarkenSky = new(5, 200f, 75f, 0.5f, 1f);

    private static float DistanceOf((float X, float Y) offset)
        => MathF.Sqrt(offset.X * offset.X + offset.Y * offset.Y);

    private static float DegreesOf((float X, float Y) offset)
        => (MathF.Atan2(offset.Y, offset.X) * 180f / MathF.PI + 360f) % 360f;

    [Test]
    public void Bonusprojektile_KommenAlsPfeileDazu()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DarkenSky.GetCount(0), Is.EqualTo(5));
            Assert.That(DarkenSky.GetCount(2), Is.EqualTo(7));
            Assert.That(DarkenSky.GetCount(-3), Is.EqualTo(5), "weniger als null Bonus gibt es nicht");
            Assert.That(new RainSettings(-1, 200f, 75f, 0.5f, 1f).GetCount(1), Is.EqualTo(1));
        });
    }

    [Test]
    public void Einschlaege_VerteilenSichVomVorlaufUeberDieDauer()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DarkenSky.GetImpactDelays(5), Is.EqualTo(new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f }).Within(Tolerance));
            Assert.That(DarkenSky.GetImpactDelays(2), Is.EqualTo(new[] { 0.5f, 1.5f }).Within(Tolerance));
            Assert.That(DarkenSky.GetImpactDelays(1), Is.EqualTo(new[] { 0.5f }).Within(Tolerance), "ein einzelner Pfeil kommt nach dem Vorlauf");
            Assert.That(DarkenSky.GetImpactDelays(0), Is.Empty);
        });
    }

    [Test]
    public void MehrPfeile_FallenImSelbenZeitraum()
    {
        var delays = DarkenSky.GetImpactDelays(9);

        Assert.Multiple(() =>
        {
            Assert.That(delays, Has.Count.EqualTo(9));
            Assert.That(delays[0], Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(delays[^1], Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(delays, Is.Ordered.Ascending);
        });
    }

    [Test]
    public void JederPfeil_BekommtEinenEinschlag()
        => Assert.That(DarkenSky.PickLandings(7, new SeededRandom(1)), Has.Count.EqualTo(7));

    [Test]
    public void OhnePfeile_GibtEsKeineEinschlaege_UndKeinenWurf()
    {
        var random = new FixedRandom(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(DarkenSky.PickLandings(0, random), Is.Empty);
            Assert.That(random.Draws, Is.Zero);
        });
    }

    [Test]
    public void KeinEinschlag_LiegtAusserhalbDesRadius()
    {
        var landings = Enumerable.Range(0, 500).SelectMany(seed => DarkenSky.PickLandings(5, new SeededRandom(seed))).ToList();

        Assert.That(landings.Max(DistanceOf), Is.LessThanOrEqualTo(DarkenSky.Radius + Tolerance));
    }

    //Gleichverteilt über den Radius, nicht über die Fläche: Die Hälfte fällt in die innere Hälfte des Radius
    [Test]
    public void Einschlaege_LiegenDichterZurMitte()
    {
        var landings = Enumerable.Range(0, 500).SelectMany(seed => DarkenSky.PickLandings(5, new SeededRandom(seed))).ToList();
        var inner    = landings.Count(landing => DistanceOf(landing) <= DarkenSky.Radius / 2f) / (float)landings.Count;

        Assert.Multiple(() =>
        {
            Assert.That(inner, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(landings.Max(DistanceOf), Is.GreaterThan(DarkenSky.Radius * 0.95f), "die Streuung reicht bis an den Rand");
        });
    }

    //Mit immer demselben Wurf 0,5 beginnt der erste Sektor bei 180°, jeder Pfeil fällt in die Mitte seines Fünftels, auf halbem Radius
    [Test]
    public void JederPfeil_FaelltInSeinemSektor()
    {
        var landings = DarkenSky.PickLandings(5, new FixedRandom(0.5f));

        Assert.Multiple(() =>
        {
            Assert.That(landings.Select(DegreesOf), Is.EqualTo(new[] { 216f, 288f, 0f, 72f, 144f }).Within(0.01f));
            Assert.That(landings.Select(DistanceOf), Is.All.EqualTo(100f).Within(Tolerance));
        });
    }

    [Test]
    public void FuenfPfeile_FallenInFuenfVerschiedeneFuenftel()
    {
        var landings = DarkenSky.PickLandings(5, new SeededRandom(7)).Select(DegreesOf).OrderBy(angle => angle).ToList();
        var gaps     = landings.Select((angle, i) => (landings[(i + 1) % 5] - angle + 360f) % 360f).ToList();

        Assert.That(gaps, Is.All.LessThan(144f), "zwischen zwei Nachbarn liegt höchstens ein leeres Fünftel, nie zwei");
    }

    [Test]
    public void GleicherSeed_GleicheEinschlaege()
        => Assert.That(DarkenSky.PickLandings(5, new SeededRandom(42)), Is.EqualTo(DarkenSky.PickLandings(5, new SeededRandom(42))));

    [Test]
    public void OhneZufall_WirdGeworfen()
        => Assert.That(() => DarkenSky.PickLandings(5, null), Throws.ArgumentNullException);

    //Für die Schätzung im Tooltip: ein Punkt in der Mitte liegt in so vielen Einschlägen
    [Test]
    public void AnteilAufDieMitte_IstEinschlagDurchRadius()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DarkenSky.GetShareOnCenter(), Is.EqualTo(0.375f).Within(Tolerance));
            Assert.That(new RainSettings(5, 0f, 75f, 0.5f, 1f).GetShareOnCenter(), Is.EqualTo(1f), "ohne Streuung trifft jeder Pfeil");
            Assert.That(new RainSettings(5, 50f, 75f, 0.5f, 1f).GetShareOnCenter(), Is.EqualTo(1f), "ein Einschlag größer als die Streuung trifft immer");
            Assert.That(new RainSettings(5, 200f, 0f, 0.5f, 1f).GetShareOnCenter(), Is.Zero);
        });
    }

    [Test]
    public void EinschlagradiusUndKoerper_ZaehlenWieBeiJederFlaeche()
    {
        var landing = DarkenSky.PickLandings(1, new SeededRandom(3))[0];

        Assert.That(AreaSettings.Contains(landing.X, landing.Y, DarkenSky.ImpactRadius + 30f), Is.EqualTo(DistanceOf(landing) <= DarkenSky.ImpactRadius + 30f + Tolerance));
    }
}
