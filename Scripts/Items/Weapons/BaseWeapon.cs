using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Configuration;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public abstract partial class BaseWeapon : BaseItem
{
    [Export]
    public int MinDamageBase { get; private set; }

    private float MinDamageAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.PhysicalDamage);
    private float MinDamagePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.PhysicalDamage);
    private float MinDamageMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.PhysicalDamage);
    public  int   MinDamageFinal                => (int)((MinDamageBase + MinDamageAddedFlat) * MinDamagePercentageMultiplier * MinDamageMoreMultiplierTotal);

    [Export]
    public int MaxDamageBase { get; set; }

    private float MaxDamageAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.PhysicalDamage);
    private float MaxDamagePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.PhysicalDamage);
    private float MaxDamageMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.PhysicalDamage);
    public  int   MaxDamageFinal                => (int)((MaxDamageBase + MaxDamageAddedFlat) * MaxDamagePercentageMultiplier * MaxDamageMoreMultiplierTotal);

    [Export]
    public float AttacksPerSecondBase { get; set; } = 1f;

    private float  AttacksPerSecondAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Attackspeed);
    private float  AttacksPerSecondPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Attackspeed);
    private float  AttacksPerSecondMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Attackspeed);
    public  double AttacksPerSecondFinal                => Math.Round((AttacksPerSecondBase + AttacksPerSecondAddedFlat) * AttacksPerSecondPercentageMultiplier * AttacksPerSecondMoreMultiplierTotal, 2);
    public  double SwingCooldownSec                     => 1 / AttacksPerSecondFinal;

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float CriticalHitChanceBase { get; set; }

    private float  CriticalHitChanceAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.CriticalHitChance);
    private float  CriticalHitChancePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.CriticalHitChance);
    private float  CriticalHitChanceMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.CriticalHitChance);
    public  double CriticalHitChanceFinal                => Math.Round((CriticalHitChanceBase + CriticalHitChanceAddedFlat) * CriticalHitChancePercentageMultiplier * CriticalHitChanceMoreMultiplierTotal, 2);

    [Export]
    public WeaponType WeaponType { get; set; }

    [Export]
    public WieldStrategy WieldStrategie { get; set; }

    public          DamageType DamageType  { get; private set; }
    public override bool       IsStackable => false;
    public override ItemType   ItemType    => ItemType.Weapon;

    public override void Init()
    {
        base.Init();

        SetDamagetypeByWeapon();
        SetAffixedItembaseName();
        SetExceptionalName();
    }

    protected override void SetExceptionalName()
        => ExceptionalName = NameGenerator.GenerateRareWeapon();

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

    private float GetTotalMoreMultiplierOf(CombatStat combatStat)
    {
        var totalMoreMultiplier = 1f;

        foreach (var modifier in GetModifierOf(ModificationType.More, combatStat))
            totalMoreMultiplier *= 1 + modifier.Value;

        return totalMoreMultiplier;
    }

    private float GetModifierSumOf(ModificationType modificationType, CombatStat combatStat)
        => GetModifierOf(modificationType, combatStat).Sum(mod => mod.Value);

    private IEnumerable<ItemModifier> GetModifierOf(ModificationType modificationType, CombatStat combatStat)
        => ItemModifiers.Where(mod => mod.CombatStat == combatStat &&
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