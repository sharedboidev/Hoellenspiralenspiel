using System;

namespace Hoellenspiralenspiel.Scripts.Core.Navigation;

//Ein neuer Pfad ist teuer. Er wird nur in festem Takt gesucht und nur, wenn sich das Ziel merklich bewegt hat
public sealed class RepathTimer
{
    private readonly double intervalSec;
    private readonly float  minTargetShiftSquared;
    private          double elapsedSec;
    private          bool   hasTarget;
    private          float  lastTargetX;
    private          float  lastTargetY;

    public RepathTimer(double intervalSec, float minTargetShift, double startOffsetSec = 0)
    {
        this.intervalSec      = Math.Max(0, intervalSec);
        minTargetShiftSquared = minTargetShift * minTargetShift;
        elapsedSec            = startOffsetSec;
    }

    public bool IsDue(double deltaSec, float targetX, float targetY)
    {
        elapsedSec += Math.Max(0, deltaSec);

        if (!hasTarget)
            return Accept(targetX, targetY);

        if (elapsedSec < intervalSec)
            return false;

        var shiftX = targetX - lastTargetX;
        var shiftY = targetY - lastTargetY;

        if (shiftX * shiftX + shiftY * shiftY < minTargetShiftSquared)
            return false;

        return Accept(targetX, targetY);
    }

    public void Reset()
        => hasTarget = false;

    private bool Accept(float targetX, float targetY)
    {
        hasTarget   = true;
        lastTargetX = targetX;
        lastTargetY = targetY;
        elapsedSec  = 0;

        return true;
    }
}
