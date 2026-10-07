using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Die Trefferzone eines Schlags auf ein einzelnes Ziel: ein Kegel vor dem Schlagenden, so weit wie die Waffe reicht.
//Der angeklickte Gegner hat Vorrang, steht er in Reichweite. Sonst trifft der Schlag den nächsten Gegner in der Zone,
//auch ohne Klick, etwa mit der Taste zum Stehenbleiben hinter eine Gruppe. Alles in Pixeln auf dem Boden
public static class StrikeHitbox
{
    public const float ArcDegrees = 90f;

    private static readonly SweepSettings Zone = new(ArcDegrees);

    public static bool Reaches(float offsetX, float offsetY, float facingX, float facingY, float reachPx, float casterRadiusPx, float targetRadiusPx)
        => Zone.Reaches(offsetX, offsetY, facingX, facingY, reachPx, casterRadiusPx, targetRadiusPx);

    //Der nächste Kandidat in der Zone, gemessen von Rand zu Rand. place liefert Versatz zur Mitte des Schlagenden und Körperradius
    public static T PickNearest<T>(IReadOnlyList<T>                                 candidates,
                                   Func<T, (float X, float Y, float Radius)>        place,
                                   float                                            facingX,
                                   float                                            facingY,
                                   float                                            reachPx,
                                   float                                            casterRadiusPx)
            where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(place);

        T   nearest     = null;
        var nearestEdge = float.MaxValue;

        foreach (var candidate in candidates)
        {
            var (x, y, radius) = place(candidate);

            if (!Reaches(x, y, facingX, facingY, reachPx, casterRadiusPx, radius))
                continue;

            var edge = MathF.Sqrt(x * x + y * y) - casterRadiusPx - radius;

            if (edge >= nearestEdge)
                continue;

            nearest     = candidate;
            nearestEdge = edge;
        }

        return nearest;
    }
}
