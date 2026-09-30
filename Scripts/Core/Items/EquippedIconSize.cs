using System;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Ein ausgerüstetes Item zeigt sich in seiner eigenen Zellgröße wie im Inventar, nie größer als sein Platz
public static class EquippedIconSize
{
    public static (int Width, int Height) Get(ItemDefinition item, int slotWidth, int slotHeight, int cellPx, int framePx)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (Math.Min(item.Width, slotWidth) * cellPx - framePx, Math.Min(item.Height, slotHeight) * cellPx - framePx);
    }
}
