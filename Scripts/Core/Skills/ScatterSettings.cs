using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Kugeln, die aus einem getroffenen Ziel springen und im Bogen um es herum einschlagen. Jede ist ein eigener Treffer mit eigenem Anteil am Waffenschaden.
//Alle Werte in Pixeln auf dem Boden
public sealed record ScatterSettings(int Count, float WeaponDamagePercent, float ImpactRadius, float FlightSec)
{
    //Etwas Luft zum Rand des Einschlags, damit Rundung den Treffer auf das stehende Ziel nicht kostet
    private const float ImpactRadiusShare = 0.9f;

    //Die Kugeln schlagen mit dem Element des Schlags ein, nur mit ihrem eigenen Anteil am Waffenschaden
    public AttackDefinition GetAttack(AttackDefinition strike)
    {
        ArgumentNullException.ThrowIfNull(strike);

        return strike with { WeaponDamagePercent = WeaponDamagePercent };
    }

    //Versatz der Einschläge zur Mitte des Ziels. Jede Kugel bekommt einen gleich großen Sektor um das Ziel, damit nicht alle auf einer Seite landen.
    //Sie landet außerhalb seines Körpers, aber so nah, dass ihr Einschlag ihn noch erreicht
    public IReadOnlyList<(float X, float Y)> PickLandings(float targetRadiusPx, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var count    = Math.Max(0, Count);
        var landings = new List<(float X, float Y)>(count);

        if (count == 0)
            return landings;

        var sector = MathF.Tau / count;
        var start  = random.NextFloat() * MathF.Tau;
        var inner  = Math.Max(0f, targetRadiusPx);
        var outer  = inner + Math.Max(0f, ImpactRadius) * ImpactRadiusShare;

        for (var i = 0; i < count; i++)
        {
            var angle = start + (i + random.NextFloat()) * sector;

            //Gleichmäßig über die Fläche des Rings, nicht gehäuft am inneren Rand
            var distance = MathF.Sqrt(inner * inner + random.NextFloat() * (outer * outer - inner * inner));

            landings.Add((MathF.Cos(angle) * distance, MathF.Sin(angle) * distance));
        }

        return landings;
    }
}
