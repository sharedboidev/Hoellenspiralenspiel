using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

public static class EnemyStats
{
    //Die Werte, die jede Einheit hat, setzt UnitBaseValues. Hier kommt dazu, was den Gegner auf seinem Level ausmacht
    public static void Apply(StatSheet sheet, EnemyDefinition definition, int level, IEnumerable<MonsterModDefinition> mods = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(definition);

        sheet.Update(stats =>
        {
            stats.SetBase(CombatStat.Strength, definition.Strength.GetAt(level));
            stats.SetBase(CombatStat.Dexterity, definition.Dexterity.GetAt(level));
            stats.SetBase(CombatStat.Intelligence, definition.Intelligence.GetAt(level));
            stats.SetBase(CombatStat.Constitution, definition.Constitution.GetAt(level));
            stats.SetBase(CombatStat.Awareness, definition.Awareness.GetAt(level));
            stats.SetBase(CombatStat.Life, definition.LifeBonus);
            stats.SetBase(CombatStat.Movementspeed, definition.Movementspeed);
            stats.SetBase(CombatStat.Armor, definition.Armor);
            stats.SetBase(CombatStat.Dodge, definition.Dodge);
            stats.SetBase(CombatStat.FireResistance, definition.FireResistance);
            stats.SetBase(CombatStat.FrostResistance, definition.FrostResistance);
            stats.SetBase(CombatStat.LightningResistance, definition.LightningResistance);

            foreach (var item in definition.Equipment)
                stats.AddModifiers(new ItemInstance(item, level).GetEquipModifiers());

            foreach (var mod in mods ?? [])
                stats.AddModifiers(mod.GetStampedModifiers());
        });
    }

    public static WeaponProfile GetWeapon(EnemyDefinition definition, int level)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var wielded = definition.WieldedWeapon;

        return wielded is null ? definition.NaturalWeapon : new ItemInstance(wielded, level).ToWeaponProfile();
    }
}
