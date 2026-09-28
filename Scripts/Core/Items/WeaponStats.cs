using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed record WeaponStats(float         MinDamage,
                                 float         MaxDamage,
                                 float         AttacksPerSecond,
                                 float         CriticalHitChance,
                                 WeaponType    WeaponType,
                                 WieldStrategy WieldStrategy)
{
    public float Range           { get; init; } = WeaponProfile.DefaultMeleeRange;
    public bool  IsRanged        { get; init; }
    public float ProjectileSpeed { get; init; }

    public DamageType DamageType => WeaponType switch
    {
        WeaponType.Sword  => DamageType.Slash,
        WeaponType.Axe    => DamageType.Slash,
        WeaponType.Bow    => DamageType.Pierce,
        WeaponType.Dagger => DamageType.Pierce,
        _                 => DamageType.Crush
    };
}
