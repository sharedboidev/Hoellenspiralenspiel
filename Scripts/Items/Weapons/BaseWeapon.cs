using System;
using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Configuration;
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
    public override ItemSlot   ItemSlot    => ItemSlot.Weapon;

    public override void Init()
    {
        base.Init();

        SetDamagetypeByWeapon();
    }

    protected override void SetExceptionalName()
        => ExceptionalName = NameGenerator.GenerateRareWeapon();

    protected override void AppendItembaseStats(StringBuilder emil)
    {
        emil.AppendLine($"{WieldStrategie.GetDescription()} {WeaponType.GetDescription()}");
        emil.AppendLine($"{DamageType.Name} Damage: {GetStyledValue(MinDamageFinal, MinDamageBase):N0} to {GetStyledValue(MaxDamageFinal, MaxDamageBase):N0}");
        emil.AppendLine($"Attacks per Second: {GetStyledValue(AttacksPerSecondFinal, AttacksPerSecondBase):0.##}");
        emil.AppendLine($"Critical Hit Chance: {GetStyledValue(CriticalHitChanceFinal, CriticalHitChanceBase):0.##}%");
    }

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