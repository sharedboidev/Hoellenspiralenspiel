using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class LobArcTests
{
    private const float Start     = 1f;
    private const float Peak      = 2.7f;
    private const float Tolerance = 0.0001f;

    [Test]
    public void Bogen_BeginntAmStart_UndEndetAufDemBoden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LobArc.HeightAt(0f, Start, Peak), Is.EqualTo(Start).Within(Tolerance));
            Assert.That(LobArc.HeightAt(1f, Start, Peak), Is.EqualTo(0f).Within(Tolerance));
        });
    }

    [Test]
    public void Bogen_ErreichtDieSpitze_UndUeberschreitetSieNie()
    {
        var top     = LobArc.GetTopProgress(Start, Peak);
        var samples = Enumerable.Range(0, 101).Select(i => LobArc.HeightAt(i / 100f, Start, Peak)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(LobArc.HeightAt(top, Start, Peak), Is.EqualTo(Peak).Within(Tolerance));
            Assert.That(samples.Max(), Is.LessThanOrEqualTo(Peak + Tolerance));
            Assert.That(samples.Min(), Is.GreaterThanOrEqualTo(0f));
        });
    }

    [Test]
    public void VomBoden_IstDerBogenSymmetrisch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LobArc.GetTopProgress(0f, Peak), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(LobArc.HeightAt(0.2f, 0f, Peak), Is.EqualTo(LobArc.HeightAt(0.8f, 0f, Peak)).Within(Tolerance));
        });
    }

    [Test]
    public void AusDerHoehe_SteigtErKuerzer_AlsErFaellt()
        => Assert.That(LobArc.GetTopProgress(Start, Peak), Is.LessThan(0.5f));

    //Die zweite Ableitung ist auf beiden Seiten der Spitze dieselbe
    [Test]
    public void Schwerkraft_IstAufDemGanzenWegGleich()
    {
        const float step = 0.01f;

        static float Curvature(float t)
            => (LobArc.HeightAt(t + step, Start, Peak) - 2f * LobArc.HeightAt(t, Start, Peak) + LobArc.HeightAt(t - step, Start, Peak)) / (step * step);

        Assert.That(Curvature(0.2f), Is.EqualTo(Curvature(0.8f)).Within(0.05f));
    }

    [Test]
    public void StartUeberDerSpitze_FaelltNurNoch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LobArc.GetTopProgress(3f, Peak), Is.Zero);
            Assert.That(LobArc.HeightAt(0f, 3f, Peak), Is.EqualTo(3f).Within(Tolerance));
            Assert.That(LobArc.HeightAt(0.5f, 3f, Peak), Is.LessThan(3f));
            Assert.That(LobArc.HeightAt(1f, 3f, Peak), Is.EqualTo(0f).Within(Tolerance));
        });
    }

    [Test]
    public void FortschrittAusserhalbDesFlugs_BleibtAnSeinenEnden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LobArc.HeightAt(-0.5f, Start, Peak), Is.EqualTo(Start).Within(Tolerance));
            Assert.That(LobArc.HeightAt(1.5f, Start, Peak), Is.EqualTo(0f).Within(Tolerance));
        });
    }
}
