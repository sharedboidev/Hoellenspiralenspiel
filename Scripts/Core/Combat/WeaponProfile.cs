using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Die Werte einer Waffe, mit denen der Kampf rechnet. Lokale Modifier der Waffe sind bereits enthalten.
//Die Reichweite ist in Welteinheiten angegeben
public sealed record WeaponProfile(float      MinDamage,
                                   float      MaxDamage,
                                   float      AttacksPerSecond,
                                   float      CriticalHitChance,
                                   DamageType DamageType,
                                   float      Range)
{
    public const float DefaultMeleeRange = 100f;

    //Gilt, solange keine Waffe angelegt ist
    public static WeaponProfile Unarmed { get; } = new(1, 3, 1.2f, 5, DamageType.Crush, DefaultMeleeRange);

    //Angriffstempo und Krit-Chance der Waffe sind die Grundwerte im Stat-Blatt. Globale Modifier wirken darauf
    public void ApplyTo(StatSheet stats)
        => stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Attackspeed, AttacksPerSecond);
            sheet.SetBase(CombatStat.CriticalHitChance, CriticalHitChance);
        });
}
