using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Loading;

//Tipps im Ladebildschirm. {aktion} im Text steht für die Taste, die der Spieler dieser Aktion gegeben hat
public static class LoadingTips
{
    private static readonly Regex Placeholder = new(@"\{([A-Za-z0-9_]+)\}");

    //Liefert null, wenn eine Aktion keine Taste hat. Ein solcher Tipp führte in die Irre
    public static string Format(string tip, Func<string, string> keyLabelOf)
    {
        if (string.IsNullOrWhiteSpace(tip))
            return null;

        var isComplete = true;

        var text = Placeholder.Replace(tip, match =>
        {
            var label = keyLabelOf(match.Groups[1].Value);

            if (string.IsNullOrEmpty(label))
                isComplete = false;

            return label ?? string.Empty;
        });

        return isComplete ? text.Trim() : null;
    }

    //Derselbe Tipp kommt nie zweimal hintereinander, solange es einen anderen gibt
    public static string Pick(IEnumerable<string> tips, Func<string, string> keyLabelOf, string previous, IRandomSource random)
    {
        var usable = (tips ?? []).Select(tip => Format(tip, keyLabelOf))
                                 .Where(text => text is not null)
                                 .Distinct()
                                 .ToList();

        if (usable.Count > 1)
            usable.Remove(previous);

        return usable.Count == 0 ? null : usable[random.NextInt(0, usable.Count)];
    }

    //Wie lange der Ladebildschirm noch stehen bleibt, damit man den Tipp lesen kann
    public static double RemainingSec(double minimumSec, double shownSec)
        => Math.Max(0, minimumSec - Math.Max(0, shownSec));
}
