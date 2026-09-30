using System.Text;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

//Dieselbe Aufmachung wie SkillTooltip, damit Tränke auf der Leiste aussehen wie Skills
public static class ConsumableTooltip
{
    private const int  TitleFontSize = 32;
    private const int  NoteFontSize  = 17;
    private const char NewLine       = '\n';

    public static string Build(ConsumableBaseResource consumable, int countInInventory, string hint = null)
    {
        var definition = consumable.Definition;
        var text       = new StringBuilder();

        text.Append($"[center][font_size={TitleFontSize}]{definition.Name}[/font_size][/center]").Append(NewLine).Append(NewLine);
        text.Append(DescribeEffect(definition.Consumable)).Append(NewLine);
        text.Append($"In inventory: {countInInventory:N0}");

        if (!string.IsNullOrEmpty(hint))
            text.Append(NewLine).Append($"[color=gray][font_size={NoteFontSize}]{hint}[/font_size][/color]");

        return text.ToString();
    }

    private static string DescribeEffect(ConsumableEffect effect)
        => effect?.Kind switch
        {
            ConsumableEffectKind.RestoreLife => $"Recovered Life: [color=lime_green]{effect.Percent:N0}[/color]%",
            ConsumableEffectKind.RestoreMana => $"Recovered Mana: [color=royal_blue]{effect.Percent:N0}[/color]%",
            _                                => string.Empty
        };
}
