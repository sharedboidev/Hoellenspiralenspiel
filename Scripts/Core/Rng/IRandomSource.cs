namespace Hoellenspiralenspiel.Scripts.Core.Rng;

//Quelle für Zufallszahlen. Der Kern würfelt nur hierüber, damit ein Seed den Ablauf festlegt und Tests feste Werte vorgeben können
public interface IRandomSource
{
    //Gleichverteilt von 0 bis unter 1
    float NextFloat();
}

public static class RandomSourceExtensions
{
    //Wurf von 0 bis unter 100, passend zu Chancen in Prozent
    public static float NextPercent(this IRandomSource random)
        => random.NextFloat() * 100f;

    public static float NextRange(this IRandomSource random, float min, float max)
        => min + random.NextFloat() * (max - min);
}
