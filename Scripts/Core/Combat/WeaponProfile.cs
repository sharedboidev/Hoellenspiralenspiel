using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record WeaponProfile(float      MinDamage,
                                   float      MaxDamage,
                                   float      AttacksPerSecond,
                                   float      CriticalHitChance,
                                   DamageType DamageType,
                                   float      Range,
                                   bool       IsRanged        = false,
                                   float      ProjectileSpeed = 0f)
{
    public const float DefaultMeleeRange = 100f;

    //Der Treffer landet noch, wenn das Ziel während des Ausholens ein Stück aus der Reichweite gerückt ist
    public const float RangeTolerance = 1.25f;

    public static WeaponProfile Unarmed { get; } = new(1, 3, 1.2f, 5, DamageType.Crush, DefaultMeleeRange);

    public float Reach => Range * RangeTolerance;

    public void ApplyTo(StatSheet stats)
        => stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Attackspeed, AttacksPerSecond);
            sheet.SetBase(CombatStat.CriticalHitChance, CriticalHitChance);
        });

    public ProjectileSettings GetProjectile()
    {
        if (!IsRanged || ProjectileSpeed <= 0f)
            return null;

        return new ProjectileSettings(ProjectileSpeed, Reach / ProjectileSpeed);
    }
}
