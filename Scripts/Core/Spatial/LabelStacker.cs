using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Spatial;

//Bildschirmkoordinaten: Top wächst nach unten
public readonly record struct LabelBox(float Left, float Top, float Width, float Height)
{
    //Zwei Schilder, die nach dem Stapeln genau den Abstand halten, dürfen durch Rundung nicht wieder als berührt gelten
    private const float Tolerance = 0.01f;

    public float Right  => Left + Width;
    public float Bottom => Top + Height;

    public bool Touches(LabelBox other, float gap)
    {
        var reach = gap - Tolerance;

        return Left < other.Right + reach && other.Left < Right + reach && Top < other.Bottom + reach && other.Top < Bottom + reach;
    }
}

public static class LabelStacker
{
    //Das neue Schild weicht nach oben aus, bis es kein anderes berührt. Die liegenden Schilder bleiben, wo sie sind
    public static LabelBox Place(LabelBox wanted, IReadOnlyList<LabelBox> placed, float gap)
    {
        ArgumentNullException.ThrowIfNull(placed);

        var box = wanted;

        //Jeder Durchgang hebt das Schild über mindestens ein weiteres, mehr Durchgänge als Schilder braucht es nie
        for (var pass = 0; pass <= placed.Count; pass++)
        {
            var wasLifted = false;

            foreach (var other in placed)
            {
                if (!box.Touches(other, gap))
                    continue;

                box       = box with { Top = other.Top - gap - box.Height };
                wasLifted = true;
            }

            if (!wasLifted)
                break;
        }

        return box;
    }

    //Von unten nach oben, so bleibt das Schild des unteren Beutels unter dem des oberen
    public static LabelBox[] PlaceAll(IReadOnlyList<LabelBox> wanted, float gap)
    {
        ArgumentNullException.ThrowIfNull(wanted);

        var order = new int[wanted.Count];

        for (var i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (left, right) =>
        {
            var byHeight = wanted[right].Bottom.CompareTo(wanted[left].Bottom);

            if (byHeight != 0)
                return byHeight;

            var bySide = wanted[left].Left.CompareTo(wanted[right].Left);

            return bySide != 0 ? bySide : left.CompareTo(right);
        });

        var result = new LabelBox[wanted.Count];
        var placed = new List<LabelBox>(wanted.Count);

        foreach (var index in order)
        {
            result[index] = Place(wanted[index], placed, gap);

            placed.Add(result[index]);
        }

        return result;
    }
}
