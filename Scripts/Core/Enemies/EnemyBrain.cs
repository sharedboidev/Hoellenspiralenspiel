using System;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public enum EnemyState
{
    Idle,
    Chasing,
    Windup,
    Recovery,
    Returning,
    Dead
}

public enum EnemyMovement
{
    None,
    TowardTarget,
    TowardHome
}

public sealed record EnemyBehaviour
{
    public float AggroRange { get; init; } = 500f;

    //So lange folgt der Gegner einem Ziel, das außerhalb von AggroRange bleibt
    public double ChaseTimeSec { get; init; } = 6;
}

public readonly record struct EnemyPerception(bool   IsTargetAlive,
                                              float  DistanceToTarget,
                                              float  EngageRange,
                                              bool   CanAttack,
                                              bool   HasArrivedHome,
                                              double WindupSec,
                                              double RecoverySec);

public readonly record struct EnemyDecision(EnemyMovement Movement,
                                            bool          StartsAttack = false,
                                            bool          Strikes      = false,
                                            bool          EndsAttack   = false,
                                            bool          GivesUp      = false,
                                            bool          Engages      = false);

public sealed class EnemyBrain
{
    private readonly AttackCycle    attackCycle = new();
    private readonly EnemyBehaviour behaviour;
    private          double         secOutOfAggroRange;
    private          bool           wasProvoked;

    public EnemyBrain(EnemyBehaviour behaviour)
    {
        ArgumentNullException.ThrowIfNull(behaviour);

        this.behaviour = behaviour;
    }

    public EnemyState State { get; private set; } = EnemyState.Idle;

    public double AttackSec { get; private set; }

    public bool IsInCombat => State is EnemyState.Chasing or EnemyState.Windup or EnemyState.Recovery;

    //Ein provozierter Gegner ruht nicht mehr, auch wenn er erst beim nächsten Schritt losläuft
    public bool IsResting => State == EnemyState.Dead || (State == EnemyState.Idle && !wasProvoked);

    public void Provoke()
    {
        if (State == EnemyState.Dead)
            return;

        secOutOfAggroRange = 0;

        if (!IsInCombat)
            wasProvoked = true;
    }

    public void Die()
    {
        State = EnemyState.Dead;

        attackCycle.Reset();
    }

    public EnemyDecision Tick(double deltaSec, in EnemyPerception perception)
    {
        var provoked = wasProvoked;

        wasProvoked = false;

        return State switch
        {
            EnemyState.Idle      => Rest(perception, provoked, EnemyMovement.None),
            EnemyState.Returning => perception.HasArrivedHome && !provoked ? Settle(perception) : Rest(perception, provoked, EnemyMovement.TowardHome),
            EnemyState.Chasing   => Chase(deltaSec, perception),
            EnemyState.Windup    => Attack(deltaSec, perception),
            EnemyState.Recovery  => Attack(deltaSec, perception),
            _                    => new EnemyDecision(EnemyMovement.None)
        };
    }

    private EnemyDecision Settle(in EnemyPerception perception)
    {
        State = EnemyState.Idle;

        return Rest(perception, false, EnemyMovement.None);
    }

    private EnemyDecision Rest(in EnemyPerception perception, bool provoked, EnemyMovement movement)
    {
        if (!perception.IsTargetAlive)
            return new EnemyDecision(movement);

        if (!provoked && perception.DistanceToTarget > behaviour.AggroRange)
            return new EnemyDecision(movement);

        State              = EnemyState.Chasing;
        secOutOfAggroRange = 0;

        return new EnemyDecision(EnemyMovement.None, Engages: true);
    }

    private EnemyDecision Chase(double deltaSec, in EnemyPerception perception)
    {
        if (!perception.IsTargetAlive)
            return GiveUp();

        if (perception.DistanceToTarget <= behaviour.AggroRange)
            secOutOfAggroRange = 0;
        else
            secOutOfAggroRange += deltaSec;

        if (secOutOfAggroRange >= behaviour.ChaseTimeSec)
            return GiveUp();

        if (perception.DistanceToTarget > perception.EngageRange)
            return new EnemyDecision(EnemyMovement.TowardTarget);

        if (!perception.CanAttack)
            return new EnemyDecision(EnemyMovement.None);

        attackCycle.Start(perception.WindupSec, perception.RecoverySec);

        State     = EnemyState.Windup;
        AttackSec = Math.Max(0, perception.WindupSec) + Math.Max(0, perception.RecoverySec);

        return new EnemyDecision(EnemyMovement.None, true);
    }

    private EnemyDecision Attack(double deltaSec, in EnemyPerception perception)
    {
        var strikes = attackCycle.Advance(deltaSec) && perception.IsTargetAlive;

        if (!attackCycle.IsReady)
        {
            State = attackCycle.Phase == AttackPhase.Windup ? EnemyState.Windup : EnemyState.Recovery;

            return new EnemyDecision(EnemyMovement.None, Strikes: strikes);
        }

        State = EnemyState.Chasing;

        return new EnemyDecision(EnemyMovement.None, Strikes: strikes, EndsAttack: true);
    }

    private EnemyDecision GiveUp()
    {
        State              = EnemyState.Returning;
        secOutOfAggroRange = 0;

        return new EnemyDecision(EnemyMovement.TowardHome, GivesUp: true);
    }
}
