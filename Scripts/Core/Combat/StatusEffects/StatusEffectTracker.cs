using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public sealed class StatusEffectTracker
{
    private const double Epsilon = 0.00001;

    private static readonly StatusEffectKind[] AllKinds = Enum.GetValues<StatusEffectKind>();

    private readonly float[]          appliedMagnitudes = new float[AllKinds.Length];
    private readonly List<Instance>[] instances         = new List<Instance>[AllKinds.Length];
    private readonly float[]          pendingDamage     = new float[AllKinds.Length];
    private readonly StatSheet        stats;
    private readonly double[]         tickTimeLeftSec = new double[AllKinds.Length];
    private          int              activeKinds;

    public StatusEffectTracker(StatSheet stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        this.stats = stats;

        for (var i = 0; i < instances.Length; i++)
            instances[i] = new List<Instance>();
    }

    public bool HasAny => activeKinds > 0;

    public float ActionFailureChance => Math.Clamp(GetMagnitude(StatusEffectKind.Shock), 0f, 1f);

    public event Action<StatusEffectKind> Started;

    public event Action<StatusEffectKind> Ended;

    public bool IsActive(StatusEffectKind kind)
        => instances[(int)kind].Count > 0;

    public int GetInstanceCount(StatusEffectKind kind)
        => instances[(int)kind].Count;

    public float GetMagnitude(StatusEffectKind kind)
    {
        var list = instances[(int)kind];

        if (list.Count == 0)
            return 0f;

        var sums      = StatusEffectRules.Get(kind).Stacking == StackingRule.Sum;
        var magnitude = 0f;

        foreach (var instance in list)
            magnitude = sums ? magnitude + instance.Magnitude : Math.Max(magnitude, instance.Magnitude);

        return magnitude;
    }

    public void Apply(StatusEffectApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Magnitude <= 0 || application.DurationSec <= 0)
            return;

        var index     = (int)application.Kind;
        var list      = instances[index];
        var rule      = StatusEffectRules.Get(application.Kind);
        var wasActive = list.Count > 0;

        if (rule.Stacking == StackingRule.Strongest)
            AddUnlessDominated(list, application);
        else
            AddToStack(list, application, rule.MaxInstances);

        if (!wasActive)
        {
            activeKinds++;
            pendingDamage[index]   = 0f;
            tickTimeLeftSec[index] = CombatRules.StatusTickIntervalSec;
        }

        RefreshModifiers(application.Kind);

        if (!wasActive)
            Started?.Invoke(application.Kind);
    }

    public void Advance(double deltaSec, List<StatusTick> ticks)
    {
        ArgumentNullException.ThrowIfNull(ticks);

        if (activeKinds == 0 || deltaSec <= 0)
            return;

        foreach (var kind in AllKinds)
        {
            if (instances[(int)kind].Count > 0)
                AdvanceKind(kind, deltaSec, ticks);
        }
    }

    public void Clear()
    {
        foreach (var kind in AllKinds)
        {
            var index = (int)kind;

            if (instances[index].Count == 0)
                continue;

            instances[index].Clear();
            pendingDamage[index] = 0f;

            End(kind);
        }
    }

    private void AdvanceKind(StatusEffectKind kind, double deltaSec, List<StatusTick> ticks)
    {
        var index       = (int)kind;
        var list        = instances[index];
        var dealsDamage = StatusEffectRules.Get(kind).DealsDamage;
        var timeLeft    = deltaSec;

        //In Teilschritten bis zum Ende der jeweils kürzesten Instanz, damit keine Instanz über ihr Ende hinaus wirkt
        while (timeLeft > Epsilon && list.Count > 0)
        {
            var step = Math.Min(timeLeft, GetShortestRemaining(list));

            if (dealsDamage)
                pendingDamage[index] += GetMagnitude(kind) * (float)step;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var instance = list[i];
                instance.RemainingSec -= step;

                if (instance.RemainingSec <= Epsilon)
                    list.RemoveAt(i);
                else
                    list[i] = instance;
            }

            timeLeft -= step;
        }

        if (dealsDamage)
            CollectTicks(kind, deltaSec, list.Count == 0, ticks);

        if (list.Count == 0)
            End(kind);
        else
            RefreshModifiers(kind);
    }

    private void CollectTicks(StatusEffectKind kind, double deltaSec, bool hasEnded, List<StatusTick> ticks)
    {
        var index = (int)kind;

        if (hasEnded)
        {
            var rest = (int)MathF.Round(pendingDamage[index]);

            if (rest > 0)
                ticks.Add(new StatusTick(kind, rest));

            pendingDamage[index] = 0f;

            return;
        }

        tickTimeLeftSec[index] -= deltaSec;

        if (tickTimeLeftSec[index] > Epsilon)
            return;

        while (tickTimeLeftSec[index] <= Epsilon)
            tickTimeLeftSec[index] += CombatRules.StatusTickIntervalSec;

        var damage = (int)MathF.Floor(pendingDamage[index] + (float)Epsilon);

        if (damage <= 0)
            return;

        pendingDamage[index] -= damage;

        ticks.Add(new StatusTick(kind, damage));
    }

    private void End(StatusEffectKind kind)
    {
        activeKinds--;

        RefreshModifiers(kind);

        Ended?.Invoke(kind);
    }

    private void RefreshModifiers(StatusEffectKind kind)
    {
        var index     = (int)kind;
        var magnitude = GetMagnitude(kind);

        if (appliedMagnitudes[index].Equals(magnitude))
            return;

        appliedMagnitudes[index] = magnitude;

        var modifiers = StatusEffectRules.GetModifiers(kind, magnitude);

        stats.Update(sheet =>
        {
            sheet.RemoveModifiersOf(StatusEffectRules.GetOriginId(kind));
            sheet.AddModifiers(modifiers);
        });
    }

    private static void AddUnlessDominated(List<Instance> list, StatusEffectApplication application)
    {
        foreach (var instance in list)
        {
            if (instance.Magnitude >= application.Magnitude && instance.RemainingSec >= application.DurationSec)
                return;
        }

        list.RemoveAll(instance => instance.Magnitude <= application.Magnitude && instance.RemainingSec <= application.DurationSec);
        list.Add(new Instance(application.Magnitude, application.DurationSec));
    }

    private static void AddToStack(List<Instance> list, StatusEffectApplication application, int maxInstances)
    {
        list.Add(new Instance(application.Magnitude, application.DurationSec));

        if (list.Count <= maxInstances)
            return;

        var weakestIndex = 0;

        for (var i = 1; i < list.Count; i++)
        {
            if (list[i].RemainingEffect < list[weakestIndex].RemainingEffect)
                weakestIndex = i;
        }

        list.RemoveAt(weakestIndex);
    }

    private static double GetShortestRemaining(List<Instance> list)
    {
        var shortest = double.MaxValue;

        foreach (var instance in list)
            shortest = Math.Min(shortest, instance.RemainingSec);

        return shortest;
    }

    private struct Instance
    {
        public Instance(float magnitude, double remainingSec)
        {
            Magnitude    = magnitude;
            RemainingSec = remainingSec;
        }

        public float  Magnitude       { get; }
        public double RemainingSec    { get; set; }
        public double RemainingEffect => Magnitude * RemainingSec;
    }
}
