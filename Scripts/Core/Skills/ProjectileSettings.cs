using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Flugverhalten eines Projektils. Geschwindigkeit und Reichweiten in Welteinheiten.
//Trifft das Projektil, entstehen ForkCount neue Projektile zu den nächsten Feinden. Das wiederholt sich ForkGenerations Mal
public sealed record ProjectileSettings(float Speed,
                                        float LifetimeSec,
                                        int   ForkCount       = 0,
                                        int   ForkGenerations = 0,
                                        float ForkRange       = 0f)
{
    //So weit fliegt das Projektil höchstens
    public float Reach => Math.Max(0f, Speed) * Math.Max(0f, LifetimeSec);

    public bool CanFork => ForkCount > 0 && ForkGenerations > 0 && ForkRange > 0f;

    //Obergrenze aller Projektile, die aus einem einzigen Wurf entstehen können
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
