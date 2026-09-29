using System;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Spatial;

public readonly record struct Spot(float X, float Y);

public sealed record SpotSearchSettings
{
    public static SpotSearchSettings Default { get; } = new();

    public int TriesPerRound { get; init; } = 24;

    public int Rounds { get; init; } = 12;

    public float Growth { get; init; } = 1.3f;

    //Um so viel wächst der Kreis mindestens, sonst käme ein Kreis ohne Radius nie vom Fleck
    public float MinGrowthStep { get; init; } = 50f;
}

public static class SpotSearch
{
    //Würfelt Punkte im Kreis, bis einer frei ist. Nach jeder erfolglosen Runde wächst der Kreis
    public static bool TryFind(Spot               center,
                               float              radius,
                               IRandomSource      random,
                               Func<Spot, bool>   isFree,
                               out Spot           found,
                               SpotSearchSettings settings = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(isFree);

        settings ??= SpotSearchSettings.Default;

        var reach = Math.Max(0f, radius);

        for (var round = 0; round < settings.Rounds; round++)
        {
            var tries = reach <= 0f ? 1 : settings.TriesPerRound;

            for (var attempt = 0; attempt < tries; attempt++)
            {
                found = reach <= 0f ? center : RollWithin(center, reach, random);

                if (isFree(found))
                    return true;
            }

            reach = Math.Max(reach * settings.Growth, reach + settings.MinGrowthStep);
        }

        found = center;

        return false;
    }

    //Die Wurzel verteilt die Punkte gleichmäßig über die Fläche, sonst häuften sie sich in der Mitte
    private static Spot RollWithin(Spot center, float reach, IRandomSource random)
    {
        var angle    = random.NextFloat() * MathF.Tau;
        var distance = MathF.Sqrt(random.NextFloat()) * reach;

        return new Spot(center.X + MathF.Cos(angle) * distance, center.Y + MathF.Sin(angle) * distance);
    }
}
