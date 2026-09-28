using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

[TestFixture]
public class ModTriggerTimersTests
{
    #region RepeatingTimer

    [Test]
    public void Takt_LoestNachJederPeriodeAus()
    {
        var timer   = new RepeatingTimer(1.5);
        var firings = 0;

        for (var i = 0; i < 600; i++)
        {
            if (timer.Advance(0.01))
                firings++;
        }

        Assert.That(firings, Is.EqualTo(4), "in 6 s passen vier Perioden von 1,5 s");
    }

    [Test]
    public void Takt_LoestVorDerErstenPeriodeNichtAus()
    {
        var timer = new RepeatingTimer(2);

        Assert.That(timer.Advance(1.99), Is.False);
    }

    [Test]
    public void Takt_MitVersatz_LoestFrueherAus()
    {
        var timer = new RepeatingTimer(2, 1.5);

        Assert.Multiple(() =>
        {
            Assert.That(timer.Advance(0.4), Is.False);
            Assert.That(timer.Advance(0.2), Is.True);
        });
    }

    [Test]
    public void Takt_LoestProAufrufHoechstensEinmalAus()
    {
        var timer = new RepeatingTimer(1);

        Assert.Multiple(() =>
        {
            Assert.That(timer.Advance(10), Is.True);
            Assert.That(timer.Advance(0), Is.True, "eine aufgestaute Periode bleibt stehen");
            Assert.That(timer.Advance(0), Is.False);
        });
    }

    [Test]
    public void Takt_NachReset_BeginntVonVorn()
    {
        var timer = new RepeatingTimer(1);

        timer.Advance(0.9);
        timer.Reset();

        Assert.That(timer.Advance(0.9), Is.False);
    }

    [Test]
    public void Takt_MitWinzigerPeriode_BleibtEndlich()
    {
        var timer   = new RepeatingTimer(0);
        var firings = 0;

        for (var i = 0; i < 60; i++)
        {
            if (timer.Advance(1.0 / 60))
                firings++;
        }

        Assert.That(firings, Is.LessThanOrEqualTo(21));
    }

    #endregion

    #region ThresholdLatch

    [Test]
    public void Schwelle_LoestBeimUnterschreitenEinmalAus()
    {
        var latch = new ThresholdLatch(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(latch.Update(0.8f), Is.False);
            Assert.That(latch.Update(0.5f), Is.True);
            Assert.That(latch.Update(0.3f), Is.False);
            Assert.That(latch.Update(0.9f), Is.False);
            Assert.That(latch.Update(0.2f), Is.False, "ohne Nachladen bleibt es bei einem Mal");
        });
    }

    [Test]
    public void Schwelle_MitNachladen_LoestErneutAus()
    {
        var latch = new ThresholdLatch(0.5f, true);

        Assert.Multiple(() =>
        {
            Assert.That(latch.Update(0.4f), Is.True);
            Assert.That(latch.Update(0.3f), Is.False);
            Assert.That(latch.Update(0.9f), Is.False);
            Assert.That(latch.Update(0.4f), Is.True);
        });
    }

    #endregion

    #region TriggerGate

    [Test]
    public void Tor_OhneEinschraenkung_LaesstAllesDurchUndWuerfeltNicht()
    {
        var gate   = new TriggerGate();
        var random = new FixedRandom(0.99f);

        Assert.Multiple(() =>
        {
            Assert.That(gate.TryPass(random), Is.True);
            Assert.That(gate.TryPass(random), Is.True);
            Assert.That(random.Draws, Is.Zero);
        });
    }

    [Test]
    public void Tor_MitChance_FolgtDemWurf()
    {
        var gate = new TriggerGate(25f);

        Assert.Multiple(() =>
        {
            Assert.That(gate.TryPass(new FixedRandom(0.24f)), Is.True);
            Assert.That(gate.TryPass(new FixedRandom(0.25f)), Is.False);
        });
    }

    [Test]
    public void Tor_MitAbklingzeit_SperrtBisSieVorbeiIst()
    {
        var gate   = new TriggerGate(100f, 2);
        var random = new FixedRandom(0f);

        Assert.That(gate.TryPass(random), Is.True);

        gate.Advance(1.9);

        Assert.That(gate.TryPass(random), Is.False);

        gate.Advance(0.1);

        Assert.That(gate.TryPass(random), Is.True);
    }

    [Test]
    public void Tor_EinFehlwurf_StartetKeineAbklingzeit()
    {
        var gate = new TriggerGate(50f, 5);

        Assert.Multiple(() =>
        {
            Assert.That(gate.TryPass(new FixedRandom(0.9f)), Is.False);
            Assert.That(gate.TryPass(new FixedRandom(0.1f)), Is.True);
        });
    }

    [Test]
    public void Tor_WaehrendDerAbklingzeit_WirdNichtGewuerfelt()
    {
        var gate   = new TriggerGate(50f, 5);
        var random = new FixedRandom(0.1f);

        gate.TryPass(random);
        gate.TryPass(random);

        Assert.That(random.Draws, Is.EqualTo(1));
    }

    #endregion
}
