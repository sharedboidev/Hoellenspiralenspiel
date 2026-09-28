using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Baut aus den Stats des Angreifers den Treffer, den die Trefferauflösung würfelt
public static class HitRequests
{
    //ATTACK: Waffenschaden mal Prozentsatz des Skills, verstärkt durch physischen oder elementaren Schaden des Angreifers
    public static HitRequest ForAttack(StatSheet attacker, WeaponProfile weapon, AttackDefinition attack)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(attack);

        var damageType  = attack.DealtAs ?? weapon.DamageType;
        var scalingStat = damageType.GetScalingStat();
        var skillFactor = attack.WeaponDamagePercent / 100f;
        var addedFlat   = attacker.GetAddedFlat(scalingStat);
        var multiplier  = attacker.GetTotalMultiplier(scalingStat);

        return new HitRequest((weapon.MinDamage * skillFactor + addedFlat) * multiplier,
                              (weapon.MaxDamage * skillFactor + addedFlat) * multiplier,
                              damageType,
                              SkillKind.Attack,
                              attacker.GetFinal(CombatStat.HitChance),
                              GetCriticalHitChance(attacker, weapon.CriticalHitChance),
                              attacker.GetFinal(CombatStat.CriticalDamage));
    }

    //SPELL: eigener Grundschaden, verstärkt durch Zauberschaden und den Schaden der Schadensart
    public static HitRequest ForSpell(StatSheet attacker, SpellDefinition spell)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(spell);

        var addedFlat  = attacker.GetAddedFlat(CombatStat.SpellDamage);
        var multiplier = attacker.GetTotalMultiplier(CombatStat.SpellDamage) * attacker.GetTotalMultiplier(spell.DamageType.GetScalingStat());

        return new HitRequest((spell.MinDamage + addedFlat) * multiplier,
                              (spell.MaxDamage + addedFlat) * multiplier,
                              spell.DamageType,
                              SkillKind.Spell,
                              attacker.GetFinal(CombatStat.HitChance),
                              GetCriticalHitChance(attacker, spell.CriticalHitChance),
                              attacker.GetFinal(CombatStat.CriticalDamage));
    }

    //Ein Skill ist entweder ATTACK oder SPELL. Die Waffe zählt nur für ATTACK
    public static HitRequest ForSkill(StatSheet attacker, WeaponProfile weapon, SkillDefinition skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        return skill.Kind == SkillKind.Spell
                ? ForSpell(attacker, skill.Spell)
                : ForAttack(attacker, weapon, skill.Attack);
    }

    //Die Grundchance kommt von Waffe oder Zauber, die Modifier vom Angreifer
    private static float GetCriticalHitChance(StatSheet attacker, float baseChance)
        => (baseChance + attacker.GetAddedFlat(CombatStat.CriticalHitChance)) * attacker.GetTotalMultiplier(CombatStat.CriticalHitChance);
}
