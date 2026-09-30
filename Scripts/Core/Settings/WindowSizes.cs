using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

public readonly record struct PixelSize(int Width, int Height)
{
    public bool FitsInto(PixelSize area)
        => Width <= area.Width && Height <= area.Height;

    public override string ToString()
        => $"{Width}x{Height}";
}

//Größen für den Fenstermodus. Godot schaltet die Auflösung des Bildschirms nie um, Größen gibt es deshalb nur im Fenster
public static class WindowSizes
{
    public static readonly PixelSize Minimum = new(640, 360);
    public static readonly PixelSize Maximum = new(7680, 4320);

    private static readonly PixelSize[] Common =
    [
        new(1280, 720),
        new(1280, 800),
        new(1600, 900),
        new(1680, 1050),
        new(1920, 1080),
        new(1920, 1200),
        new(2560, 1440),
        new(2560, 1600),
        new(3200, 1800),
        new(3840, 2160)
    ];

    //Nur was ganz auf die nutzbare Fläche passt. Passt nichts, bleibt die Fläche selbst
    public static IReadOnlyList<PixelSize> Offer(PixelSize usable)
    {
        var fitting = Common.Where(size => size.FitsInto(usable)).ToList();

        return fitting.Count > 0 ? fitting : [Fit(usable, usable)];
    }

    public static PixelSize Fit(PixelSize wanted, PixelSize usable)
    {
        var maxWidth  = Math.Max(Minimum.Width, Math.Min(Maximum.Width, usable.Width));
        var maxHeight = Math.Max(Minimum.Height, Math.Min(Maximum.Height, usable.Height));

        return new PixelSize(Math.Clamp(wanted.Width, Minimum.Width, maxWidth), Math.Clamp(wanted.Height, Minimum.Height, maxHeight));
    }
}
