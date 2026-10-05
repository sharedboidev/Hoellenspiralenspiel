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
        var physical    = GetAddedToBase(attacker, CombatStat.AddedPhysicalToAttacks);

        //"Adds X to Y Physical Damage to Attacks" zählt wie die Waffe zum Grundschaden und wächst mit dem Waffenschaden des Skills
        var main = new DamageRange(((weapon.MinDamage + physical.Min) * skillFactor + addedFlat) * multiplier,
                                   ((weapon.MaxDamage + physical.Max) * skillFactor + addedFlat) * multiplier);

        //Der Zusatzschaden der Waffe und der für alle Angriffe gehören zum Grundschaden: Er wächst mit dem Waffenschaden des Skills und mit den Erhöhungen seines Elements
        var added = weapon.AddedDamage.Select((element, range) => (range + GetAddedToBase(attacker, GetAddedToAttacksStat(element)))
                                                                     .Times(skillFactor * GetMultiplier(attacker, element, SkillKind.Attack)));

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
                              GetCriticalHitChance(attacker, weapon.CriticalHitChance, SkillKind.Attack),
                              attacker.GetFinal(CombatStat.CriticalDamage))
        {
            AddedDamage              = added,
            DamageOverTimeMultiplier = StatusEffectRules.GetDamageOverTimeMultiplier(attacker),
            DamageOverTimeByType     = StatusEffectRules.GetDamageOverTimeByType(attacker)
        };
    }

    public static HitRequest ForSpell(StatSheet attacker, SpellDefinition spell)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(spell);

        var addedFlat   = attacker.GetAddedFlat(CombatStat.SpellDamage);
        var spellFactor = attacker.GetTotalMultiplier(CombatStat.SpellDamage);
        var multiplier  = spellFactor * GetMultiplier(attacker, spell.DamageType, SkillKind.Spell);

        var main = new DamageRange((spell.MinDamage + addedFlat) * multiplier,
                                   (spell.MaxDamage + addedFlat) * multiplier);

        //"Adds X to Y Fire Damage to Spells" zählt zum Grundschaden des Zaubers und wächst wie er mit dem Zauberschaden und seinem Element
        var added = PerElement<DamageRange>.From(element => GetAddedToBase(attacker, GetAddedToSpellsStat(element))
                                                               .Times(spellFactor * GetMultiplier(attacker, element, SkillKind.Spell)));

        //Hat der Zauber selbst dieses Element, zählt der Zusatzschaden zu seinem Hauptteil
        if (spell.DamageType.IsElemental())
        {
            main  += added[spell.DamageType];
            added =  added.With(spell.DamageType, default);
        }

        return new HitRequest(main.Min,
                              main.Max,
                              spell.DamageType,
                              SkillKind.Spell,
                              attacker.GetFinal(CombatStat.HitChance),
                              GetCriticalHitChance(attacker, spell.CriticalHitChance, SkillKind.Spell),
                              attacker.GetFinal(CombatStat.CriticalDamage))
        {
            AddedDamage              = added,
            DamageOverTimeMultiplier = StatusEffectRules.GetDamageOverTimeMultiplier(attacker),
            DamageOverTimeByType     = StatusEffectRules.GetDamageOverTimeByType(attacker)
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

    //Erhöhte Krit-Chance für Zauber zählt mit der allgemeinen zusammen
    private static float GetCriticalHitChance(StatSheet attacker, float baseChance, SkillKind skillKind)
    {
        var increased = attacker.GetIncreasedMultiplier(CombatStat.CriticalHitChance);
        var more      = attacker.GetMoreMultiplier(CombatStat.CriticalHitChance);

        if (skillKind == SkillKind.Spell)
            AddScaling(attacker, CombatStat.SpellCriticalHitChance, ref increased, ref more);

        return (baseChance + attacker.GetAddedFlat(CombatStat.CriticalHitChance)) * (increased * more);
    }

    //Ein globales "Adds X to Y" steht in zwei Stats, X im genannten und Y in seinem Gegenstück mit Max
    private static DamageRange GetAddedToBase(StatSheet attacker, CombatStat minimumStat)
    {
        CombatStatGroups.TryGetRangeMaximum(minimumStat, out var maximumStat);

        return new DamageRange(attacker.GetFinal(minimumStat), attacker.GetFinal(maximumStat));
    }

    private static CombatStat GetAddedToAttacksStat(DamageType element)
        => element switch
        {
            DamageType.Fire      => CombatStat.AddedFireToAttacks,
            DamageType.Frost     => CombatStat.AddedFrostToAttacks,
            DamageType.Lightning => CombatStat.AddedLightningToAttacks,
            _                    => throw new ArgumentOutOfRangeException(nameof(element), element, "Kein Element")
        };

    private static CombatStat GetAddedToSpellsStat(DamageType element)
        => element switch
        {
            DamageType.Fire      => CombatStat.AddedFireToSpells,
            DamageType.Frost     => CombatStat.AddedFrostToSpells,
            DamageType.Lightning => CombatStat.AddedLightningToSpells,
            _                    => throw new ArgumentOutOfRangeException(nameof(element), element, "Kein Element")
        };
}
