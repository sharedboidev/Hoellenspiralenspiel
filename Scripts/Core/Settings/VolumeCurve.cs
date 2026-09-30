using System;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

//Das Ohr hört logarithmisch. Mit dem Quadrat des Reglers klingt die Mitte auch wie die Mitte, 50 % sind rund -12 dB
public static class VolumeCurve
{
    public static bool IsMuted(float share)
        => !(share > 0f);

    public static float ToDecibels(float share)
    {
        if (IsMuted(share))
            return float.NegativeInfinity;

        var amplitude = Math.Min(share, 1f);

        return 20f * MathF.Log10(amplitude * amplitude);
    }
}
