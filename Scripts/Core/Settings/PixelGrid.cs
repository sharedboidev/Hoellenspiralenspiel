using System;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

//Ein Pixel der PS1 deckt immer gleich viele Pixel des Bildschirms. Sonst wechseln die Zellen zwischen zwei Größen, bei 2000 Zeilen etwa 8 und 9.
//Jede feinere Stufe ist echt feiner, solange die Zellen dafür groß genug sind. Sonst gäbe es bei 720 oder 1200 Zeilen zweimal dieselbe
public static class PixelGrid
{
    public static int TargetLines(PixelGrain grain)
        => grain switch
        {
            PixelGrain.Fine   => 480,
            PixelGrain.Medium => 360,
            _                 => 240
        };

    public static int CellSize(int screenHeight, PixelGrain grain)
    {
        var coarse = Near(screenHeight, PixelGrain.Coarse);

        if (grain == PixelGrain.Coarse)
            return coarse;

        var medium = Math.Max(1, Math.Min(Near(screenHeight, PixelGrain.Medium), coarse - 1));

        return grain == PixelGrain.Medium ? medium : Math.Max(1, Math.Min(Near(screenHeight, PixelGrain.Fine), medium - 1));
    }

    private static int Near(int screenHeight, PixelGrain grain)
        => Math.Max(1, (int)Math.Round((double)screenHeight / TargetLines(grain), MidpointRounding.AwayFromZero));
}
