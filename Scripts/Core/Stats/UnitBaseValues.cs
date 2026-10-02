using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

//Diese Grundwerte hat jede Einheit, bevor Held oder Gegner ihre eigenen setzen
public static class UnitBaseValues
{
    public static void Apply(StatSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        sheet.Update(stats =>
        {
            stats.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            stats.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
            stats.SetBase(CombatStat.BlockReduction, CombatRules.BaseBlockReduction);
            stats.SetBase(CombatStat.ProjectileCount, 1);
        });
    }
}
