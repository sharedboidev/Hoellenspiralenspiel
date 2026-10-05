using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Die Höhe eines geworfenen Körpers, der aus startHeight bis peakHeight steigt und bei Fortschritt 1 auf dem Boden landet.
//Die Schwerkraft ist auf dem ganzen Weg gleich, deshalb steigt er schneller, als er fällt, wenn er nicht vom Boden startet
public static class LobArc
{
    public static float HeightAt(float progress, float startHeight, float peakHeight)
    {
        var start = Math.Max(0f, startHeight);
        var peak  = Math.Max(start, peakHeight);
        var t     = Math.Clamp(progress, 0f, 1f);

        if (peak <= 0f)
            return 0f;

        var top = GetTopProgress(start, peak);

        if (t < top)
        {
            var rest = (top - t) / top;

            return peak - (peak - start) * rest * rest;
        }

        var fall = (t - top) / (1f - top);

        return peak - peak * fall * fall;
    }

    //Steigen und Fallen dauern so lange wie die Wurzel ihrer Höhe
    public static float GetTopProgress(float startHeight, float peakHeight)
    {
        var start = Math.Max(0f, startHeight);
        var peak  = Math.Max(start, peakHeight);

        if (peak <= 0f)
            return 0f;

        var rise = MathF.Sqrt(peak - start);

        return rise / (rise + MathF.Sqrt(peak));
    }
}
