using System;

namespace Hoellenspiralenspiel.Scripts.Core.Rng;

public sealed class SeededRandom : IRandomSource
{
    private readonly Random random;

    public SeededRandom(int seed)
    {
        Seed   = seed;
        random = new Random(seed);
    }

    public int Seed { get; }

    public float NextFloat()
        => random.NextSingle();
}
