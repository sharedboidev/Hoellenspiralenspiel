using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Configuration;
using Hoellenspiralenspiel.Scripts.Models.Weapons;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public abstract partial class BaseWeapon : BaseItem
{
    [Export]
    public int MinDamageBase { get; private set; }

    private float MinDamageAddedFlat            => GetModifierSumOf(ModificationType.Flat, WeaponStat.PhysicalDamage);
    private float MinDamagePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, WeaponStat.PhysicalDamage);
    private float MinDamageMoreMultiplierTotal  => GetTotalMoreMultiplierOf(WeaponStat.PhysicalDamage);
    public  int   MinDamageFinal                => (int)((MinDamageBase + MinDamageAddedFlat) * MinDamagePercentageMultiplier * MinDamageMoreMultiplierTotal);

    [Export]
    public int MaxDamageBase { get; set; }

    private float MaxDamageAddedFlat            => GetModifierSumOf(ModificationType.Flat, WeaponStat.PhysicalDamage);
    private float MaxDamagePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, WeaponStat.PhysicalDamage);
    private float MaxDamageMoreMultiplierTotal  => GetTotalMoreMultiplierOf(WeaponStat.PhysicalDamage);
    public  int   MaxDamageFinal                => (int)((MaxDamageBase + MaxDamageAddedFlat) * MaxDamagePercentageMultiplier * MaxDamageMoreMultiplierTotal);

    [Export]
    public float AttacksPerSecondBase { get; set; } = 1f;

    private float  AttacksPerSecondAddedFlat            => GetModifierSumOf(ModificationType.Flat, WeaponStat.AttackSpeed);
    private float  AttacksPerSecondPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, WeaponStat.AttackSpeed);
    private float  AttacksPerSecondMoreMultiplierTotal  => GetTotalMoreMultiplierOf(WeaponStat.AttackSpeed);
    public  double AttacksPerSecondFinal                => Math.Round((AttacksPerSecondBase + AttacksPerSecondAddedFlat) * AttacksPerSecondPercentageMultiplier * AttacksPerSecondMoreMultiplierTotal, 2);
    public  double SwingCooldownSec                     => 1 / AttacksPerSecondFinal;

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float CriticalHitChanceBase { get; set; }

    private float  CriticalHitChanceAddedFlat            => GetModifierSumOf(ModificationType.Flat, WeaponStat.CriticalHitChance);
    private float  CriticalHitChancePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, WeaponStat.CriticalHitChance);
    private float  CriticalHitChanceMoreMultiplierTotal  => GetTotalMoreMultiplierOf(WeaponStat.CriticalHitChance);
    public  double CriticalHitChanceFinal                => Math.Round((CriticalHitChanceBase + CriticalHitChanceAddedFlat) * CriticalHitChancePercentageMultiplier * CriticalHitChanceMoreMultiplierTotal, 2);

    [Export]
    public WeaponType WeaponType { get; set; }

    [Export]
    public WieldStrategy WieldStrategie { get; set; }

    [Export]
    public Godot.Collections.Dictionary<Requirement, int> Requirements { get; set; } = new();

    public          List<WeaponStatModifier> WeaponStatModifiers { get; } = new();
    public          DamageType               DamageType          { get; private set; }
    public override bool                     IsStackable         => false;

    protected override bool IsMagic
    {
        get
        {
            var prefixAmount = WeaponStatModifiers.Count(mod => mod.AffixType == AffixType.Prefix);

            if (prefixAmount > 1)
                return false;

            var suffixAmount = WeaponStatModifiers.Count(mod => mod.AffixType == AffixType.Suffix);

            if (suffixAmount > 1)
                return false;

            return prefixAmount + suffixAmount is <= 2 and > 0;
        }
    }

    public bool CanBeEquipedBy(Player2D player)
    {
        var canWield = true;

        foreach (var requirement in Requirements)
        {
            var requiredAttributevalue = player.GetRequiredAttributevalue(requirement.Key);

            canWield &= requirement.Value <= requiredAttributevalue;
        }

        return canWield;
    }

    public override void Init()
    {
        base.Init();

        SetDamagetypeByWeapon();
        RollModifiers();
        SetAffixedItembaseName();
        SetUniqueName();
    }

    private void RollModifiers()
    {
        var normalizedAffixCount = GetNormalizedAffixAmount();
        var rng                  = new Random();

        for (var i = 0; i < normalizedAffixCount; i++)
        {
            var nextAffixToRoll = rng.Next(0, 2) == 0 ? AffixType.Prefix : AffixType.Suffix;
            var newAffix        = AffixDispenser.GetWeaponAffix(nextAffixToRoll);

            WeaponStatModifiers.Add(newAffix);
        }
    }

    private int GetNormalizedAffixAmount()
    {
        var possibleAffixProbabilityCeiling = FindAffixProbability();

        var maximumAffixes = 8;
        var totalAffixes   = new Random().Next(0, possibleAffixProbabilityCeiling + 1);

        return Math.Min(totalAffixes, maximumAffixes);
    }

    private int FindAffixProbability()
        => ItemLevel switch
        {
            >= 0 and <= 10 => 3,
            <= 25          => 5,
            <= 40          => 6,
            <= 50          => 7,
            <= 60          => 8,
            <= 70          => 9,
            <= 80          => 10,
            <= 90          => 11,
            <= 100         => 12,
            _              => throw new ArgumentOutOfRangeException()
        };

    protected override void SetUniqueName() { }

    protected override void SetAffixedItembaseName()
    {
        var prefix = WeaponStatModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Prefix);
        var suffix = WeaponStatModifiers.FirstOrDefault(mod => mod.AffixType == AffixType.Suffix);
        
        var prefixName = prefix is null ? string.Empty : AffixDispenser.ItemnameMap[prefix.WeaponStat];
        var suffixName = suffix is null ? string.Empty : AffixDispenser.ItemnameMap[suffix.WeaponStat];

        AffixedItembaseName = prefixName + ItembaseName + suffixName;
    }

    public override string GetTooltipDescription()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        AppendStats(emil);
        AppendRequirements(emil);
        AppendAffixes(emil);

        emil.Append("[/center]");

        return emil.ToString();
    }

    private void AppendAffixes(StringBuilder emil)
    {
        foreach (var affix in WeaponStatModifiers.OrderBy(a => a.AffixType))
        {
            switch (affix.ModificationType)
            {
                case ModificationType.Flat when affix.WeaponStat == WeaponStat.AttackSpeed:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##} to Attacks per Second[/color]");

                    break;
                case ModificationType.Flat when affix.WeaponStat == WeaponStat.CriticalHitChance:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##}% to Critical Hit Chance[/color]");

                    break;
                case ModificationType.Flat:
                    emil.AppendLine($"[color=dodger_blue]+{affix.Value:0.##} to {affix.WeaponStat.GetDescription()}[/color]");

                    break;
                case ModificationType.Percentage:
                    emil.AppendLine($"[color=dodger_blue]{affix.Value * 100:N0}% increased {affix.WeaponStat.GetDescription()}[/color]");

                    break;
                case ModificationType.More:
                    emil.AppendLine($"[color=dodger_blue]{affix.Value * 100:N0}% More {affix.WeaponStat.GetDescription()}[/color]");

                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void AppendStats(StringBuilder emil)
    {
        emil.AppendLine($"{WieldStrategie.GetDescription()} {WeaponType.GetDescription()}");
        emil.AppendLine($"{DamageType.Name} Damage: {GetStyledValue(MinDamageFinal, MinDamageBase):N0} to {GetStyledValue(MaxDamageFinal, MaxDamageBase):N0}");
        emil.AppendLine($"Attacks per Second: {GetStyledValue(AttacksPerSecondFinal, AttacksPerSecondBase):0.##}");
        emil.AppendLine($"Critical Hit Chance: {GetStyledValue(CriticalHitChanceFinal, CriticalHitChanceBase):0.##}%");
    }

    private void AppendRequirements(StringBuilder emil)
    {
        foreach (var requirement in Requirements)
            emil.AppendLine($"Required {requirement.Key.GetDescription()}: {requirement.Value:N0}");
    }

    private string GetStyledValue(double finalValue, double baseValue)
    {
        finalValue = Math.Round(finalValue, 2);
        baseValue  = Math.Round(baseValue, 2);

        if (finalValue < baseValue)
            return $"[color=firebrick]{finalValue}[/color]";

        if (finalValue > baseValue)
            return $"[color=dodger_blue]{finalValue}[/color]";

        return $"{finalValue}";
    }

    private float GetTotalMoreMultiplierOf(WeaponStat weaponStat)
    {
        var totalMoreMultiplier = 1f;

        foreach (var modifier in GetModifierOf(ModificationType.More, weaponStat))
            totalMoreMultiplier *= 1 + modifier.Value;

        return totalMoreMultiplier;
    }

    private float GetModifierSumOf(ModificationType modificationType, WeaponStat weaponStat)
        => GetModifierOf(modificationType, weaponStat).Sum(mod => mod.Value);

    private IEnumerable<WeaponStatModifier> GetModifierOf(ModificationType modificationType, WeaponStat weaponStat)
        => WeaponStatModifiers.Where(mod => mod.WeaponStat == weaponStat &&
                                            mod.ModificationType == modificationType);

    private void SetDamagetypeByWeapon()
        => DamageType = WeaponType switch
        {
            WeaponType.Sword => new SlashDamage(),
            WeaponType.Axe   => new SlashDamage(),
            WeaponType.Flail => new CrushDamage(),
            WeaponType.Staff => new CrushDamage(),
            WeaponType.Bow   => new PierceDamage(),
            _                => null
        };
}