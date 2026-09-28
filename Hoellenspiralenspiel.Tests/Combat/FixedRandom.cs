using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Tests.Combat;

internal sealed class FixedRandom : IRandomSource
{
    private readonly float        fallback;
    private readonly Queue<float> values;

    public FixedRandom(float fallback, params float[] values)
    {
        this.fallback = fallback;
        this.values   = new Queue<float>(values);
    }

    public int Draws { get; private set; }

    public float NextFloat()
    {
        Draws++;

        return values.Count > 0 ? values.Dequeue() : fallback;
    }
}

internal static class Rolls
{
    private const float Never = 0.999f;

    public static FixedRandom Create(float hit    = 0f,
                                     float dodge  = Never,
                                     float parry  = Never,
                                     float block  = Never,
                                     float crit   = Never,
                                     float damage = 0.5f)
        => new(0.5f, hit, dodge, parry, block, crit, damage);
}
