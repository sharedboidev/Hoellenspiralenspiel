using System.Text;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Configuration;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public abstract partial class BaseWeapon : BaseItem
{
    [Export]
    public int MinDamage { get; private set; }

    [Export]
    public int MaxDamage { get; set; }

    [Export]
    public float SwingCooldownSec { get; set; } = 1f;

    public float AttacksPerSecond => 1 / SwingCooldownSec;

    [Export]
    public WeaponType WeaponType { get; set; }

    [Export]
    public WieldStrategy WieldStrategie { get; set; }

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float CriticalHitChance { get; set; }

    [Export]
    public Dictionary<Requirement, int> Requirements { get; set; } = new();

    public          DamageType DamageType  { get; private set; }
    public override bool       IsStackable => false;

    public bool CanBeEquipedBy(Player2D player)
    {
        var canWear = true;

        foreach (var requirement in Requirements)
        {
            var requiredAttributevalue = player.GetRequiredAttributevalue(requirement.Key);

            canWear &= requirement.Value <= requiredAttributevalue;
        }

        return canWear;
    }

    public override void Init()
    {
        base.Init();

        SetDamagetypeByWeapon();
        SetExceptionalName();
    }

    public override string GetTooltipDescription()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");
        emil.AppendLine($"{WieldStrategie.GetDescription()} {WeaponType.GetDescription()}");
        emil.AppendLine($"{DamageType.Name} Damage: {MinDamage:N0} To {MaxDamage:N0}");
        emil.AppendLine($"Attacks per Second: {AttacksPerSecond:N}");
        emil.AppendLine($"Critical Hit Chance: {CriticalHitChance:N2}%");

        foreach (var requirement in Requirements)
            emil.AppendLine($"Required {requirement.Key.GetDescription()}: {requirement.Value:N0}");

        emil.Append("[/center]");

        return emil.ToString();
    }

    private void SetDamagetypeByWeapon() => DamageType = WeaponType switch
    {
        WeaponType.Sword => new SlashDamage(),
        WeaponType.Axe => new SlashDamage(),
        WeaponType.Flail => new CrushDamage(),
        WeaponType.Staff => new CrushDamage(),
        WeaponType.Bow => new PierceDamage(),
        _ => null
    };
}