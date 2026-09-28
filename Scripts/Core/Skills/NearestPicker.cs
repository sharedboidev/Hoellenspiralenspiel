using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Sucht die nächsten Kandidaten in einem Durchlauf, ohne alle nach Entfernung zu sortieren
public static class NearestPicker
{
    //Füllt result mit höchstens count Kandidaten innerhalb von maxDistance, der nächste zuerst.
    //getDistanceSquared liefert das Quadrat des Abstands. Ein negativer Wert schließt den Kandidaten aus
    public static void Pick<T>(IReadOnlyList<T> candidates,
                               Func<T, float>   getDistanceSquared,
                               float            maxDistance,
                               int              count,
                               List<T>          result)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(getDistanceSquared);
        ArgumentNullException.ThrowIfNull(result);

        result.Clear();

        if (count <= 0 || maxDistance <= 0f)
            return;

        var maxDistanceSquared = maxDistance * maxDistance;
        var distances          = new List<float>(count);

        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate       = candidates[i];
            var distanceSquared = getDistanceSquared(candidate);

            if (distanceSquared < 0f || distanceSquared > maxDistanceSquared)
                continue;

            if (result.Count == count && distanceSquared >= distances[count - 1])
                continue;

            var position = distances.Count;

            while (position > 0 && distances[position - 1] > distanceSquared)
                position--;

            distances.Insert(position, distanceSquared);
            result.Insert(position, candidate);

            if (result.Count <= count)
                continue;

            distances.RemoveAt(count);
            result.RemoveAt(count);
        }
    }
}
