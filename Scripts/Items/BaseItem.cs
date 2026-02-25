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

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem
        : Node2D,
          ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public             int                ItemLevel           { get; set; }
    public abstract    bool               IsStackable         { get; }
    public abstract    string             ItembaseName        { get; }
    protected          string             AffixedItembaseName { get; set; }
    protected          string             ExceptionalName     { get; set; }
    protected abstract ItemType           ItemType            { get; }
    protected          List<ItemModifier> ItemModifiers       { get; } = new();

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
        => string.Empty;

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

    protected void SetAffixedItembaseName()
    {
        var prefix = ItemModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Prefix);
        var suffix = ItemModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Suffix);

        AffixedItembaseName = $"{prefix?.ItemnameAddition} " + ItembaseName + $" {suffix?.ItemnameAddition}";
    }

    public void AddModifier(ItemModifier modifier)
        => ItemModifiers.Add(modifier);

    protected virtual void SetExceptionalName() { }

    public virtual void Init() { }

    public override void _Ready()
        => Icon = GetNode<TextureRect>("Icon");
}