using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class WallFadeRuleTests
{
    private const float Radius     = 12f;
    private const float HeroHeight = 1.8f;
    private const float Masonry    = 1.5f;

    //Die Mauer läuft längs X durch den Ursprung, die Normale zeigt nach Süden
    private static readonly WallPlane Wall = new(0f, 0f, 0f, 1f);

    private static readonly WallOpening Closed        = new(WallOpeningRule.Closed, WallOpeningRule.Closed);
    private static readonly WallOpening HalfFromNorth = new(WallOpeningRule.Closed, WallOpeningRule.Half);

    //Die Kamera steht im Südosten und blickt mit 30 Grad nach unten, wie im Spiel
    private static WallFadeView HeroAt(float x, float z, float radius = Radius)
        => new(new WorldPoint(x, 0f, z), HeroHeight, new WorldPoint(x + 17.4f, 14.2f, z + 17.4f), radius);

    private static WorldPoint OnWall(float x, float height = Masonry)
        => new(x, height, 0f);

    private static float OpacityAt(float x, WallFadeView view)
        => WallFadeRule.GetOpacity(OnWall(x), Wall, WallOpening.Open, view);

    [Test]
    public void StehtDerHeldVorDerMauer_BleibtSieWieSieIst()
    {
        Assert.That(OpacityAt(0f, HeroAt(0f, 2f)), Is.EqualTo(1f));
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(0f), Wall, WallOpening.Open, HeroAt(0f, 2f)), Is.False);
    }

    [Test]
    public void StehtDerHeldHinterDerMauer_WirdSieDurchsichtig()
    {
        Assert.That(OpacityAt(0f, HeroAt(0f, -2f)), Is.EqualTo(0f));
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(3f), Wall, WallOpening.Open, HeroAt(0f, -2f)), Is.True);
    }

    [Test]
    public void DerDurchsichtigeBereichReichtSoWeitWieDerLichtradius()
    {
        var view = HeroAt(0f, -2f);

        Assert.That(WallFadeRule.IsSeeThrough(OnWall(9f), Wall, WallOpening.Open, view), Is.True);
        Assert.That(OpacityAt(12.5f, view), Is.EqualTo(1f));
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(-12.5f), Wall, WallOpening.Open, view), Is.False);
    }

    [Test]
    public void EinGroessererLichtradius_OeffnetMehrMauer()
    {
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(15f), Wall, WallOpening.Open, HeroAt(0f, -2f)), Is.False);
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(15f), Wall, WallOpening.Open, HeroAt(0f, -2f, 20f)), Is.True);
    }

    [Test]
    public void AmRandGehtDieMauerAllmaehlichZu()
    {
        var view   = HeroAt(0f, -2f);
        var inner  = OpacityAt(10.6f, view);
        var middle = OpacityAt(11.2f, view);
        var outer  = OpacityAt(11.8f, view);

        Assert.That(inner, Is.GreaterThan(0f).And.LessThan(middle));
        Assert.That(middle, Is.LessThan(outer));
        Assert.That(outer, Is.LessThan(1f));
    }

    [Test]
    public void BeimDurchschreitenDerTuerSpringtDieMauerNicht()
    {
        var atPlane = OpacityAt(6f, HeroAt(0f, -0.01f));
        var close   = OpacityAt(6f, HeroAt(0f, -0.25f));
        var behind  = OpacityAt(6f, HeroAt(0f, -0.5f));

        Assert.That(atPlane, Is.GreaterThan(0.99f));
        Assert.That(close, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(behind, Is.EqualTo(0f));
    }

    [Test]
    public void DieRichtungDerNormaleIstEgal()
    {
        var flipped = Wall with { NormalZ = -1f };

        Assert.That(WallFadeRule.GetOpacity(OnWall(0f), flipped, WallOpening.Open, HeroAt(0f, -2f)), Is.EqualTo(0f));
        Assert.That(WallFadeRule.GetOpacity(OnWall(6f), flipped, WallOpening.Open, HeroAt(0f, 2f)), Is.EqualTo(1f));
    }

    [Test]
    public void EineMauerLaengsZ_OeffnetSichWennDerHeldWestlichSteht()
    {
        var wall  = new WallPlane(0f, 0f, 1f, 0f);
        var point = new WorldPoint(0f, Masonry, 4f);

        Assert.That(WallFadeRule.IsSeeThrough(point, wall, WallOpening.Open, HeroAt(-2f, 0f)), Is.True);
        Assert.That(WallFadeRule.IsSeeThrough(point, wall, WallOpening.Open, HeroAt(2f, 0f)), Is.False);
    }

    [Test]
    public void OhneLichtradius_BleibtDieMauerAbseitsDesHeldenZu()
        => Assert.That(OpacityAt(6f, HeroAt(0f, -2f, 0f)), Is.EqualTo(1f));

    [Test]
    public void EineVerschlosseneMauer_BleibtAuchZuWennDerHeldHinterIhrSteht()
    {
        Assert.That(WallFadeRule.GetOpacity(OnWall(6f), Wall, Closed, HeroAt(0f, -2f)), Is.EqualTo(1f));
        Assert.That(WallFadeRule.GetOpacity(OnWall(-4f), Wall, Closed, HeroAt(0f, -2f)), Is.EqualTo(1f));
    }

    [Test]
    public void EineHalbOffeneMauer_IstHalbDurchsichtigUndLaesstKlicksDurch()
    {
        var view = HeroAt(0f, -2f);

        Assert.That(WallFadeRule.GetOpacity(OnWall(6f), Wall, HalfFromNorth, view), Is.EqualTo(0.5f));
        Assert.That(WallFadeRule.IsSeeThrough(OnWall(6f), Wall, HalfFromNorth, view), Is.True);
        Assert.That(WallFadeRule.GetOpacity(OnWall(12.5f), Wall, HalfFromNorth, view), Is.EqualTo(1f));
    }

    [Test]
    public void DieOeffnungHaengtDavonAbAufWelcherSeiteDerHeldSteht()
    {
        var flipped = new WallPlane(0f, 0f, 0f, -1f);
        var opening = new WallOpening(WallOpeningRule.Half, WallOpeningRule.Open);

        Assert.That(WallFadeRule.GetOpacity(OnWall(6f), flipped, opening, HeroAt(0f, -2f)), Is.EqualTo(0.5f));
        Assert.That(WallFadeRule.GetOpacity(OnWall(6f), Wall, opening, HeroAt(0f, -2f)), Is.EqualTo(0f));
    }

    //Die Sichtlinie von der Kamera zum Helden quert die Mauer einen Meter östlich von ihm
    [Test]
    public void WoDieMauerDenHeldenVerdeckt_BleibtSieNieGanzZu()
    {
        var view = HeroAt(0f, -1f);

        Assert.That(WallFadeRule.GetOpacity(new WorldPoint(1f, 1.3f, 0f), Wall, Closed, view), Is.EqualTo(0.5f));
        Assert.That(WallFadeRule.IsSeeThrough(new WorldPoint(1f, 1.3f, 0f), Wall, Closed, view), Is.True);
    }

    [Test]
    public void AbseitsDerSichtlinieZumHelden_BleibtEineVerschlosseneMauerZu()
    {
        var view = HeroAt(0f, -1f);

        Assert.That(WallFadeRule.GetOpacity(new WorldPoint(3.5f, 1.3f, 0f), Wall, Closed, view), Is.EqualTo(1f));
        Assert.That(WallFadeRule.GetOpacity(new WorldPoint(-1.5f, 1.3f, 0f), Wall, Closed, view), Is.EqualTo(1f));
    }

    [Test]
    public void EineMauerHinterDemHelden_VerdecktIhnNicht()
        => Assert.That(WallFadeRule.GetOpacity(new WorldPoint(-0.4f, 1.3f, 0f), Wall, Closed, HeroAt(0f, 0.6f)), Is.EqualTo(1f));

    [Test]
    public void AmRandDerSichtlinieGehtDieMauerAllmaehlichZu()
    {
        var view  = HeroAt(0f, -1f);
        var inner = WallFadeRule.GetOpacity(new WorldPoint(1.95f, 1.3f, 0f), Wall, Closed, view);
        var outer = WallFadeRule.GetOpacity(new WorldPoint(2.15f, 1.3f, 0f), Wall, Closed, view);

        Assert.That(inner, Is.GreaterThan(0.5f).And.LessThan(outer));
        Assert.That(outer, Is.LessThan(1f));
    }

    [Test]
    public void OhneLichtradius_BleibtDerHeldTrotzdemZuSehen()
        => Assert.That(WallFadeRule.GetOpacity(new WorldPoint(1f, 1.3f, 0f), Wall, WallOpening.Open, HeroAt(0f, -1f, 0f)), Is.EqualTo(0.5f));
}
