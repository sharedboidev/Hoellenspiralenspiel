using Hoellenspiralenspiel.Scripts.Core.Enemies;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

[TestFixture]
public class EnemyBrainTests
{
    private const double Step = 1.0 / 60;

    private static readonly EnemyBehaviour Behaviour = new() { AggroRange = 500f, ChaseTimeSec = 3 };

    private static EnemyPerception See(float  distance,
                                       bool   alive     = true,
                                       float  engage    = 80f,
                                       bool   canAttack = true,
                                       bool   atHome    = false,
                                       double windup    = 0.5,
                                       double recovery  = 0.7)
        => new(alive, distance, engage, canAttack, atHome, windup, recovery);

    private static EnemyBrain ChasingBrain(float distance = 300f)
    {
        var brain = new EnemyBrain(Behaviour);

        brain.Tick(Step, See(distance));

        return brain;
    }

    private static EnemyDecision TickFor(EnemyBrain brain, double seconds, EnemyPerception perception)
    {
        var decision = default(EnemyDecision);

        for (var elapsed = 0.0; elapsed < seconds - Step / 2; elapsed += Step)
            decision = brain.Tick(Step, perception);

        return decision;
    }

    #region Ruhe und Aufwachen

    [Test]
    public void NeuerGegner_Ruht()
    {
        var brain = new EnemyBrain(Behaviour);

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(brain.IsInCombat, Is.False);
            Assert.That(brain.IsResting, Is.True);
        });
    }

    [Test]
    public void ZielAusserhalbDesAggroradius_LaesstIhnRuhen()
    {
        var brain = new EnemyBrain(Behaviour);

        var decision = brain.Tick(Step, See(501f));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.None));
            Assert.That(decision.Engages, Is.False);
        });
    }

    [Test]
    public void ZielImAggroradius_WecktIhn()
    {
        var brain = new EnemyBrain(Behaviour);

        var decision = brain.Tick(Step, See(500f));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
            Assert.That(decision.Engages, Is.True);
        });
    }

    [Test]
    public void TotesZiel_WecktIhnNicht()
    {
        var brain = new EnemyBrain(Behaviour);

        brain.Tick(Step, See(10f, false));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void Provozieren_WecktIhnAuchAusDerFerne()
    {
        var brain = new EnemyBrain(Behaviour);

        brain.Provoke();

        var decision = brain.Tick(Step, See(2000f));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
            Assert.That(decision.Engages, Is.True);
        });
    }

    [Test]
    public void Provozieren_BeendetDieRuheSofort()
    {
        var brain = new EnemyBrain(Behaviour);

        brain.Provoke();

        Assert.Multiple(() =>
        {
            Assert.That(brain.IsResting, Is.False, "sonst bliebe ein schlafender Gegner liegen, weil nur wache Gegner denken");
            Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
        });
    }

    [Test]
    public void Provozieren_GiltNurFuerDenNaechstenSchritt()
    {
        var brain = new EnemyBrain(Behaviour);

        brain.Provoke();
        brain.Tick(Step, See(2000f, false));
        brain.Tick(Step, See(2000f));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
    }

    #endregion

    #region Verfolgen

    [Test]
    public void ZielAusserReichweite_WirdVerfolgt()
    {
        var brain = ChasingBrain();

        var decision = brain.Tick(Step, See(300f));

        Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.TowardTarget));
    }

    [Test]
    public void ZielInReichweite_WirdAngegriffen()
    {
        var brain = ChasingBrain();

        var decision = brain.Tick(Step, See(79f));

        Assert.Multiple(() =>
        {
            Assert.That(decision.StartsAttack, Is.True);
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.None));
            Assert.That(brain.State, Is.EqualTo(EnemyState.Windup));
        });
    }

    [Test]
    public void Angriff_MeldetSeineGesamteDauer()
    {
        var brain = ChasingBrain();

        brain.Tick(Step, See(79f, windup: 0.25, recovery: 0.35));

        Assert.That(brain.AttackSec, Is.EqualTo(0.6).Within(0.0001));
    }

    [Test]
    public void OhneBereitenSkill_WartetErInReichweite()
    {
        var brain = ChasingBrain();

        var decision = brain.Tick(Step, See(79f, canAttack: false));

        Assert.Multiple(() =>
        {
            Assert.That(decision.StartsAttack, Is.False);
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.None));
            Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
        });
    }

    [Test]
    public void OhneSicht_LaeuftErTrotzNaeheWeiter()
    {
        var brain = ChasingBrain();

        var decision = brain.Tick(Step, See(79f, engage: 0f));

        Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.TowardTarget));
    }

    #endregion

    #region Angriff

    [Test]
    public void Angriff_TrifftGenauEinmalNachDemAusholen()
    {
        var brain   = ChasingBrain();
        var strikes = 0;

        brain.Tick(Step, See(50f));

        for (var i = 0; i < 29; i++)
        {
            if (brain.Tick(Step, See(50f)).Strikes)
                strikes++;
        }

        Assert.That(strikes, Is.Zero, "nach 0,48 s wird noch ausgeholt");

        for (var i = 0; i < 10; i++)
        {
            if (brain.Tick(Step, See(50f)).Strikes)
                strikes++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(strikes, Is.EqualTo(1));
            Assert.That(brain.State, Is.EqualTo(EnemyState.Recovery));
        });
    }

    [Test]
    public void Angriff_EndetNachDemErholen()
    {
        var brain = ChasingBrain();
        var ends  = 0;

        brain.Tick(Step, See(50f));

        for (var i = 0; i < 80; i++)
        {
            if (brain.Tick(Step, See(50f, canAttack: false)).EndsAttack)
                ends++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(ends, Is.EqualTo(1));
            Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
        });
    }

    [Test]
    public void WaehrendDesAngriffs_BleibtErStehen()
    {
        var brain = ChasingBrain();

        brain.Tick(Step, See(50f));

        var decision = brain.Tick(Step, See(900f));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.None));
            Assert.That(brain.State, Is.EqualTo(EnemyState.Windup));
        });
    }

    [Test]
    public void StirbtDasZielBeimAusholen_FaelltKeinTreffer()
    {
        var brain   = ChasingBrain();
        var strikes = 0;

        brain.Tick(Step, See(50f));

        for (var i = 0; i < 100; i++)
        {
            if (brain.Tick(Step, See(50f, false)).Strikes)
                strikes++;
        }

        Assert.That(strikes, Is.Zero);
    }

    [Test]
    public void NachDemAngriff_FolgtDerNaechste()
    {
        var brain  = ChasingBrain();
        var starts = 0;

        for (var i = 0; i < 60 * 5; i++)
        {
            if (brain.Tick(Step, See(50f)).StartsAttack)
                starts++;
        }

        Assert.That(starts, Is.EqualTo(5), "ein Angriff dauert 1,2 s, die Angriffe beginnen nach 0, 1,2, 2,4, 3,6 und 4,8 s");
    }

    #endregion

    #region Aufgeben und Rückweg

    [Test]
    public void ZielImAggroradius_WirdEndlosVerfolgt()
    {
        var brain = ChasingBrain();

        TickFor(brain, 30, See(400f));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
    }

    [Test]
    public void ZielAusserhalb_WirdNachDerVerfolgungszeitAufgegeben()
    {
        var brain = ChasingBrain();

        TickFor(brain, 2.9, See(800f));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));

        var decision = TickFor(brain, 0.2, See(800f));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Returning));
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.TowardHome));
        });
    }

    [Test]
    public void Aufgeben_WirdGenauEinmalGemeldet()
    {
        var brain   = ChasingBrain();
        var giveUps = 0;

        for (var i = 0; i < 60 * 6; i++)
        {
            if (brain.Tick(Step, See(800f)).GivesUp)
                giveUps++;
        }

        Assert.That(giveUps, Is.EqualTo(1));
    }

    [Test]
    public void KommtDasZielZurueckInDenAggroradius_BeginntDieZeitNeu()
    {
        var brain = ChasingBrain();

        TickFor(brain, 2.5, See(800f));
        TickFor(brain, 0.1, See(400f));
        TickFor(brain, 2.5, See(800f));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
    }

    [Test]
    public void EinTreffer_SetztDieVerfolgungszeitZurueck()
    {
        var brain = ChasingBrain();

        TickFor(brain, 2.5, See(800f));

        brain.Provoke();

        TickFor(brain, 2.5, See(800f));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
    }

    [Test]
    public void StirbtDasZiel_GibtErSofortAuf()
    {
        var brain = ChasingBrain();

        var decision = brain.Tick(Step, See(100f, false));

        Assert.Multiple(() =>
        {
            Assert.That(decision.GivesUp, Is.True);
            Assert.That(brain.State, Is.EqualTo(EnemyState.Returning));
        });
    }

    [Test]
    public void AufDemRueckweg_LaeuftErNachHause()
    {
        var brain = ChasingBrain();

        TickFor(brain, 4, See(800f));

        var decision = brain.Tick(Step, See(800f));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.TowardHome));
            Assert.That(brain.IsInCombat, Is.False);
            Assert.That(brain.IsResting, Is.False);
        });
    }

    [Test]
    public void ZuHauseAngekommen_RuhtEr()
    {
        var brain = ChasingBrain();

        TickFor(brain, 4, See(800f));

        var decision = brain.Tick(Step, See(800f, atHome: true));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(decision.Movement, Is.EqualTo(EnemyMovement.None));
        });
    }

    [Test]
    public void AufDemRueckweg_WecktIhnEinZielImAggroradius()
    {
        var brain = ChasingBrain();

        TickFor(brain, 4, See(800f));

        var decision = brain.Tick(Step, See(300f));

        Assert.Multiple(() =>
        {
            Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
            Assert.That(decision.Engages, Is.True);
        });
    }

    [Test]
    public void AufDemRueckweg_WecktIhnEinTreffer()
    {
        var brain = ChasingBrain();

        TickFor(brain, 4, See(800f));

        brain.Provoke();
        brain.Tick(Step, See(800f, atHome: true));

        Assert.That(brain.State, Is.EqualTo(EnemyState.Chasing));
    }

    #endregion

    #region Tod

    [Test]
    public void ToterGegner_TutNichtsMehr()
    {
        var brain = ChasingBrain();

        brain.Tick(Step, See(50f));
        brain.Die();
        brain.Provoke();

        var strikes = 0;

        for (var i = 0; i < 120; i++)
        {
            var decision = brain.Tick(Step, See(50f));

            if (decision.Strikes || decision.StartsAttack || decision.Movement != EnemyMovement.None)
                strikes++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(strikes, Is.Zero);
            Assert.That(brain.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(brain.IsResting, Is.True);
        });
    }

    #endregion
}
