using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

public sealed class ItemTooltipContent : ITooltipObject
{
    private const string AffixColor = "dodger_blue";
    private const string RareColor  = "yellow";
    private const string UnmetColor = "firebrick";

    private readonly ItemInstance               item;
    private readonly IReadOnlyList<Requirement> unmetRequirements;

    public ItemTooltipContent(ItemInstance item, IReadOnlyList<Requirement> unmetRequirements)
    {
        this.item              = item;
        this.unmetRequirements = unmetRequirements ?? [];
    }

    public string GetTooltipTitle()
    {
        var text = new StringBuilder("[center]");

        switch (item.Rarity)
        {
            case ItemRarity.Magic:
                text.AppendLine($"[color={AffixColor}]{item.AffixedName}[/color]");

                break;
            case ItemRarity.Rare:
                text.AppendLine($"[color={RareColor}]{item.RareName}[/color]");
                text.AppendLine($"[color={RareColor}]{item.Definition.Name}[/color]");

                break;
            default:
                text.AppendLine(item.Definition.Name);

                break;
        }

        return text.Append("[/center]").ToString();
    }

    public string GetTooltipDescription()
    {
        var text = new StringBuilder("[center]");

        AppendBaseStats(text);
        AppendGuard(text);
        AppendRequirements(text);
        AppendAffixes(text);

        return text.Append("[/center]").ToString();
    }

    private void AppendBaseStats(StringBuilder text)
    {
        var definition = item.Definition;

        switch (definition.Kind)
        {
            case ItemKind.Weapon:
                AppendWeaponStats(text, definition.Weapon);

                break;
            case ItemKind.Armor:
                if (definition.IsShield)
                    text.AppendLine("Shield");

                text.AppendLine($"Armor: {Styled(item.ArmorValue, definition.Armor)}");

                break;
            case ItemKind.Consumable:
                AppendConsumableEffect(text, definition.Consumable);

                break;
        }
    }

    private void AppendWeaponStats(StringBuilder text, WeaponStats weapon)
    {
        text.AppendLine($"{weapon.WieldStrategy.GetDescription()} {weapon.WeaponType.GetDescription()}");
        text.AppendLine($"{weapon.DamageType} Damage: {Styled(item.MinDamage, weapon.MinDamage)} to {Styled(item.MaxDamage, weapon.MaxDamage)}");
        text.AppendLine($"Attacks per Second: {Styled(item.AttacksPerSecond, weapon.AttacksPerSecond)}");
        text.AppendLine($"Critical Hit Chance: {Styled(item.CriticalHitChance, weapon.CriticalHitChance)}%");

        if (weapon.IsRanged)
            text.AppendLine($"Range: {weapon.Range:N0}");
    }

    private static void AppendConsumableEffect(StringBuilder text, ConsumableEffect effect)
    {
        if (effect.Kind == ConsumableEffectKind.RestoreMana)
            text.AppendLine($"Recovered Mana: [color=royal_blue]{effect.Percent:N0}[/color]%");
        else
            text.AppendLine($"Recovered Life: [color=lime_green]{effect.Percent:N0}[/color]%");
    }

    private void AppendGuard(StringBuilder text)
    {
        var guard = item.Definition.Guard;

        AppendGuardLine(text, "Block Chance", guard.MeleeBlock);
        AppendGuardLine(text, "Spell Block Chance", guard.SpellBlock);
        AppendGuardLine(text, "Parry Chance", guard.MeleeParry);
        AppendGuardLine(text, "Spell Parry Chance", guard.SpellParry);
    }

    private static void AppendGuardLine(StringBuilder text, string label, float value)
    {
        if (value > 0f)
            text.AppendLine($"{label}: {value:0.##}%");
    }

    private void AppendRequirements(StringBuilder text)
    {
        foreach (var (requirement, neededValue) in item.Definition.Requirements)
        {
            var line = $"Required {requirement.GetDescription()}: {neededValue:N0}";

            text.AppendLine(unmetRequirements.Contains(requirement) ? $"[color={UnmetColor}]{line}[/color]" : line);
        }
    }

    private void AppendAffixes(StringBuilder text)
    {
        foreach (var affix in item.Affixes.OrderBy(affix => affix.Type))
            text.AppendLine($"[color={AffixColor}]{Describe(affix)}[/color]");
    }

    private static string Describe(ItemAffix affix)
        => affix.Modification switch
        {
            ModificationType.Flat when affix.Stat == CombatStat.Attackspeed => $"+{affix.Value:0.##} to Attacks per Second",
            ModificationType.Flat when IsShownAsPercent(affix.Stat)         => $"+{affix.Value:0.##}% to {affix.Stat.GetDescription()}",
            ModificationType.Flat                                           => $"+{affix.Value:0.##} to {affix.Stat.GetDescription()}",
            ModificationType.Percentage                                     => $"{affix.Value * 100:N0}% increased {affix.Stat.GetDescription()}",
            ModificationType.More                                           => $"{affix.Value * 100:N0}% More {affix.Stat.GetDescription()}",
            _                                                               => throw new ArgumentOutOfRangeException(nameof(affix), affix.Modification, null)
        };

    private static bool IsShownAsPercent(CombatStat stat)
        => stat is CombatStat.CriticalHitChance or CombatStat.BlockReduction or CombatStat.MeleeBlock or CombatStat.SpellBlock or CombatStat.MeleeParry or CombatStat.SpellParry;

    private static string Styled(double finalValue, double baseValue)
    {
        finalValue = Math.Round(finalValue, 2);
        baseValue  = Math.Round(baseValue, 2);

        if (finalValue < baseValue)
            return $"[color={UnmetColor}]{finalValue}[/color]";

        return finalValue > baseValue ? $"[color={AffixColor}]{finalValue}[/color]" : $"{finalValue}";
    }
}
