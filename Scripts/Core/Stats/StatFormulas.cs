using System;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

public static class StatFormulas
{
    public static float Combine(float baseValue, float addedFlat, float increasedSum, float moreProduct)
        => (baseValue + addedFlat) * (1 + increasedSum) * moreProduct;

    public static float TruncateAttribute(float value)
        => MathF.Truncate(value);

    public static float GetLifeBase(int strength, int constitution)
        => 5 + strength + 3 * constitution;

    public static float GetManaBase(int awareness, int intelligence)
        => 3 + awareness + 5 * intelligence;

    public static float GetLiferegenerationBase(int strength, int constitution)
        => MathF.Truncate(strength / 5f + constitution / 3f);
}
