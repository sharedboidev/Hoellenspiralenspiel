using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Extensions;

public static class FCTExtensions
{
    private const int   NormalFontSize   = 36;
    private const int   CriticalFontSize = 42;
    private const int   StatusFontSize   = 28;
    private const float JitterPx         = 24f;

    private static readonly PackedScene FCTScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/floating_combat_text.tscn");

    private static readonly Vector2 StatusNameLift = new(0, -45);

    public static void ShowHit(this BaseUnit target, HitResult hit)
    {
        var isPlayer = target.Faction == Faction.Player;

        switch (hit.Avoidance)
        {
            case HitAvoidance.Missed:
                target.ShowCombatText("Miss", Colors.LightGray, NormalFontSize);

                return;
            case HitAvoidance.Dodged:
                target.ShowCombatText("Dodge", Colors.LightGray, NormalFontSize);

                return;
            case HitAvoidance.Parried:
                target.ShowCombatText("Parry", Colors.LightGray, NormalFontSize);

                return;
        }

        var text     = hit.WasBlocked ? $"Block {hit.FinalDamage:N0}" : hit.FinalDamage.ToString("N0");
        var fontSize = hit.IsCritical ? CriticalFontSize : NormalFontSize;
        var color    = hit.IsCritical ? Colors.DarkOrange : isPlayer ? Colors.Red : Colors.White;

        target.ShowCombatText(text, color, fontSize, hit.FinalDamage);
    }

    public static void ShowStatusTick(this BaseUnit target, StatusTick tick)
        => target.ShowCombatText(tick.Damage.ToString("N0"), GetColorOf(tick.Kind), StatusFontSize, tick.Damage);

    public static void ShowStatusStarted(this BaseUnit target, StatusEffectKind kind)
        => target.ShowCombatText(GetNameOf(kind), GetColorOf(kind), StatusFontSize, offset: target.CombatTextOffset + StatusNameLift);

    public static void ShowHeal(this BaseUnit target, float amount, Vector2 offset)
        => target.ShowCombatText(amount.ToString("N0"), Colors.LimeGreen, NormalFontSize, (int)amount, offset);

    public static void ShowCombatText(this BaseUnit target,
                                      string        text,
                                      Color         color,
                                      int           fontSize,
                                      int           value  = 0,
                                      Vector2?      offset = null)
    {
        var mainScene = target.GetTree()?.CurrentScene;

        if (mainScene is null)
            return;

        //Der Versatz ist reine Darstellung, damit sich Zahlen nicht überdecken, und läuft deshalb nicht über die Zufallsquelle des Spiels
        var jitter = new Vector2((float)GD.RandRange(-JitterPx, JitterPx), (float)GD.RandRange(-JitterPx / 2, JitterPx / 2));

        var floatingCombatText = FCTScene.Instantiate<FloatingCombatText>();
        floatingCombatText.Display      = floatingCombatText.GetNode<Label>(nameof(Label));
        floatingCombatText.Value        = value;
        floatingCombatText.Position     = target.GlobalPosition + (offset ?? target.CombatTextOffset) + jitter;
        floatingCombatText.Display.Text = text;

        floatingCombatText.SetFontColor(color);
        floatingCombatText.SetFontSize(fontSize);
        floatingCombatText.Show();

        mainScene.AddChild(floatingCombatText);
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
