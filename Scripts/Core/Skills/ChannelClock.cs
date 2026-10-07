using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Zählt die Ticks eines Wirbels. Ein großer Zeitschritt kann mehrere Ticks fällig machen, mehr als zehn auf einmal gibt es nicht
public sealed class ChannelClock
{
    private const int    MaxTicksPerStep = 10;
    private const double MinIntervalSec  = 0.001;

    private double untilTickSec;

    public bool IsRunning { get; private set; }

    public void Start(double firstTickSec)
    {
        untilTickSec = Math.Max(0, firstTickSec);
        IsRunning    = true;
    }

    public void Stop()
        => IsRunning = false;

    public int Advance(double deltaSec, double intervalSec)
    {
        if (!IsRunning)
            return 0;

        var interval = Math.Max(MinIntervalSec, intervalSec);
        var ticks    = 0;

        untilTickSec -= Math.Max(0, deltaSec);

        while (untilTickSec <= 0 && ticks < MaxTicksPerStep)
        {
            ticks++;
            untilTickSec += interval;
        }

        return ticks;
    }
}
