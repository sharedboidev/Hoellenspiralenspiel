using System;

namespace Hoellenspiralenspiel.Scripts.Core.Rng;

//Gleicher Seed ergibt dieselbe Folge von Würfen
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
