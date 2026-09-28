using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

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
    private const float MoreLightRadiusCeiling       = 400f;
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

    //Eine OriginId pro Attribut: alle von einem Attribut abgeleiteten Modifier werden gemeinsam ersetzt,
    //ohne die abgeleiteten Modifier eines anderen Attributs zu entfernen.
    public static string GetOriginIdFor(CombatStat attribute)
        => $"DerivedFrom{attribute}";

    private static CombatStatModifier[] GetDerivedAwarenessStats(int awareness)
    {
        var originId = GetOriginIdFor(CombatStat.Awareness);

        var derivedMeleeParryValue = GetLogisticGrowthValue(awareness, MoreParryChanceCeiling);
        var meleeParryModifier     = new CombatStatModifier(CombatStat.MeleeParry, ModificationType.More, derivedMeleeParryValue / 100, originId);

        var derivedMeleeBlockValue = GetLogisticGrowthValue(awareness, MoreBlockChanceCeiling);
        var meleeBlockModifier     = new CombatStatModifier(CombatStat.MeleeBlock, ModificationType.More, derivedMeleeBlockValue / 100, originId);

        var derivedCriticalHitChanceValue = GetLogisticGrowthValue(awareness, MoreCriticalHitChanceCeiling);
        var criticalHitChanceModifier     = new CombatStatModifier(CombatStat.CriticalHitChance, ModificationType.More, derivedCriticalHitChanceValue / 100, originId);

        var derivedLightRadiusValue = GetLogisticGrowthValue(awareness, MoreLightRadiusCeiling);
        var lightRadiusModifier     = new CombatStatModifier(CombatStat.LightRadius, ModificationType.More, derivedLightRadiusValue / 100, originId);

        return [meleeParryModifier, meleeBlockModifier, criticalHitChanceModifier, lightRadiusModifier];
    }

    private static CombatStatModifier[] GetDerivedConstitutionStats(int consti)
    {
        var originId = GetOriginIdFor(CombatStat.Constitution);

        var derivedLifeValue = GetLogisticGrowthValue(consti, MoreLifeCeiling);
        var lifeModifier     = new CombatStatModifier(CombatStat.Life, ModificationType.More, derivedLifeValue / 100, originId);

        var derivedArmorValue = GetLogisticGrowthValue(consti, FlatArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.Flat, derivedArmorValue, originId);

        return [lifeModifier, armorModifier];
    }

    private static CombatStatModifier[] GetDerivedIntelligenceStats(int intelligence)
    {
        var originId = GetOriginIdFor(CombatStat.Intelligence);

        var spellDamageValue        = GetLogisticGrowthValue(intelligence, MoreSpellDamageCeiling);
        var elementalDamageModifier = new CombatStatModifier(CombatStat.SpellDamage, ModificationType.More, spellDamageValue / 100, originId);

        var derivedManaValue = GetLogisticGrowthValue(intelligence, MoreManaCeiling);
        var manaModifier     = new CombatStatModifier(CombatStat.Mana, ModificationType.More, derivedManaValue / 100, originId);

        return [elementalDamageModifier, manaModifier];
    }

    private static CombatStatModifier[] GetDerivedDexterityStats(int dex)
    {
        var originId = GetOriginIdFor(CombatStat.Dexterity);

        var derivedAttackspeedValue = GetLogisticGrowthValue(dex, MoreAttackspeedCeiling);
        var attackspeedModifier     = new CombatStatModifier(CombatStat.Attackspeed, ModificationType.More, derivedAttackspeedValue / 100, originId);

        var derivedDodgeValue = GetLogisticGrowthValue(dex, MoreDodgeCeiling);
        var dodgeModifier     = new CombatStatModifier(CombatStat.Dodge, ModificationType.More, derivedDodgeValue / 100, originId);

        return [attackspeedModifier, dodgeModifier];
    }

    private static CombatStatModifier[] GetDerivedStrengthStats(int strength)
    {
        var originId = GetOriginIdFor(CombatStat.Strength);

        var derivedPhysicalDamageValue = GetLogisticGrowthValue(strength, MorePhysicalDamageCeiling);
        var physicalDamageModifier     = new CombatStatModifier(CombatStat.PhysicalDamage, ModificationType.More, derivedPhysicalDamageValue / 100, originId);

        var derivedArmorValue = GetLogisticGrowthValue(strength, MoreArmorCeiling);
        var armorModifier     = new CombatStatModifier(CombatStat.Armor, ModificationType.More, derivedArmorValue / 100, originId);

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