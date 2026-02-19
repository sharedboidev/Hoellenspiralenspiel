using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Configuration;
using Hoellenspiralenspiel.Scripts.Units;

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
    public WeaponTypes WeaponType { get; set; }

    [Export]
    public WieldStrategies WieldStrategie { get; set; }

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float CriticalHitChance { get; set; }

    [Export]
    public Dictionary<Requirements, int> Requirements { get; set; } = new();

    public          DamageType DamageType  { get; private set; }
    public override bool       IsStackable => false;

    public override void _Ready()
    {
        base._Ready();

        SetDamagetypeByWeapon();
    }

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

    private void SetDamagetypeByWeapon() => DamageType = WeaponType switch
    {
        WeaponTypes.Sword => new SlashDamage(),
        WeaponTypes.Axe => new SlashDamage(),
        WeaponTypes.Flail => new CrushDamage(),
        WeaponTypes.Staff => new CrushDamage(),
        WeaponTypes.Bow => new PierceDamage(),
        _ => null
    };
}