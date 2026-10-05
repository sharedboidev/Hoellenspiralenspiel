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
    private const string PriceColor = "gold";

    private const string EquippedNote = "Currently Equipped";
    private const int    NoteFontSize = 20;

    private readonly ItemInstance               item;
    private readonly string                     priceNote;
    private readonly IReadOnlyList<Requirement> unmetRequirements;

    public ItemTooltipContent(ItemInstance item, IReadOnlyList<Requirement> unmetRequirements, string priceNote = null)
    {
        this.item              = item;
        this.unmetRequirements = unmetRequirements ?? [];
        this.priceNote         = priceNote;
    }

    public bool ShowsEquippedNote { get; init; }

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

        if (!string.IsNullOrEmpty(priceNote))
            text.AppendLine($"[color={PriceColor}]{priceNote}[/color]");

        text.Append("[/center]");

        //Unten rechts, damit die Werte beider Tooltips auf gleicher Höhe beginnen
        if (ShowsEquippedNote)
            text.Append($"[right][font_size={NoteFontSize}][color=gray]{EquippedNote}[/color][/font_size][/right]");

        return text.ToString();
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

        foreach (var (element, range) in item.AddedDamage.Entries)
        {
            if (!range.IsEmpty)
                text.AppendLine($"{element} Damage: {Styled(range.Min, 0)} to {Styled(range.Max, 0)}");
        }

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
        var guard     = item.Guard;
        var baseGuard = item.Definition.Guard;

        AppendGuardLine(text, "Block Chance", guard.MeleeBlock, baseGuard.MeleeBlock);
        AppendGuardLine(text, "Spell Block Chance", guard.SpellBlock, baseGuard.SpellBlock);
        AppendGuardLine(text, "Parry Chance", guard.MeleeParry, baseGuard.MeleeParry);
        AppendGuardLine(text, "Spell Parry Chance", guard.SpellParry, baseGuard.SpellParry);
    }

    private static void AppendGuardLine(StringBuilder text, string label, float value, float baseValue)
    {
        if (value > 0f)
            text.AppendLine($"{label}: {Styled(value, baseValue)}%");
    }

    private void AppendRequirements(StringBuilder text)
    {
        foreach (var (requirement, neededValue) in item.Requirements)
        {
            var value = neededValue < item.Definition.Requirements[requirement] ? $"[color={AffixColor}]{neededValue:N0}[/color]" : $"{neededValue:N0}";
            var line  = $"Required {requirement.GetDescription()}: {value}";

            text.AppendLine(unmetRequirements.Contains(requirement) ? $"[color={UnmetColor}]{line}[/color]" : line);
        }
    }

    //Ein hybrider Affix zeigt beide Stats, jeden in seiner Zeile
    private void AppendAffixes(StringBuilder text)
    {
        foreach (var affix in item.Affixes.OrderBy(affix => affix.Type))
        {
            text.AppendLine($"[color={AffixColor}]{Describe(affix.Stat, affix.Modification, affix.Value, affix.ValueTo, affix.IsLocal)}[/color]");

            if (affix.Hybrid is { } hybrid)
                text.AppendLine($"[color={AffixColor}]{Describe(hybrid.Stat, hybrid.Modification, hybrid.Value, 0f, hybrid.IsLocal)}[/color]");
        }
    }

    private static string Describe(CombatStat stat, ModificationType modification, float value, float valueTo, bool isLocal)
        => (stat, modification) switch
        {
            _ when valueTo > 0f                                                   => $"Adds {value:0.##} to {valueTo:0.##} {stat.GetDescription()}",
            (CombatStat.Attackspeed, ModificationType.Flat)                       => $"+{value:0.##} to Attacks per Second",
            (CombatStat.Liferegeneration, ModificationType.Flat)                  => $"Regenerate {value:0.#} Life per second",
            (CombatStat.LifeOnHit, ModificationType.Flat)                         => $"Grants {value:0.##} Life per Enemy Hit",
            (CombatStat.LifeOnKill, ModificationType.Flat)                        => $"Gain {value:0.##} Life per Enemy Killed",
            (CombatStat.ManaOnKill, ModificationType.Flat)                        => $"Gain {value:0.##} Mana per Enemy Killed",
            (CombatStat.Leech, ModificationType.Flat)                             => $"{value:0.##}% of Physical Attack Damage Leeched as Life",
            (CombatStat.ManaLeech, ModificationType.Flat)                         => $"{value:0.##}% of Physical Attack Damage Leeched as Mana",
            (CombatStat.ReflectPhysical, ModificationType.Flat)                   => $"Reflects {value:0.##}% of Physical Damage to Melee Attackers",
            (CombatStat.Damagereduction, ModificationType.Flat)                   => $"{value:0.##}% additional Physical Damage Reduction",
            (CombatStat.AilmentAvoidance, ModificationType.Flat)                  => $"{value:0.##}% chance to Avoid Ailments",
            (CombatStat.ReducedCriticalDamageTaken, ModificationType.Flat)        => $"You take {value:0.##}% reduced Extra Damage from Critical Strikes",
            (CombatStat.ProjectileCount, ModificationType.Flat) when isLocal      => value > 1f ? $"Bow Attacks fire {value:0} additional Arrows" : "Bow Attacks fire an additional Arrow",
            (CombatStat.MeleeBlock, ModificationType.Percentage) when isLocal     => $"{value * 100:N0}% increased Chance to Block",
            (CombatStat.DamageOverTime, ModificationType.More)                    => $"+{value * 100:N0}% to Damage over Time Multiplier",
            (CombatStat.PhysicalDamageOverTime, ModificationType.More)            => $"+{value * 100:N0}% to Physical Damage over Time Multiplier",
            (CombatStat.FireDamageOverTime, ModificationType.More)                => $"+{value * 100:N0}% to Fire Damage over Time Multiplier",
            (_, ModificationType.Flat) when IsShownAsPercent(stat)                => $"+{value:0.##}% to {stat.GetDescription()}",
            (_, ModificationType.Flat)                                            => $"+{value:0.##} to {stat.GetDescription()}",
            (_, ModificationType.Percentage) when value < 0                       => $"{-value * 100:N0}% reduced {stat.GetDescription()}",
            (_, ModificationType.Percentage)                                      => $"{value * 100:N0}% increased {stat.GetDescription()}",
            (_, ModificationType.More)                                            => $"{value * 100:N0}% More {stat.GetDescription()}",
            _                                                                     => throw new ArgumentOutOfRangeException(nameof(modification), modification, null)
        };

    private static bool IsShownAsPercent(CombatStat stat)
        => stat is CombatStat.CriticalHitChance or CombatStat.CriticalDamage or CombatStat.BlockReduction or CombatStat.MeleeBlock or CombatStat.SpellBlock or CombatStat.MeleeParry or CombatStat.SpellParry
                or CombatStat.FireResistance or CombatStat.FrostResistance or CombatStat.LightningResistance or CombatStat.AllElementalResistances
                or CombatStat.MaxFireResistance or CombatStat.MaxFrostResistance or CombatStat.MaxLightningResistance or CombatStat.AllMaximumResistances;

    private static string Styled(double finalValue, double baseValue)
    {
        finalValue = Math.Round(finalValue, 2);
        baseValue  = Math.Round(baseValue, 2);

        if (finalValue < baseValue)
            return $"[color={UnmetColor}]{finalValue}[/color]";

        return finalValue > baseValue ? $"[color={AffixColor}]{finalValue}[/color]" : $"{finalValue}";
    }
}
