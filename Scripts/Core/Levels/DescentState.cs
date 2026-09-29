using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Ein Abstieg hat einen Seed, aus dem jede Ebene ihren eigenen ableitet. Ebene 0 ist die Oberfläche
public sealed class DescentState
{
    private readonly Dictionary<int, string> revealedByDepth = new();

    public int Seed { get; private set; }

    public int Depth { get; private set; }

    public bool IsBelowGround => Depth > 0;

    public IReadOnlyDictionary<int, string> RevealedByDepth => revealedByDepth;

    public void Begin(int seed)
    {
        Seed  = seed;
        Depth = 0;

        revealedByDepth.Clear();
    }

    public void Restore(int seed, int depth, IEnumerable<KeyValuePair<int, string>> revealed)
    {
        Begin(seed);

        Depth = depth < 0 ? 0 : depth;

        foreach (var (levelDepth, encoded) in revealed ?? [])
            revealedByDepth[levelDepth] = encoded;
    }

    public void GoTo(int depth)
        => Depth = depth < 0 ? 0 : depth;

    public void Remember(int depth, ExplorationMap map)
    {
        if (map is not null && map.RevealedCount > 0)
            revealedByDepth[depth] = map.Encode();
    }

    public string GetRevealed(int depth)
        => revealedByDepth.GetValueOrDefault(depth);

    public int GetSeedOf(int depth)
        => GetSeedOf(Seed, depth);

    //Nachbarn im Seed und in der Tiefe dürfen keine ähnlichen Ebenen ergeben, deshalb die Streuung
    public static int GetSeedOf(int descentSeed, int depth)
    {
        unchecked
        {
            var mixed = (uint)descentSeed * 2654435761u + (uint)depth * 2246822519u;

            mixed ^= mixed >> 15;
            mixed *= 2246822519u;
            mixed ^= mixed >> 13;

            return (int)mixed;
        }
    }
}
