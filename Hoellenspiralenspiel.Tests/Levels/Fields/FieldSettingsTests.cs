using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

//So baut der Abstieg die Einstellungen einer Fläche aus seinen Feldern und dem Kreis
[TestFixture]
public class FieldSettingsTests
{
    private static readonly FieldSettings FromDescent = new() { Width = 36, Height = 26, ObstacleShare = 0.1f, CellsPerFieldPack = 35 };

    [TestCase(1, 5, false)]
    [TestCase(2, 6, false)]
    [TestCase(3, 7, true)]
    public void JedeFlaecheLiegtEinBereichslevelHoeher_DieLetzteBekommtDieArena(int depth, int areaLevel, bool isLast)
    {
        var settings = FromDescent.ForDepth(5, depth, 3);

        Assert.Multiple(() =>
        {
            Assert.That(settings.AreaLevel, Is.EqualTo(areaLevel));
            Assert.That(settings.IsLastField, Is.EqualTo(isLast));
        });
    }

    [Test]
    public void DieFelderDesAbstiegsBleiben()
        => Assert.That(FromDescent.ForDepth(5, 2, 3) with { AreaLevel = FromDescent.AreaLevel }, Is.EqualTo(FromDescent));

    [Test]
    public void EinKreisMitEinerFlaeche_HatNurDieLetzte()
        => Assert.That(FromDescent.ForDepth(1, 1, 1).IsLastField, Is.True);
}
