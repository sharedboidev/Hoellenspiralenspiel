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
            text.Append(skill.Definition.NeedsBow ? "[color=red]Needs a bow[/color]" : "[color=red]Needs a melee weapon[/color]").Append(NewLine);
        text.Append($"[color=orange]DPS: {Format(estimate.Dps)}[/color]").Append(NewLine);
        text.Append($"Average Hit: {Format(estimate.AverageHit)}{(skill.Definition.IsCharged ? " at full charge" : string.Empty)}").Append(NewLine);

        if (skill.Definition.IsCharged)
            AppendCharge(text, skill.Definition, ChargeSettings.GetRateFactor(caster.Stats));

        if (estimate.ScatterCount > 0)
            text.Append($"Per Ball: {Format(estimate.ScatterAverageHit)} ({estimate.ScatterCount} Balls)").Append(NewLine);

        if (estimate.ArrowCount > 0)
            text.Append($"Arrows: {estimate.ArrowCount}, about {estimate.ArrowsOnTarget:0.#} on a single target").Append(NewLine);

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

    //Der Waffenschaden des Skills gilt bei voller Ladung, die Mindest- und Höchstladung geben ihren Anteil davon. Die Zeiten gelten für das Tempo des Helden
    private static void AppendCharge(StringBuilder text, SkillDefinition skill, float rateFactor)
    {
        var charge = skill.Charge;
        var min    = Stage(skill, charge.MinPercent, rateFactor);
        var full   = Stage(skill, ChargeSettings.FullPercent, rateFactor);
        var max    = Stage(skill, charge.MaxPercent, rateFactor);

        text.Append($"Charge: {min}, {full}, {max}").Append(NewLine);
        text.Append($"Pierces past {Seconds(charge, ChargeSettings.FullPercent, rateFactor)}. Held {charge.OverholdSec:0.#} s past {Seconds(charge, charge.MaxPercent, rateFactor)} it fizzles, {charge.OverholdCooldownSec:0.#} s cooldown")
            .Append(NewLine);
    }

    private static string Stage(SkillDefinition skill, float chargePercent, float rateFactor)
        => $"{skill.Attack.WeaponDamagePercent * skill.Charge.GetDamageFactor(chargePercent):0}% at {Seconds(skill.Charge, chargePercent, rateFactor)}";

    private static string Seconds(ChargeSettings charge, float chargePercent, float rateFactor)
        => $"{charge.GetSecToReach(chargePercent, rateFactor):0.#} s";

    public static SkillDamageEstimate Estimate(SkillResource skill, IHero caster)
        => SkillDamageEstimator.Estimate(caster.Stats, caster.Weapon, skill.Definition, caster.StatusEffects.ActionFailureChance);

    private static string Title(string title)
        => $"[center][font_size={TitleFontSize}]{title}[/font_size][/center]";

    private static string Format(float value)
        => value >= 100f ? value.ToString("N0") : value.ToString("0.#");
}
