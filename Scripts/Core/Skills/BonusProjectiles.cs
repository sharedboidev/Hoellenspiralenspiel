using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Geschosse über das erste hinaus. Der Stat Projectiles gilt für jeden Skill mit mehreren Geschossen, auch für die Kugeln eines Schlags.
//Die weiteren Pfeile eines Bogens zählen nur für seine eigenen Angriffe
public static class BonusProjectiles
{
    public static int FromStats(StatSheet stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        return Math.Max(1, stats.GetFinalWhole(CombatStat.ProjectileCount)) - 1;
    }

    public static int ForBow(StatSheet stats, WeaponProfile weapon)
        => FromStats(stats) + Math.Max(0, weapon?.ExtraProjectiles ?? 0);
}
