using System;

namespace Hoellenspiralenspiel.Scripts.Core.Hud;

public readonly record struct ScreenBox(float X, float Y, float Width, float Height)
{
    public float Right  => X + Width;
    public float Bottom => Y + Height;
}

//Über dem Element, ist oben kein Platz, darunter, und immer ganz im Bild. Ein Begleiter steht links daneben, ist dort kein Platz, rücken beide nach rechts
public static class TooltipPlacement
{
    public static ScreenBox Place(ScreenBox item, float width, float height, float screenWidth, float screenHeight)
        => PlaceBeside(item, width, height, IsRoomAbove(item, height), screenWidth, screenHeight);

    public static (ScreenBox Main, ScreenBox Companion) PlaceWithCompanion(ScreenBox item,
                                                                            float mainWidth,
                                                                            float mainHeight,
                                                                            float companionWidth,
                                                                            float companionHeight,
                                                                            float screenWidth,
                                                                            float screenHeight,
                                                                            float gap)
    {
        //Der höhere entscheidet die Seite, sonst ragte er über das Element
        var isAbove = IsRoomAbove(item, Math.Max(mainHeight, companionHeight));
        var main    = PlaceBeside(item, mainWidth, mainHeight, isAbove, screenWidth, screenHeight);
        var x       = main.X - gap - companionWidth;
        var y       = isAbove ? item.Y - companionHeight : main.Y;

        if (x < 0)
        {
            x    = 0;
            main = main with { X = Clamp(companionWidth + gap, screenWidth - mainWidth) };
        }

        return (main, new ScreenBox(x, Clamp(y, screenHeight - companionHeight), companionWidth, companionHeight));
    }

    private static bool IsRoomAbove(ScreenBox item, float height)
        => item.Y - height >= 0;

    private static ScreenBox PlaceBeside(ScreenBox item, float width, float height, bool isAbove, float screenWidth, float screenHeight)
    {
        var x = item.X + item.Width / 2 - width / 2;
        var y = isAbove ? item.Y - height : item.Bottom;

        return new ScreenBox(Clamp(x, screenWidth - width), Clamp(y, screenHeight - height), width, height);
    }

    private static float Clamp(float value, float max)
        => Math.Clamp(value, 0, Math.Max(0, max));
}
