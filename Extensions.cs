using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.UI;

namespace Hoellenspiralenspiel;

public static class Extensions
{
    public static T[] GetAllChildren<T>(this Node node)
            where T : Node
    {
        var myChildren = node.GetChildren()
                             .Where(mc => mc is T)
                             .Cast<T>()
                             .ToList();

        var retVal = new List<T>();

        foreach (var child in myChildren)
        {
            var grandChildren = child.GetAllChildren<T>().ToList();

            retVal.AddRange(grandChildren);
        }

        retVal.AddRange(myChildren);

        return retVal.ToArray();
    }

    public static FloatingCombatText AsHeal(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.LimeGreen);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    public static FloatingCombatText AsHealCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkGreen);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    public static FloatingCombatText AsDamageDealt(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.White);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    public static FloatingCombatText AsDamageDealtCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkOrange);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    public static FloatingCombatText AsDamageReceived(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.Red);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    public static FloatingCombatText AsDamageReceivedCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkOrange);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }
}