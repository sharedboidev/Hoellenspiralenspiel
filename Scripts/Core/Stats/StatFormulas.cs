using System;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

//Alle Rechenregeln des Stat-Systems an einer Stelle
public static class StatFormulas
{
    //Flat wird addiert, Increased wird untereinander addiert, More wird multipliziert
    public static float Combine(float baseValue, float addedFlat, float increasedSum, float moreProduct)
        => (baseValue + addedFlat) * (1 + increasedSum) * moreProduct;

    //Attribute sind ganze Zahlen, Nachkommastellen werden abgeschnitten
    public static float TruncateAttribute(float value)
        => MathF.Truncate(value);

    //Base Life: 5 + S + 3*C
    public static float GetLifeBase(int strength, int constitution)
        => 5 + strength + 3 * constitution;

    //Base Mana: 3 + A + 5*I
    public static float GetManaBase(int awareness, int intelligence)
        => 3 + awareness + 5 * intelligence;

    public static float GetLiferegenerationBase(int strength, int constitution)
        => MathF.Truncate(strength / 5f + constitution / 3f);
}
