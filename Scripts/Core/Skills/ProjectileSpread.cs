using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public static class ProjectileSpread
{
    public const float DegreesBetweenProjectiles = 12f;
    public const float MaxTotalDegrees           = 60f;

    //Der Fächer liegt mittig um die Zielrichtung, ein einzelnes Projektil fliegt geradeaus
    public static float GetOffsetDegrees(int index, int count)
    {
        if (count <= 1)
            return 0f;

        var totalDegrees = Math.Min(MaxTotalDegrees, DegreesBetweenProjectiles * (count - 1));
        var step         = totalDegrees / (count - 1);

        return -totalDegrees / 2f + step * Math.Clamp(index, 0, count - 1);
    }
}
