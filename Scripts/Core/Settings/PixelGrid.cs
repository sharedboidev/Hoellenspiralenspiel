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

    //Das Bild der PS1 in Zellen: so viele, dass sie das Fenster decken. Geht eine Kante des Fensters nicht in Zellen auf,
    //ragt die letzte Zelle über das Fenster hinaus
    public static PixelSize CellsToCover(PixelSize window, int cell)
    {
        cell = Math.Max(1, cell);

        return new PixelSize(Math.Max(1, (window.Width + cell - 1) / cell), Math.Max(1, (window.Height + cell - 1) / cell));
    }

    //Wo das Bild im Fenster liegt, in Pixeln des Fensters. Der Überstand verteilt sich auf beide Seiten, so trifft die Mitte des Bildes
    //die Mitte des Fensters bis auf einen halben Pixel. Ein ungerader Rest geht nach rechts und unten
    public static (int X, int Y) Offset(PixelSize window, PixelSize cells, int cell)
        => (-((cells.Width * cell - window.Width) / 2), -((cells.Height * cell - window.Height) / 2));

    private static int Near(int screenHeight, PixelGrain grain)
        => Math.Max(1, (int)Math.Round((double)screenHeight / TargetLines(grain), MidpointRounding.AwayFromZero));
}
