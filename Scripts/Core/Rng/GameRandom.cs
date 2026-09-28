namespace Hoellenspiralenspiel.Scripts.Core.Rng;

//Die gemeinsame Zufallsquelle des Spiels. Wer würfelt, holt sie hier ab und legt keine eigene an
public static class GameRandom
{
    public static SeededRandom Shared { get; private set; } = new(System.Environment.TickCount);

    public static void Reseed(int seed)
        => Shared = new SeededRandom(seed);
}
