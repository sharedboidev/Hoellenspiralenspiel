using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Spatial;

public static class GridSearch
{
    //Das Gitter hängt an der Welt, nicht an der Mitte. Was nacheinander von verschiedenen Orten aus abgelegt wird, liegt dadurch in einer Flucht
    public static bool TryFind(Spot center, float spacing, float minDistance, float maxDistance, Func<Spot, bool> isFree, out Spot found)
    {
        ArgumentNullException.ThrowIfNull(isFree);

        if (spacing <= 0f)
            throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Der Abstand der Gitterpunkte muss größer als 0 sein.");

        foreach (var spot in ListAround(center, spacing, minDistance, maxDistance))
        {
            if (!isFree(spot))
                continue;

            found = spot;

            return true;
        }

        found = center;

        return false;
    }

    //Die nächsten Punkte zuerst, so füllt sich der Kreis von innen nach außen
    private static List<Spot> ListAround(Spot center, float spacing, float minDistance, float maxDistance)
    {
        var spots       = new List<Spot>();
        var firstColumn = (int)MathF.Ceiling((center.X - maxDistance) / spacing);
        var lastColumn  = (int)MathF.Floor((center.X + maxDistance) / spacing);
        var firstRow    = (int)MathF.Ceiling((center.Y - maxDistance) / spacing);
        var lastRow     = (int)MathF.Floor((center.Y + maxDistance) / spacing);

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            for (var row = firstRow; row <= lastRow; row++)
            {
                var spot     = new Spot(column * spacing, row * spacing);
                var distance = GetDistanceSquared(spot, center);

                if (distance >= minDistance * minDistance && distance <= maxDistance * maxDistance)
                    spots.Add(spot);
            }
        }

        spots.Sort((left, right) =>
        {
            var byDistance = GetDistanceSquared(left, center).CompareTo(GetDistanceSquared(right, center));

            if (byDistance != 0)
                return byDistance;

            return left.X.Equals(right.X) ? left.Y.CompareTo(right.Y) : left.X.CompareTo(right.X);
        });

        return spots;
    }

    private static float GetDistanceSquared(Spot spot, Spot center)
        => (spot.X - center.X) * (spot.X - center.X) + (spot.Y - center.Y) * (spot.Y - center.Y);
}
