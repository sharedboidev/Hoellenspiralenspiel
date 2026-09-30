using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Hud;

public readonly record struct ScreenSpan(float Left, float Right)
{
    public float Width => Right - Left;
}

public static class FreeSpan
{
    //Bei gleicher Breite gewinnt der linke Streifen
    public static ScreenSpan? Widest(float screenWidth, IEnumerable<ScreenSpan> covered, float minWidth)
    {
        ArgumentNullException.ThrowIfNull(covered);

        var widest = new ScreenSpan(0f, 0f);
        var left   = 0f;

        foreach (var part in covered.Where(part => part.Width > 0f && part.Right > 0f && part.Left < screenWidth).OrderBy(part => part.Left))
        {
            widest = Wider(widest, new ScreenSpan(left, part.Left));
            left   = Math.Max(left, part.Right);
        }

        widest = Wider(widest, new ScreenSpan(left, screenWidth));

        return widest.Width > 0f && widest.Width >= minWidth ? widest : null;
    }

    private static ScreenSpan Wider(ScreenSpan widest, ScreenSpan candidate)
        => candidate.Width > widest.Width ? candidate : widest;
}
