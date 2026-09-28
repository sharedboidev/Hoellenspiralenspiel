using Hoellenspiralenspiel.Scripts.Core.Navigation;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Navigation;

[TestFixture]
public class RepathTimerTests
{
    private const double Step = 1.0 / 60;

    [Test]
    public void ErsterAufruf_VerlangtSofortEinenPfad()
    {
        var timer = new RepathTimer(0.4, 64f);

        Assert.That(timer.IsDue(Step, 100f, 100f), Is.True);
    }

    [Test]
    public void VorAblaufDesTakts_GibtEsKeinenNeuenPfad()
    {
        var timer = new RepathTimer(0.4, 64f);

        timer.IsDue(Step, 0f, 0f);

        Assert.That(timer.IsDue(0.39, 500f, 500f), Is.False);
    }

    [Test]
    public void NachAblaufDesTakts_GibtEsEinenNeuenPfad_WennDasZielSichBewegtHat()
    {
        var timer = new RepathTimer(0.4, 64f);

        timer.IsDue(Step, 0f, 0f);

        Assert.That(timer.IsDue(0.4, 64f, 0f), Is.True);
    }

    [Test]
    public void StehtDasZiel_GibtEsKeinenNeuenPfad()
    {
        var timer = new RepathTimer(0.4, 64f);
        var paths = 0;

        for (var i = 0; i < 600; i++)
        {
            if (timer.IsDue(Step, 10f, 10f))
                paths++;
        }

        Assert.That(paths, Is.EqualTo(1));
    }

    [Test]
    public void KleineBewegungen_SummierenSichGegenDasLetztePfadziel()
    {
        var timer = new RepathTimer(0.1, 64f);

        timer.IsDue(Step, 0f, 0f);

        var paths = 0;

        for (var i = 1; i <= 100; i++)
        {
            if (timer.IsDue(Step, i * 2f, 0f))
                paths++;
        }

        Assert.That(paths, Is.EqualTo(3), "bei 64, 128 und 192 Pixel Abstand zum letzten Pfadziel");
    }

    [Test]
    public void LaufendesZiel_ErgibtHoechstensEinenPfadProTakt()
    {
        var timer = new RepathTimer(0.4, 10f);
        var paths = 0;

        for (var i = 0; i < 600; i++)
        {
            if (timer.IsDue(Step, i * 5f, 0f))
                paths++;
        }

        Assert.That(paths, Is.InRange(24, 26), "in 10 s passen 25 Takte von 0,4 s");
    }

    [Test]
    public void Versatz_VerschiebtNurDenZweitenPfad()
    {
        var early = new RepathTimer(0.4, 10f);
        var late  = new RepathTimer(0.4, 10f, 0.3);

        Assert.Multiple(() =>
        {
            Assert.That(early.IsDue(Step, 0f, 0f), Is.True);
            Assert.That(late.IsDue(Step, 0f, 0f), Is.True);
        });
    }

    [Test]
    public void NachReset_GibtEsSofortEinenPfad()
    {
        var timer = new RepathTimer(0.4, 64f);

        timer.IsDue(Step, 0f, 0f);
        timer.Reset();

        Assert.That(timer.IsDue(Step, 0f, 0f), Is.True);
    }
}
