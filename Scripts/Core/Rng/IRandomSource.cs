namespace Hoellenspiralenspiel.Scripts.Core.Rng;

public interface IRandomSource
{
    //Gleichverteilt von 0 bis unter 1
    float NextFloat();
}

public static class RandomSourceExtensions
{
    public static float NextPercent(this IRandomSource random)
        => random.NextFloat() * 100f;

    public static float NextRange(this IRandomSource random, float min, float max)
        => min + random.NextFloat() * (max - min);
}
