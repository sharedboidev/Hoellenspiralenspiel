using Hoellenspiralenspiel.Scripts.Core.Hud;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Hud;

[TestFixture]
public class FreeSpanTests
{
    private const float ScreenWidth = 2560f;
    private const float MinWidth    = 400f;

    private static ScreenSpan? Widest(params ScreenSpan[] covered)
        => FreeSpan.Widest(ScreenWidth, covered, MinWidth);

    [Test]
    public void OhneFensterIstDieGanzeBreiteFrei()
        => Assert.That(Widest(), Is.EqualTo(new ScreenSpan(0f, ScreenWidth)));

    [Test]
    public void ZwischenTruheUndBogenLiegtDerBreitesteStreifen()
        => Assert.That(Widest(new ScreenSpan(1612f, 2530f), new ScreenSpan(30f, 946f)), Is.EqualTo(new ScreenSpan(946f, 1612f)));

    [Test]
    public void UeberlappendeTeileDeckenGemeinsamAb()
        => Assert.That(Widest(new ScreenSpan(1168f, 1618f), new ScreenSpan(1598f, 1694f), new ScreenSpan(1612f, 2530f)), Is.EqualTo(new ScreenSpan(0f, 1168f)));

    [Test]
    public void EinTeilInnerhalbEinesAnderenAendertNichts()
        => Assert.That(Widest(new ScreenSpan(1612f, 2530f), new ScreenSpan(1617f, 1657f)), Is.EqualTo(new ScreenSpan(0f, 1612f)));

    [Test]
    public void IstJederStreifenZuSchmalGibtEsKeinen()
        => Assert.That(Widest(new ScreenSpan(30f, 946f), new ScreenSpan(1168f, 2530f)), Is.Null);

    [Test]
    public void GenauDieMindestbreiteReicht()
        => Assert.That(Widest(new ScreenSpan(MinWidth, ScreenWidth)), Is.EqualTo(new ScreenSpan(0f, MinWidth)));

    [Test]
    public void IstAllesVerdecktGibtEsKeinenStreifen()
        => Assert.That(FreeSpan.Widest(ScreenWidth, [new ScreenSpan(0f, ScreenWidth)], 0f), Is.Null);

    [Test]
    public void BeiGleicherBreiteGewinntDerLinkeStreifen()
        => Assert.That(Widest(new ScreenSpan(1000f, 1560f)), Is.EqualTo(new ScreenSpan(0f, 1000f)));

    [Test]
    public void TeileUeberDenRandZaehlenNurInnerhalb()
        => Assert.That(Widest(new ScreenSpan(-50f, 100f), new ScreenSpan(2400f, 2700f)), Is.EqualTo(new ScreenSpan(100f, 2400f)));

    [Test]
    public void TeileAusserhalbDesBildesZaehlenNicht()
        => Assert.That(Widest(new ScreenSpan(3000f, 3200f), new ScreenSpan(-300f, -100f)), Is.EqualTo(new ScreenSpan(0f, ScreenWidth)));

    [Test]
    public void EinTeilOhneBreiteTeiltNicht()
        => Assert.That(Widest(new ScreenSpan(1280f, 1280f)), Is.EqualTo(new ScreenSpan(0f, ScreenWidth)));

    [Test]
    public void EinBreiteresBildGibtMehrPlatz()
        => Assert.That(FreeSpan.Widest(3440f, [new ScreenSpan(30f, 946f), new ScreenSpan(2048f, 3410f)], MinWidth), Is.EqualTo(new ScreenSpan(946f, 2048f)));
}
