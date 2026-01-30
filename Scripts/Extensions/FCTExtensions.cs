using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Extensions;

public static class FCTExtensions
{
    private static readonly PackedScene FCTScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/floating_combat_text.tscn");

    public static void InstatiateFloatingCombatText(this BaseUnit target, HitResult hitResult, Node currentScene, Vector2 offset = default)
    {
        var floatingCombatTextInstance = FCTScene.Instantiate<FloatingCombatText>();
        floatingCombatTextInstance.Display      = floatingCombatTextInstance.GetNode<Label>(nameof(Label));
        floatingCombatTextInstance.Value        = (int)hitResult.Value;
        floatingCombatTextInstance.Position     = target.Position + offset;
        floatingCombatTextInstance.Display.Text = hitResult.Value.ToString("N0");

        floatingCombatTextInstance = hitResult.LifeModificationMode switch
        {
            LifeModificationMode.Damage when target is Player2D && hitResult.HitType is HitType.Critical => floatingCombatTextInstance.AsDamageReceivedCritical(),
            LifeModificationMode.Damage when target is Player2D => floatingCombatTextInstance.AsDamageReceived(),
            LifeModificationMode.Damage when hitResult.HitType is HitType.Normal => floatingCombatTextInstance.AsDamageDealt(),
            LifeModificationMode.Damage when hitResult.HitType is HitType.Critical => floatingCombatTextInstance.AsDamageDealtCritical(),
            LifeModificationMode.Heal when hitResult.HitType is HitType.Normal => floatingCombatTextInstance.AsHeal(),
            LifeModificationMode.Heal when hitResult.HitType is HitType.Critical => floatingCombatTextInstance.AsHealCritical(),
            _ => floatingCombatTextInstance
        };

        floatingCombatTextInstance.Show();

        currentScene.AddChild(floatingCombatTextInstance);
    }

    private static FloatingCombatText AsHeal(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.LimeGreen);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    private static FloatingCombatText AsHealCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkGreen);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    private static FloatingCombatText AsDamageDealt(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.White);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    private static FloatingCombatText AsDamageDealtCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkOrange);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    private static FloatingCombatText AsDamageReceived(this FloatingCombatText floatingCombatText, int fontSize = 36)
    {
        floatingCombatText.SetFontColor(Colors.Red);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }

    private static FloatingCombatText AsDamageReceivedCritical(this FloatingCombatText floatingCombatText, int fontSize = 42)
    {
        floatingCombatText.SetFontColor(Colors.DarkOrange);
        floatingCombatText.SetFontSize(fontSize);

        return floatingCombatText;
    }
}