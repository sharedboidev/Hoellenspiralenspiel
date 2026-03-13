using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Utils;

public static class DerivedStatProvider
{
    private const float MorePhysicalDamageCeiling    = 100f;
    private const float MoreArmorCeiling             = 300f;
    private const float MoreAttackspeedCeiling       = 100f;
    private const float MoreDodgeCeiling             = 100f;
    private const float MoreSpellDamageCeiling       = 200f;
    private const float MoreManaCeiling              = 300f;
    private const float MoreLifeCeiling              = 100f;
    private const float FlatArmorCeiling             = 1000f;
    private const float MoreParryChanceCeiling       = 200f;
    private const float MoreBlockChanceCeiling       = 200f;
    private const float MoreCriticalHitChanceCeiling = 200f;
    private const float FlatLightRadiusCeiling       = 400f;

    public static CombatStatModifier[] GetModifiersFor(CombatStat combatStat, int value) => combatStat switch
    {
        CombatStat.Strength => GetDerivedStrengthStats(value),
        CombatStat.Dexterity => GetDerivedDexterityStats(value),
        CombatStat.Intelligence => GetDerivedIntelligenceStats(value),
        CombatStat.Constitution => GetDerivedConstitutionStats(value),
        CombatStat.Awareness => GetDerivedAwarenessStats(value),
        _ => []
    };

    private static CombatStatModifier[] GetDerivedAwarenessStats(int awareness)
    {
        var derivedMeleeParryValue = GetLimitedGrowthValue(awareness, MoreParryChanceCeiling);
        var meleeParryModifier     = new CombatStatModifier(CombatStat.MeleeParry, ModificationType.More, derivedMeleeParryValue / 100, nameof(BaseUnit.MeleeParryMoreMultiplierTotal));

        var derivedMeleeBlockValue = GetLimitedGrowthValue(awareness, MoreBlockChanceCeiling);
        var meleeBlockModifier     = new CombatStatModifier(CombatStat.MeleeBlock, ModificationType.More, derivedMeleeBlockValue / 100, nameof(BaseUnit.MeleeBlockMoreMultiplierTotal));

        var derivedCriticalHitChanceValue = GetLimitedGrowthValue(awareness, MoreCriticalHitChanceCeiling);
        var criticalHitChanceModifier     = new CombatStatModifier(CombatStat.CriticalHitChance, ModificationType.More, derivedCriticalHitChanceValue / 100, nameof(BaseUnit.CriticalHitChanceMoreMultiplierTotal));

        return [meleeParryModifier, meleeBlockModifier, criticalHitChanceModifier];
    }

    private static CombatStatModifier[] GetDerivedConstitutionStats(int consti)
    {
        var derivedLifeValue = GetLimitedGrowthValue(consti, MoreLifeCeiling);
        var lifeModifier     = new CombatStatModifier(CombatStat.Life, ModificationType.More, derivedLifeValue / 100, nameof(BaseUnit.LifeMoreMultiplierTotal));

        var derivedArmorValue = GetLimitedGrowthValue(consti, FlatArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.Flat, derivedArmorValue, nameof(BaseUnit.ArmorMoreMultiplierTotal));

        return [lifeModifier, armorModifier];
    }

    private static CombatStatModifier[] GetDerivedIntelligenceStats(int intelligence)
    {
        var derivedSpellDamageValue = GetLimitedGrowthValue(intelligence, MoreSpellDamageCeiling);
        var elementalDamageModifier     = new CombatStatModifier(CombatStat.SpellDamage, ModificationType.More, derivedSpellDamageValue / 100, nameof(BaseUnit.SpellDamageMoreMultiplierTotal));

        var derivedManaValue = GetLimitedGrowthValue(intelligence, MoreManaCeiling);
        var manaModifier     = new CombatStatModifier(CombatStat.Mana, ModificationType.More, derivedManaValue / 100, nameof(Player2D.ManaMoreMultiplierTotal));

        return [elementalDamageModifier, manaModifier];
    }

    private static CombatStatModifier[] GetDerivedDexterityStats(int dex)
    {
        var derivedAttackspeedValue = GetLimitedGrowthValue(dex, MoreAttackspeedCeiling);
        var attackspeedModifier     = new CombatStatModifier(CombatStat.Attackspeed, ModificationType.More, derivedAttackspeedValue / 100, nameof(BaseUnit.AttackspeedMoreMultiplierTotal));

        var derivedDodgeValue = GetLimitedGrowthValue(dex, MoreDodgeCeiling);
        var dodgeModifier     = new CombatStatModifier(CombatStat.Dodge, ModificationType.More, derivedDodgeValue / 100, nameof(BaseUnit.DodgeMoreMultiplierTotal));

        return [attackspeedModifier, dodgeModifier];
    }

    private static CombatStatModifier[] GetDerivedStrengthStats(int strength)
    {
        var derivedPhysicalDamageValue = GetLimitedGrowthValue(strength, MorePhysicalDamageCeiling);
        var physicalDamageModifier     = new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.More, derivedPhysicalDamageValue / 100, nameof(BaseUnit.PhysicalDamageMoreMultiplierTotal));

        var derivedArmorValue = GetLimitedGrowthValue(strength, MoreArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.More, derivedArmorValue / 100, nameof(BaseUnit.ArmorMoreMultiplierTotal));

        return [physicalDamageModifier, armorModifier];
    }

    private static float GetLimitedGrowthValue(int attributeValue, float modifierCeiling) => (float)(modifierCeiling - modifierCeiling * Math.Exp(-.05f * attributeValue));
}