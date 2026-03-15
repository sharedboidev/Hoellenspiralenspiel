using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Utils;

//Graphplotting & Feinjustierung: https://www.desmos.com/calculator
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
    private static readonly Dictionary<float, float> GrowthParameterMap = new()
    {
        { 100f, 0.0009f },
        { 200f, 0.0005f },
        { 300f, 0.00035f },
        { 400f, 0.00025f },
        { 500f, 0.0002f },
        { 1000f, 0.00012f }
    };

    public static CombatStatModifier[] GetModifiersFor(CombatStat combatStat, int value)
        => combatStat switch
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
        var derivedMeleeParryValue = GetLogisticGrowthValue(awareness, MoreParryChanceCeiling);
        var meleeParryModifier     = new CombatStatModifier(CombatStat.MeleeParry, ModificationType.More, derivedMeleeParryValue / 100, nameof(BaseUnit.MeleeParryMoreMultiplierTotal));

        var derivedMeleeBlockValue = GetLogisticGrowthValue(awareness, MoreBlockChanceCeiling);
        var meleeBlockModifier     = new CombatStatModifier(CombatStat.MeleeBlock, ModificationType.More, derivedMeleeBlockValue / 100, nameof(BaseUnit.MeleeBlockMoreMultiplierTotal));

        var derivedCriticalHitChanceValue = GetLogisticGrowthValue(awareness, MoreCriticalHitChanceCeiling);
        var criticalHitChanceModifier     = new CombatStatModifier(CombatStat.CriticalHitChance, ModificationType.More, derivedCriticalHitChanceValue / 100, nameof(BaseUnit.CriticalHitChanceMoreMultiplierTotal));

        return [meleeParryModifier, meleeBlockModifier, criticalHitChanceModifier];
    }

    private static CombatStatModifier[] GetDerivedConstitutionStats(int consti)
    {
        var derivedLifeValue = GetLogisticGrowthValue(consti, MoreLifeCeiling);
        var lifeModifier     = new CombatStatModifier(CombatStat.Life, ModificationType.More, derivedLifeValue / 100, nameof(BaseUnit.LifeMoreMultiplierTotal));

        var derivedArmorValue = GetLogisticGrowthValue(consti, FlatArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.Flat, derivedArmorValue, nameof(BaseUnit.ArmorMoreMultiplierTotal));

        return [lifeModifier, armorModifier];
    }

    private static CombatStatModifier[] GetDerivedIntelligenceStats(int intelligence)
    {
        var spellDamageValue        = GetLogisticGrowthValue(intelligence, MoreSpellDamageCeiling);
        var elementalDamageModifier = new CombatStatModifier(CombatStat.SpellDamage, ModificationType.More, spellDamageValue / 100, nameof(BaseUnit.SpellDamageMoreMultiplierTotal));

        var derivedManaValue = GetLogisticGrowthValue(intelligence, MoreManaCeiling);
        var manaModifier     = new CombatStatModifier(CombatStat.Mana, ModificationType.More, derivedManaValue / 100, nameof(Player2D.ManaMoreMultiplierTotal));

        return [elementalDamageModifier, manaModifier];
    }

    private static CombatStatModifier[] GetDerivedDexterityStats(int dex)
    {
        var derivedAttackspeedValue = GetLogisticGrowthValue(dex, MoreAttackspeedCeiling);
        var attackspeedModifier     = new CombatStatModifier(CombatStat.Attackspeed, ModificationType.More, derivedAttackspeedValue / 100, nameof(BaseUnit.AttackspeedMoreMultiplierTotal));

        var derivedDodgeValue = GetLogisticGrowthValue(dex, MoreDodgeCeiling);
        var dodgeModifier     = new CombatStatModifier(CombatStat.Dodge, ModificationType.More, derivedDodgeValue / 100, nameof(BaseUnit.DodgeMoreMultiplierTotal));

        return [attackspeedModifier, dodgeModifier];
    }

    private static CombatStatModifier[] GetDerivedStrengthStats(int strength)
    {
        var derivedPhysicalDamageValue = GetLogisticGrowthValue(strength, MorePhysicalDamageCeiling);
        var physicalDamageModifier     = new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.More, derivedPhysicalDamageValue / 100, nameof(BaseUnit.PhysicalDamageMoreMultiplierTotal));

        var derivedArmorValue = GetLogisticGrowthValue(strength, MoreArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.More, derivedArmorValue / 100, nameof(BaseUnit.ArmorMoreMultiplierTotal));

        return [physicalDamageModifier, armorModifier];
    }

    private static float GetLogisticGrowthValue(int attributeValue, float modifierCeiling)
    {
        var modifierFloor = 2;
        var k             = GrowthParameterMap[modifierCeiling];

        var firstFactor  = Math.Exp(-k * modifierCeiling * attributeValue);
        var secondFactor = modifierCeiling / modifierFloor - 1;

        var growthValue = modifierCeiling * (1 / (1 + firstFactor * secondFactor));

        return (float)growthValue;

        //Old limited Growth Formula
        //return (float)(modifierCeiling - modifierCeiling * Math.Exp(-.05f * attributeValue));
    }
}