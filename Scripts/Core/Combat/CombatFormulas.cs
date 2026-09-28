using System;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Alle Rechenregeln der Schadensminderung an einer Stelle
public static class CombatFormulas
{
    //Ab diesem Wert ist ein Ziel immun. Negative Resistenz erhöht den Schaden
    public const float MaxResistance = 100f;

    //Rüstung wirkt gegen kleine Treffer stark und gegen große schwach
    public static float MitigateByArmor(float damage, float armor)
    {
        if (damage <= 0)
            return 0;

        if (armor <= 0)
            return damage;

        return damage - armor * damage / (armor + 5 * damage);
    }

    public static float MitigateByResistance(float damage, float resistancePercent)
    {
        if (damage <= 0)
            return 0;

        return damage * (100f - Math.Min(resistancePercent, MaxResistance)) / 100f;
    }

    public static float ClampChance(float chancePercent)
        => Math.Clamp(chancePercent, 0f, 100f);
}
