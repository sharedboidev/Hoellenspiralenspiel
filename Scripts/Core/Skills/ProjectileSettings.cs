using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Pierces: Das Projektil fliegt nach einem Treffer weiter und trifft jedes Ziel auf seiner Bahn einmal. Mauern halten es trotzdem auf
public sealed record ProjectileSettings(float Speed,
                                        float LifetimeSec,
                                        int   ForkCount       = 0,
                                        int   ForkGenerations = 0,
                                        float ForkRange       = 0f,
                                        bool  Pierces         = false)
{
    public float Reach => Math.Max(0f, Speed) * Math.Max(0f, LifetimeSec);

    public bool CanFork => ForkCount > 0 && ForkGenerations > 0 && ForkRange > 0f;

    public int MaxProjectiles
    {
        get
        {
            if (!CanFork)
                return 1;

            var total         = 1;
            var perGeneration = 1;

            for (var generation = 0; generation < ForkGenerations; generation++)
            {
                perGeneration *= ForkCount;
                total         += perGeneration;
            }

            return total;
        }
    }
}
