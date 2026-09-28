using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Eine Fläche auf dem Boden. Nach DelaySec beginnt sie zu wirken und wächst in ExpansionSec auf ihren Radius.
//Ohne ExpansionSec trifft sie alle Einheiten in der Fläche im selben Augenblick
public sealed record AreaSettings(float Radius, float ExpansionSec = 0f, float DelaySec = 0f)
{
    //Der Boden ist isometrisch gestaucht: Ein Kreis auf dem Boden ist auf dem Bildschirm halb so hoch wie breit
    public const float GroundYScale = 0.5f;

    //Radius der Fläche, nachdem sie die angegebene Zeit gewirkt hat. Die Verzögerung ist dabei schon abgelaufen
    public float GetRadiusAfter(double activeSec)
    {
        if (activeSec < 0)
            return 0f;

        if (ExpansionSec <= 0f || activeSec >= ExpansionSec)
            return Math.Max(0f, Radius);

        return Math.Max(0f, Radius) * (float)(activeSec / ExpansionSec);
    }

    //Liegt ein Punkt mit diesem Abstand zur Mitte innerhalb des Radius? Der Abstand nach oben und unten zählt wegen der Stauchung doppelt
    public static bool Contains(float offsetX, float offsetY, float radius)
    {
        if (radius <= 0f)
            return false;

        var groundY = offsetY / GroundYScale;

        return offsetX * offsetX + groundY * groundY <= radius * radius;
    }
}
