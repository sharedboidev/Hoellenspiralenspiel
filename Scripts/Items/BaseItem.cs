using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Models.Weapons;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem
        : Node2D,
          ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public          int                ItemLevel           { get; set; } = 1;
    public abstract bool               IsStackable         { get; }
    public abstract string             ItembaseName        { get; }
    protected       string             AffixedItembaseName { get; set; }
    protected       string             ExceptionalName     { get; set; }
    public abstract ItemSlot           ItemSlot            { get; }
    protected       List<ItemModifier> ItemModifiers       { get; } = new();

    [Export]
    public Godot.Collections.Dictionary<Requirement, int> Requirements { get; set; } = new();

    protected virtual bool IsMagic
    {
        get
        {
            var prefixAmount = ItemModifiers.Count(mod => mod.AffixType == AffixType.Prefix);

            if (prefixAmount > 1)
                return false;

            var suffixAmount = ItemModifiers.Count(mod => mod.AffixType == AffixType.Suffix);

            if (suffixAmount > 1)
                return false;

            return prefixAmount + suffixAmount is <= 2 and > 0;
        }
    }

    protected virtual bool IsRare
    {
        get
        {
            var prefixAmount = ItemModifiers.Count(mod => mod.AffixType == AffixType.Prefix);

            if (prefixAmount > 1)
                return true;

            var suffixAmount = ItemModifiers.Count(mod => mod.AffixType == AffixType.Suffix);

            if (suffixAmount > 1)
                return true;

            return prefixAmount + suffixAmount > 2;
        }
    }

    public virtual string GetTooltipDescription()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        AppendItembaseStats(emil);
        AppendRequirements(emil);
        AppendAffixes(emil);

        emil.Append("[/center]");

        return emil.ToString();
    }

    private void AppendAffixes(StringBuilder emil)
    {
        foreach (var affix in ItemModifiers.OrderBy(a => a.AffixType))
        {
            switch (affix.ModificationType)
            {
                case ModificationType.Flat when affix.CombatStat == CombatStat.Attackspeed:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##} to Attacks per Second[/color]");

                    break;
                case ModificationType.Flat when affix.CombatStat == CombatStat.CriticalHitChance:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##}% to Critical Hit Chance[/color]");

                    break;
                case ModificationType.Flat:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##} to {affix.CombatStat.GetDescription()}[/color]");

                    break;
                case ModificationType.Percentage:
                    emil.AppendLine($"[color=dodger_blue]{affix.Value * 100:N0}% increased {affix.CombatStat.GetDescription()}[/color]");

                    break;
                case ModificationType.More:
                    emil.AppendLine($"[color=dodger_blue]{affix.Value * 100:N0}% More {affix.CombatStat.GetDescription()}[/color]");

                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected abstract void AppendItembaseStats(StringBuilder emil);

    private void AppendRequirements(StringBuilder emil)
    {
        foreach (var requirement in Requirements)
            emil.AppendLine($"Required {requirement.Key.GetDescription()}: {requirement.Value:N0}");
    }

    public virtual void Init()
    {
        SetAffixedItembaseName();
        SetExceptionalName();
    }

    public void AddModifier(ItemModifier modifier)
        => ItemModifiers.Add(modifier);

    public bool CanBeEquipedBy(Player2D player)
    {
        if (player is null)
            return false;

        var canWield = true;

        foreach (var requirement in Requirements)
        {
            var requiredAttributevalue = player.GetRequiredAttributevalue(requirement.Key);

            canWield &= requirement.Value <= requiredAttributevalue;
        }

        return canWield;
    }
    public virtual string GetTooltipTitle()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        if (IsMagic)
            emil.AppendLine($"[color=dodger_blue]{AffixedItembaseName}[/color]");
        else
        {
            if (IsRare)
            {
                emil.AppendLine($"[color=yellow]{ExceptionalName}[/color]");
                emil.AppendLine($"[color=yellow]{ItembaseName}[/color]");
            }
            else
                emil.AppendLine($"{ItembaseName}");
        }

        emil.Append("[/center]");

        return emil.ToString();
    }

    protected string GetStyledValue(double finalValue, double baseValue)
    {
        finalValue = Math.Round(finalValue, 2);
        baseValue  = Math.Round(baseValue, 2);

        if (finalValue < baseValue)
            return $"[color=firebrick]{finalValue}[/color]";

        if (finalValue > baseValue)
            return $"[color=dodger_blue]{finalValue}[/color]";

        return $"{finalValue}";
    }


    protected void SetAffixedItembaseName()
    {
        var prefix = ItemModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Prefix);
        var suffix = ItemModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Suffix);

        AffixedItembaseName = $"{prefix?.ItemnameAddition} " + ItembaseName + $" {suffix?.ItemnameAddition}";
    }

    protected virtual void SetExceptionalName() { }


    protected float GetTotalMoreMultiplierOf(CombatStat combatStat)
    {
        var totalMoreMultiplier = 1f;

        foreach (var modifier in GetModifierOf(ModificationType.More, combatStat))
            totalMoreMultiplier *= 1 + modifier.Value;

        return totalMoreMultiplier;
    }

    protected float GetModifierSumOf(ModificationType modificationType, CombatStat combatStat)
        => GetModifierOf(modificationType, combatStat).Sum(mod => mod.Value);

    private IEnumerable<ItemModifier> GetModifierOf(ModificationType modificationType, CombatStat combatStat)
        => ItemModifiers.Where(mod => mod.CombatStat == combatStat &&
                                      mod.ModificationType == modificationType);
    public override void _Ready()
        => Icon = GetNode<TextureRect>("Icon");
}