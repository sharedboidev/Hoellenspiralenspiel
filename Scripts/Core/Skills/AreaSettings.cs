using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public sealed record AreaSettings(float Radius, float ExpansionSec = 0f, float DelaySec = 0f)
{
    public float GetRadiusAfter(double activeSec)
    {
        if (activeSec < 0)
            return 0f;

        if (ExpansionSec <= 0f || activeSec >= ExpansionSec)
            return Math.Max(0f, Radius);

        return Math.Max(0f, Radius) * (float)(activeSec / ExpansionSec);
    }

    public static bool Contains(float offsetX, float offsetY, float radius)
    {
        if (radius <= 0f)
            return false;

        return offsetX * offsetX + offsetY * offsetY <= radius * radius;
    }
}
