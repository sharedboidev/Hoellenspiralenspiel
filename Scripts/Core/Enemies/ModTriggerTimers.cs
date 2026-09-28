using System;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public sealed class RepeatingTimer
{
    private readonly double periodSec;
    private          double elapsedSec;

    public RepeatingTimer(double periodSec, double startOffsetSec = 0)
    {
        this.periodSec = Math.Max(0.05, periodSec);

        elapsedSec = Math.Clamp(startOffsetSec, 0, this.periodSec);
    }

    //Löst höchstens einmal pro Aufruf aus, der Rest der Zeit bleibt für den nächsten Aufruf stehen
    public bool Advance(double deltaSec)
    {
        elapsedSec += Math.Max(0, deltaSec);

        if (elapsedSec < periodSec)
            return false;

        elapsedSec = Math.Min(elapsedSec - periodSec, periodSec);

        return true;
    }

    public void Reset()
        => elapsedSec = 0;
}

public sealed class ThresholdLatch
{
    private readonly bool  rearms;
    private readonly float threshold;
    private          bool  isBelow;

    public ThresholdLatch(float threshold, bool rearms = false)
    {
        this.threshold = threshold;
        this.rearms    = rearms;
    }

    public bool Update(float value)
    {
        if (value > threshold)
        {
            if (rearms)
                isBelow = false;

            return false;
        }

        if (isBelow)
            return false;

        isBelow = true;

        return true;
    }
}

public sealed class TriggerGate
{
    private const double RoundingSlackSec = 1e-9;

    private readonly float  chancePercent;
    private readonly double cooldownSec;
    private          double cooldownLeftSec;

    public TriggerGate(float chancePercent = 100f, double cooldownSec = 0)
    {
        this.chancePercent = chancePercent;
        this.cooldownSec   = Math.Max(0, cooldownSec);
    }

    public void Advance(double deltaSec)
    {
        cooldownLeftSec -= deltaSec;

        if (cooldownLeftSec < RoundingSlackSec)
            cooldownLeftSec = 0;
    }

    //Gewürfelt wird nur, wenn die Chance unter 100 % liegt und die Abklingzeit vorbei ist
    public bool TryPass(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (cooldownLeftSec > 0)
            return false;

        if (chancePercent < 100f && random.NextPercent() >= chancePercent)
            return false;

        cooldownLeftSec = cooldownSec;

        return true;
    }
}
