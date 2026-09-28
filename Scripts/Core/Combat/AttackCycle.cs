using System;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public enum AttackPhase
{
    Ready,
    Windup,
    Recovery
}

//Der Takt eines Angriffs: Ausholen, Treffer, Erholen. Spieler und Gegner benutzen denselben Ablauf
public sealed class AttackCycle
{
    private double recoverySec;

    public AttackPhase Phase       { get; private set; } = AttackPhase.Ready;
    public double      TimeLeftSec { get; private set; }
    public bool        IsReady     => Phase == AttackPhase.Ready;

    //Liefert false, wenn noch ein Angriff läuft
    public bool Start(double windupSec, double recoverySec)
    {
        if (!IsReady)
            return false;

        this.recoverySec = Math.Max(0, recoverySec);
        Phase            = AttackPhase.Windup;
        TimeLeftSec      = Math.Max(0, windupSec);

        return true;
    }

    //Liefert true genau in dem Schritt, in dem das Ausholen endet und der Treffer fällt
    public bool Advance(double deltaSec)
    {
        var hasImpact = false;

        while (Phase != AttackPhase.Ready)
        {
            if (deltaSec < TimeLeftSec)
            {
                TimeLeftSec -= deltaSec;

                break;
            }

            deltaSec -= TimeLeftSec;

            if (Phase == AttackPhase.Windup)
            {
                hasImpact   = true;
                Phase       = AttackPhase.Recovery;
                TimeLeftSec = recoverySec;
            }
            else
            {
                Phase       = AttackPhase.Ready;
                TimeLeftSec = 0;
            }
        }

        return hasImpact;
    }

    //Bricht das Ausholen ab, es fällt kein Treffer. Nach dem Treffer läuft das Erholen weiter,
    //damit ein Abbruch den nächsten Angriff nicht beschleunigt
    public void CancelWindup()
    {
        if (Phase != AttackPhase.Windup)
            return;

        Phase       = AttackPhase.Ready;
        TimeLeftSec = 0;
    }

    public void Reset()
    {
        Phase       = AttackPhase.Ready;
        TimeLeftSec = 0;
    }
}
