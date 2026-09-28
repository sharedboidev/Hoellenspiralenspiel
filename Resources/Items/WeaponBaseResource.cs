using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Items;

[GlobalClass]
public partial class WeaponBaseResource : EquippableBaseResource
{
    [ExportGroup("Weapon")]
    [Export]
    public ItemSlot Slot { get; set; } = ItemSlot.PhysicalWeapon;

    [Export]
    public WeaponType WeaponType { get; set; }

    [Export]
    public WieldStrategy WieldStrategy { get; set; }

    [Export]
    public float MinDamage { get; set; }

    [Export]
    public float MaxDamage { get; set; }

    [Export]
    public float AttacksPerSecond { get; set; } = 1f;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float CriticalHitChance { get; set; }

    [Export]
    public float Range { get; set; } = WeaponProfile.DefaultMeleeRange;

    //Mit einer Szene ist die Waffe eine Fernkampfwaffe
    [Export]
    public PackedScene ProjectileScene { get; set; }

    [Export]
    public float ProjectileSpeed { get; set; } = 1400f;

    protected override ItemDefinition CreateBaseDefinition()
    {
        var weapon = new WeaponStats(MinDamage, MaxDamage, AttacksPerSecond, CriticalHitChance, WeaponType, WieldStrategy)
        {
            Range           = Range,
            IsRanged        = ProjectileScene is not null,
            ProjectileSpeed = ProjectileSpeed
        };

        return ItemDefinition.ForWeapon(Id, DisplayName, Slot, weapon) with { Guard = Guard };
    }
}
