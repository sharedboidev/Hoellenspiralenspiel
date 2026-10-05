using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public static class HitRequests
{
    public static HitRequest ForAttack(StatSheet attacker, WeaponProfile weapon, AttackDefinition attack)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(attack);

        var damageType  = attack.DealtAs ?? weapon.DamageType;
        var skillFactor = attack.WeaponDamagePercent / 100f;
        var addedFlat   = attacker.GetAddedFlat(damageType.GetScalingStat());
        var multiplier  = GetMultiplier(attacker, damageType, SkillKind.Attack);

        var main = new DamageRange((weapon.MinDamage * skillFactor + addedFlat) * multiplier,
                                   (weapon.MaxDamage * skillFactor + addedFlat) * multiplier);

        //Der Zusatzschaden gehört zum Grundschaden der Waffe: Er wächst mit dem Waffenschaden des Skills und mit den Erhöhungen seines Elements
        var added = weapon.AddedDamage.Select((element, range) => range.Times(skillFactor * GetMultiplier(attacker, element, SkillKind.Attack)));

        //Wandelt der Skill den Schaden in ein Element, zählt der Zusatzschaden desselben Elements zum Hauptteil
        if (damageType.IsElemental())
        {
            main  += added[damageType];
            added =  added.With(damageType, default);
        }

        return new HitRequest(main.Min,
                              main.Max,
                              damageType,
                              SkillKind.Attack,
                              attacker.GetFinal(CombatStat.HitChance),
                              GetCriticalHitChance(attacker, weapon.CriticalHitChance),
                              attacker.GetFinal(CombatStat.CriticalDamage))
        {
            AddedDamage              = added,
            DamageOverTimeMultiplier = StatusEffectRules.GetDamageOverTimeMultiplier(attacker)
        };
    }

    public static HitRequest ForSpell(StatSheet attacker, SpellDefinition spell)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(spell);

        var addedFlat  = attacker.GetAddedFlat(CombatStat.SpellDamage);
        var multiplier = attacker.GetTotalMultiplier(CombatStat.SpellDamage) * GetMultiplier(attacker, spell.DamageType, SkillKind.Spell);

        return new HitRequest((spell.MinDamage + addedFlat) * multiplier,
                              (spell.MaxDamage + addedFlat) * multiplier,
                              spell.DamageType,
                              SkillKind.Spell,
                              attacker.GetFinal(CombatStat.HitChance),
                              GetCriticalHitChance(attacker, spell.CriticalHitChance),
                              attacker.GetFinal(CombatStat.CriticalDamage))
        {
            DamageOverTimeMultiplier = StatusEffectRules.GetDamageOverTimeMultiplier(attacker)
        };
    }

    public static HitRequest ForSkill(StatSheet attacker, WeaponProfile weapon, SkillDefinition skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        return skill.Kind == SkillKind.Spell
                ? ForSpell(attacker, skill.Spell)
                : ForAttack(attacker, weapon, skill.Attack);
    }

    //Erhöhungen der Stats, die einen Schaden treffen, zählen zusammen. More-Modifier multiplizieren sich.
    //Ein Element wächst mit ElementalDamage und seinem eigenen Stat, in Angriffen auch mit ElementalAttackDamage
    private static float GetMultiplier(StatSheet attacker, DamageType damageType, SkillKind skillKind)
    {
        if (damageType.IsPhysical())
            return attacker.GetTotalMultiplier(CombatStat.PhysicalDamage);

        var increased = attacker.GetIncreasedMultiplier(CombatStat.ElementalDamage);
        var more      = attacker.GetMoreMultiplier(CombatStat.ElementalDamage);

        AddScaling(attacker, damageType.GetElementStat(), ref increased, ref more);

        if (skillKind == SkillKind.Attack)
            AddScaling(attacker, CombatStat.ElementalAttackDamage, ref increased, ref more);

        return increased * more;
    }

    private static void AddScaling(StatSheet attacker, CombatStat stat, ref float increased, ref float more)
    {
        increased += attacker.GetIncreasedMultiplier(stat) - 1f;
        more      *= attacker.GetMoreMultiplier(stat);
    }

    private static float GetCriticalHitChance(StatSheet attacker, float baseChance)
        => (baseChance + attacker.GetAddedFlat(CombatStat.CriticalHitChance)) * attacker.GetTotalMultiplier(CombatStat.CriticalHitChance);
}
