using System.Text;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public static class SkillTooltip
{
    private const float WidthPx       = 300f;
    private const int   MarginPx      = 10;
    private const int   FontSize      = 22;
    private const int   TitleFontSize = 32;
    private const int   NoteFontSize  = 17;
    private const int   OutlineSize   = 6;

    //Godot trennt Zeilen mit einem einzelnen Zeilenvorschub, der Wagenrücklauf von Windows ergäbe Leerzeilen
    private const char NewLine = '\n';

    public static string Build(SkillResource skill, IHero caster)
    {
        var estimate = Estimate(skill, caster);
        var uses     = skill.Kind == SkillKind.Attack ? "Attacks" : "Casts";
        var text     = new StringBuilder();

        text.Append(Title($"[color=gold]{skill.NameOrId}[/color]")).Append(NewLine).Append(NewLine);

        if (!SkillGate.FitsWeapon(skill.Definition, caster.Weapon.IsRanged))
            text.Append("[color=red]Needs a melee weapon[/color]").Append(NewLine);
        text.Append($"[color=orange]DPS: {Format(estimate.Dps)}[/color]").Append(NewLine);
        text.Append($"Average Hit: {Format(estimate.AverageHit)}").Append(NewLine);
        text.Append($"Crit Chance: {estimate.CriticalHitChance:0.#}%").Append(NewLine);
        text.Append($"{uses} per Second: {estimate.UsesPerSecond:0.##}");

        if (skill.Kind == SkillKind.Spell && skill.Definition.CastSec > 0)
            text.Append(NewLine).Append($"Cast Time: {skill.Definition.GetCastSec(caster.Stats):0.##} s");

        if (skill.CooldownSec > 0)
            text.Append(NewLine).Append($"Cooldown: {skill.CooldownSec:0.##} s");

        return text.ToString();
    }

    public static string BuildNote(string title, string hint)
        => $"{Title(title)}{NewLine}[color=gray][font_size={NoteFontSize}]{hint}[/font_size][/color]";

    public static Control CreateContent(string bbcode)
    {
        var label = new RichTextLabel
        {
            BbcodeEnabled     = true,
            FitContent        = true,
            AutowrapMode      = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(WidthPx, 0),
            MouseFilter       = Control.MouseFilterEnum.Ignore,
            Text              = bbcode
        };

        label.AddThemeFontSizeOverride("normal_font_size", FontSize);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", OutlineSize);

        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };

        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
            margin.AddThemeConstantOverride(side, MarginPx);

        margin.AddChild(label);

        return margin;
    }

    public static SkillDamageEstimate Estimate(SkillResource skill, IHero caster)
        => SkillDamageEstimator.Estimate(caster.Stats, caster.Weapon, skill.Definition, caster.StatusEffects.ActionFailureChance);

    private static string Title(string title)
        => $"[center][font_size={TitleFontSize}]{title}[/font_size][/center]";

    private static string Format(float value)
        => value >= 100f ? value.ToString("N0") : value.ToString("0.#");
}
