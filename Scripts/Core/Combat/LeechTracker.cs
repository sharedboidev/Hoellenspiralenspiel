using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Leech heilt nicht sofort, sondern gleichmäßig über drei Sekunden. Jeder Treffer bringt eine eigene Instanz,
//alle laufen zugleich und heilen nebeneinander
public sealed class LeechTracker
{
    public const float DurationSec = 3f;

    private const double Epsilon = 1e-9;

    private readonly List<Instance> instances = new();

    public bool IsActive => instances.Count > 0;

    public int InstanceCount => instances.Count;

    //Was noch heilt, für die Anzeige im Orb
    public float Pending
    {
        get
        {
            var pending = 0.0;

            foreach (var instance in instances)
                pending += instance.PerSecond * instance.RemainingSec;

            return (float)pending;
        }
    }

    public void Add(float amount)
    {
        if (amount > 0f && float.IsFinite(amount))
            instances.Add(new Instance(amount / (double)DurationSec, DurationSec));
    }

    //Liefert, was in diesem Schritt geheilt wird
    public float Advance(double deltaSec)
    {
        if (deltaSec <= 0 || instances.Count == 0)
            return 0f;

        var healed = 0.0;

        for (var i = instances.Count - 1; i >= 0; i--)
        {
            var instance = instances[i];
            var stepSec  = Math.Min(deltaSec, instance.RemainingSec);

            healed                += instance.PerSecond * stepSec;
            instance.RemainingSec -= stepSec;

            if (instance.RemainingSec <= Epsilon)
                instances.RemoveAt(i);
            else
                instances[i] = instance;
        }

        return (float)healed;
    }

    public void Clear()
        => instances.Clear();

    private struct Instance
    {
        public Instance(double perSecond, double remainingSec)
        {
            PerSecond    = perSecond;
            RemainingSec = remainingSec;
        }

        public double PerSecond    { get; }
        public double RemainingSec { get; set; }
    }
}
