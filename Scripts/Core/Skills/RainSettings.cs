using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Pfeile, die nach einem Schuss in den Himmel auf ein Zielgebiet fallen. Jeder ist ein eigener Treffer mit dem Anteil des Skills am Waffenschaden.
//Längen in Pixeln auf dem Boden, Zeiten in Sekunden ab dem Schuss
public sealed record RainSettings(int Count, float Radius, float ImpactRadius, float DelaySec, float DurationSec)
{
    //Zusätzliche Projektile kommen als weitere Pfeile dazu
    public int GetCount(int bonusProjectiles)
        => Math.Max(0, Count) + Math.Max(0, bonusProjectiles);

    //Der erste Pfeil schlägt nach der Verzögerung ein, der letzte am Ende der Dauer, die übrigen gleichmäßig dazwischen
    public IReadOnlyList<float> GetImpactDelays(int count)
    {
        var delays = new List<float>(Math.Max(0, count));

        for (var i = 0; i < count; i++)
            delays.Add(DelaySec + (count > 1 ? DurationSec * i / (count - 1) : 0f));

        return delays;
    }

    //Versatz der Einschläge zur Mitte. Jeder Pfeil bekommt einen gleich großen Sektor, damit nicht alle auf einer Seite fallen.
    //Der Abstand ist gleichverteilt über den Radius, also dichter zur Mitte hin: Ein Teil der Pfeile trifft ein Ziel in der Mitte, der Rest streut um es herum
    public IReadOnlyList<(float X, float Y)> PickLandings(int count, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var landings = new List<(float X, float Y)>(Math.Max(0, count));

        if (count <= 0)
            return landings;

        var sector = MathF.Tau / count;
        var start  = random.NextFloat() * MathF.Tau;
        var reach  = Math.Max(0f, Radius);

        for (var i = 0; i < count; i++)
        {
            var angle    = start + (i + random.NextFloat()) * sector;
            var distance = random.NextFloat() * reach;

            landings.Add((MathF.Cos(angle) * distance, MathF.Sin(angle) * distance));
        }

        return landings;
    }

    //Anteil der Pfeile, deren Einschlag einen Punkt in der Mitte erreicht. Die Schätzung im Tooltip rechnet damit, ein Körper fängt mehr
    public float GetShareOnCenter()
        => Radius <= 0f ? 1f : Math.Clamp(Math.Max(0f, ImpactRadius) / Radius, 0f, 1f);
}
