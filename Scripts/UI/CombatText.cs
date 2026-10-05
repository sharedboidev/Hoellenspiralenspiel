using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public static class CombatText
{
    private const int   NormalFontSize   = 36;
    private const int   CriticalFontSize = 42;
    private const int   StatusFontSize   = 28;
    private const float JitterMeters     = 0.24f;
    private const float StatusNameLift   = 0.45f;

    private const string LayerName = "CombatTextLayer";

    public static void ShowHit(BaseUnit target, HitResult hit)
    {
        var isPlayer = target.Faction == Faction.Player;

        switch (hit.Avoidance)
        {
            case HitAvoidance.Missed:
                Show(target, "Miss", Colors.LightGray, NormalFontSize);

                return;
            case HitAvoidance.Dodged:
                Show(target, "Dodge", Colors.LightGray, NormalFontSize);

                return;
            case HitAvoidance.Parried:
                Show(target, "Parry", Colors.LightGray, NormalFontSize);

                return;
        }

        var text     = hit.WasBlocked ? $"Block {hit.FinalDamage:N0}" : hit.FinalDamage.ToString("N0");
        var fontSize = hit.IsCritical ? CriticalFontSize : NormalFontSize;
        var color    = hit.IsCritical ? Colors.DarkOrange : isPlayer ? Colors.Red : Colors.White;

        Show(target, text, color, fontSize);
    }

    public static void ShowHeal(BaseUnit target, float amount)
        => Show(target, amount.ToString("N0"), Colors.LimeGreen, NormalFontSize);

    //Schaden, den ein Verteidiger mit Reflect zurückwirft
    public static void ShowReflected(BaseUnit target, int amount)
        => Show(target, amount.ToString("N0"), Colors.Silver, NormalFontSize);

    public static void ShowStatusTick(BaseUnit target, StatusTick tick)
        => Show(target, tick.Damage.ToString("N0"), GetColorOf(tick.Kind), StatusFontSize);

    public static void ShowStatusStarted(BaseUnit target, StatusEffectKind kind)
        => Show(target, GetNameOf(kind), GetColorOf(kind), StatusFontSize, StatusNameLift);

    public static void Show(BaseUnit target, string text, Color color, int fontSize, float lift = 0f)
    {
        var scene = target.GetTree()?.CurrentScene;

        if (scene is null || !target.IsSeen)
            return;

        var jitter = new Vector3((float)GD.RandRange(-JitterMeters, JitterMeters), (float)GD.RandRange(-JitterMeters / 2, JitterMeters / 2), 0);
        var anchor = target.GlobalPosition + Vector3.Up * (target.CombatTextHeight + lift) + jitter;

        var label = new FloatingCombatText
        {
            Text        = text,
            WorldAnchor = anchor,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);

        GetLayer(scene).AddChild(label);
    }

    public static CanvasLayer GetLayer(Node scene)
    {
        var layer = scene.GetNodeOrNull<CanvasLayer>(LayerName);

        if (layer is not null)
            return layer;

        layer = new CanvasLayer { Name = LayerName, Layer = 0 };

        scene.AddChild(layer);

        return layer;
    }

    private static Color GetColorOf(StatusEffectKind kind)
        => kind switch
        {
            StatusEffectKind.Bleed => Colors.Crimson,
            StatusEffectKind.Burn  => Colors.OrangeRed,
            StatusEffectKind.Shock => Colors.Yellow,
            StatusEffectKind.Chill => Colors.DeepSkyBlue,
            _                      => Colors.White
        };

    private static string GetNameOf(StatusEffectKind kind)
        => kind switch
        {
            StatusEffectKind.Bleed => "Bleeding",
            StatusEffectKind.Burn  => "Burning",
            StatusEffectKind.Shock => "Shocked",
            StatusEffectKind.Chill => "Chilled",
            _                      => kind.ToString()
        };
}
