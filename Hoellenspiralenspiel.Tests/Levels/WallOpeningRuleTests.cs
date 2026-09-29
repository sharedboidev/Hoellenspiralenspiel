using Hoellenspiralenspiel.Scripts.Core.Levels;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class WallOpeningRuleTests
{
    private const int Outside = WallOpeningRule.NoRoom;
    private const int Yard    = 3;
    private const int Cellar  = 7;

    [Test]
    public void ImRaum_OeffnenSichSeineVorderenMauernGanz()
        => Assert.That(WallOpeningRule.Decide(Outside, Yard, Yard), Is.EqualTo(WallOpeningRule.Open));

    [Test]
    public void VonAussen_BleibenDieVorderenMauernEinesRaumsZu()
    {
        Assert.That(WallOpeningRule.Decide(Outside, Yard, Outside), Is.EqualTo(WallOpeningRule.Closed));
        Assert.That(WallOpeningRule.Decide(Outside, Yard, Cellar), Is.EqualTo(WallOpeningRule.Closed));
    }

    [Test]
    public void HinterEinemFremdenRaum_OeffnenSichSeineHinterenMauernHalb()
    {
        Assert.That(WallOpeningRule.Decide(Yard, Outside, Outside), Is.EqualTo(WallOpeningRule.Half));
        Assert.That(WallOpeningRule.Decide(Yard, Cellar, Cellar), Is.EqualTo(WallOpeningRule.Half));
    }

    [Test]
    public void OhneRaum_OeffnetSichDieMauerGanz()
        => Assert.That(WallOpeningRule.Decide(Outside, Outside, Outside), Is.EqualTo(WallOpeningRule.Open));

    [Test]
    public void EineMauerImRaum_OeffnetSichNurFuerDenDerDrinSteht()
    {
        Assert.That(WallOpeningRule.Decide(Yard, Yard, Yard), Is.EqualTo(WallOpeningRule.Open));
        Assert.That(WallOpeningRule.Decide(Yard, Yard, Outside), Is.EqualTo(WallOpeningRule.Closed));
    }

    [Test]
    public void BeideSeitenEinerMauer_FolgenDemStandortDesHelden()
    {
        var fromOutside = WallOpeningRule.DecideForBothSides(Yard, Outside, Outside);
        var fromInside  = WallOpeningRule.DecideForBothSides(Yard, Outside, Yard);

        Assert.That(fromOutside, Is.EqualTo(new WallOpening(WallOpeningRule.Closed, WallOpeningRule.Half)));
        Assert.That(fromInside, Is.EqualTo(new WallOpening(WallOpeningRule.Open, WallOpeningRule.Open)));
    }
}
