namespace Hoellenspiralenspiel.Scripts.Core.Rng;

//Wer würfelt, holt die Zufallsquelle hier ab und legt keine eigene an, sonst bestimmt der Seed den Ablauf nicht mehr
public static class GameRandom
{
    public static SeededRandom Shared { get; private set; } = new(System.Environment.TickCount);

    public static void Reseed(int seed)
        => Shared = new SeededRandom(seed);
}
