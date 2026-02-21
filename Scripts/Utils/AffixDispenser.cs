using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models.Weapons;

namespace Hoellenspiralenspiel.Scripts.Utils;

public static class AffixDispenser
{
    public static Dictionary<WeaponStat, string> ItemnameMap = new()
    {
        { WeaponStat.PhysicalDamage, "Warrior's " },
        { WeaponStat.ElementalDamage, "Sorcerer's " },
        { WeaponStat.AttackSpeed, " of Quickness" },
        { WeaponStat.CriticalHitChance, " of Precision" },
    };
    
    private static readonly WeaponStat[] PossiblePrefixes = [WeaponStat.PhysicalDamage, WeaponStat.ElementalDamage];
    private static readonly WeaponStat[] PossibleSuffixes = [WeaponStat.AttackSpeed, WeaponStat.CriticalHitChance];
    private static readonly Random       Rng              = new();

    public static WeaponStatModifier GetWeaponAffix(AffixType affixType)
        => affixType switch
        {
            AffixType.Prefix => GetWeaponPrefix(),
            AffixType.Suffix => GetWeaponSuffix(),
            _                => throw new ArgumentOutOfRangeException(nameof(affixType), affixType, null)
        };

    private static WeaponStatModifier GetWeaponPrefix()
    {
        var chosenPrefix     = PossiblePrefixes[Rng.Next(0, PossiblePrefixes.Length)];
        var modificationMode = RollModificationMode();

        var value = modificationMode switch
        {
            ModificationType.Flat       => Rng.Next(1, 16),
            ModificationType.Percentage => Rng.Next(30, 301) / 100f,
            _                           => throw new ArgumentOutOfRangeException()
        };

        return new WeaponStatModifier(AffixType.Prefix, chosenPrefix, modificationMode, value);
    }

    private static ModificationType RollModificationMode()
    {
        var modificationMode = Rng.Next(0, 2) == 0 ? ModificationType.Flat : ModificationType.Percentage;

        return modificationMode;
    }

    private static WeaponStatModifier GetWeaponSuffix()
    {
        var chosenSuffix     = PossibleSuffixes[Rng.Next(0, PossibleSuffixes.Length)];
        var modificationMode = RollModificationMode();

        var value = modificationMode switch
        {
            ModificationType.Flat when chosenSuffix is WeaponStat.AttackSpeed             => Rng.Next(1, 21) / 100f,
            ModificationType.Flat when chosenSuffix is WeaponStat.CriticalHitChance       => Rng.Next(1, 31) / 10f,
            ModificationType.Percentage when chosenSuffix is WeaponStat.AttackSpeed       => Rng.Next(5, 41) / 100f,
            ModificationType.Percentage when chosenSuffix is WeaponStat.CriticalHitChance => Rng.Next(15, 101) / 100f,
            _                                                                             => throw new ArgumentOutOfRangeException()
        };

        return new WeaponStatModifier(AffixType.Suffix, chosenSuffix, modificationMode, value);
    }
}