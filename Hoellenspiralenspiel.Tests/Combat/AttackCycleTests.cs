using Hoellenspiralenspiel.Scripts.Core.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class AttackCycleTests
{
    [Test]
    public void NeuerTakt_IstBereit()
    {
        var cycle = new AttackCycle();

        Assert.Multiple(() =>
        {
            Assert.That(cycle.IsReady, Is.True);
            Assert.That(cycle.Advance(1), Is.False, "ohne Angriff fällt kein Treffer");
        });
    }

    [Test]
    public void Treffer_FaelltGenauEinmalAmEndeDesAusholens()
    {
        var cycle   = new AttackCycle();
        var impacts = 0;

        cycle.Start(0.3, 0.2);

        for (var i = 0; i < 60; i++)
        {
            if (cycle.Advance(0.01))
                impacts++;

            if (i == 28)
                Assert.That(impacts, Is.Zero, "nach 0,29 s wird noch ausgeholt");
        }

        Assert.Multiple(() =>
        {
            Assert.That(impacts, Is.EqualTo(1));
            Assert.That(cycle.IsReady, Is.True, "nach 0,6 s sind Ausholen und Erholen vorbei");
        });
    }

    [Test]
    public void Phasen_FolgenAufeinander()
    {
        var cycle = new AttackCycle();

        cycle.Start(0.3, 0.2);

        Assert.That(cycle.Phase, Is.EqualTo(AttackPhase.Windup));

        cycle.Advance(0.3);

        Assert.That(cycle.Phase, Is.EqualTo(AttackPhase.Recovery));

        cycle.Advance(0.1);

        Assert.That(cycle.Phase, Is.EqualTo(AttackPhase.Recovery));

        cycle.Advance(0.1);

        Assert.That(cycle.Phase, Is.EqualTo(AttackPhase.Ready));
    }

    [Test]
    public void GrosserSchritt_DurchlaeuftDenGanzenAngriff()
    {
        var cycle = new AttackCycle();

        cycle.Start(0.3, 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Advance(5), Is.True);
            Assert.That(cycle.IsReady, Is.True);
        });
    }

    [Test]
    public void Start_WaehrendEinAngriffLaeuftWirdAbgelehnt()
    {
        var cycle = new AttackCycle();

        Assert.That(cycle.Start(0.3, 0.2), Is.True);
        Assert.That(cycle.Start(0.3, 0.2), Is.False);

        cycle.Advance(0.3);

        Assert.That(cycle.Start(0.3, 0.2), Is.False, "auch im Erholen");

        cycle.Advance(0.2);

        Assert.That(cycle.Start(0.3, 0.2), Is.True);
    }

    [Test]
    public void Abbruch_ImAusholenVerhindertDenTreffer()
    {
        var cycle = new AttackCycle();

        cycle.Start(0.3, 0.2);
        cycle.Advance(0.2);
        cycle.CancelWindup();

        Assert.Multiple(() =>
        {
            Assert.That(cycle.IsReady, Is.True);
            Assert.That(cycle.Advance(1), Is.False);
        });
    }

    [Test]
    public void Abbruch_NachDemTrefferVerkuerztDasErholenNicht()
    {
        var cycle = new AttackCycle();

        cycle.Start(0.3, 0.2);
        cycle.Advance(0.3);
        cycle.CancelWindup();

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Phase, Is.EqualTo(AttackPhase.Recovery));
            Assert.That(cycle.TimeLeftSec, Is.EqualTo(0.2).Within(0.0001));
        });
    }

    [Test]
    public void Reset_BeendetDenAngriffSofort()
    {
        var cycle = new AttackCycle();

        cycle.Start(0.3, 0.2);
        cycle.Advance(0.3);
        cycle.Reset();

        Assert.That(cycle.IsReady, Is.True);
    }
}
