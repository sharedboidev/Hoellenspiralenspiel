using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Die Werte einer Waffe, mit denen der Kampf rechnet. Lokale Modifier der Waffe sind bereits enthalten.
//Die Reichweite ist in Welteinheiten angegeben. Fernkampfwaffen schießen ein Projektil mit ProjectileSpeed
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

    //Gilt, solange keine Waffe angelegt ist
    public static WeaponProfile Unarmed { get; } = new(1, 3, 1.2f, 5, DamageType.Crush, DefaultMeleeRange);

    //Bis zu diesem Abstand trifft die Waffe
    public float Reach => Range * RangeTolerance;

    //Angriffstempo und Krit-Chance der Waffe sind die Grundwerte im Stat-Blatt. Globale Modifier wirken darauf
    public void ApplyTo(StatSheet stats)
        => stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Attackspeed, AttacksPerSecond);
            sheet.SetBase(CombatStat.CriticalHitChance, CriticalHitChance);
        });

    //Das Projektil einer Fernkampfwaffe fliegt so weit, wie die Waffe reicht
    public ProjectileSettings GetProjectile()
    {
        if (!IsRanged || ProjectileSpeed <= 0f)
            return null;

        return new ProjectileSettings(ProjectileSpeed, Reach / ProjectileSpeed);
    }
}
