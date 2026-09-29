using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Der Abstieg in einen Kreis hat einen Seed, aus dem jede Ebene ihren eigenen ableitet. Tiefe 0 heißt, der Held ist nicht im Kreis
public sealed class DescentState
{
    private readonly Dictionary<int, SortedSet<int>> killedByDepth   = new();
    private readonly Dictionary<int, string>         revealedByDepth = new();

    public int Seed { get; private set; }

    public int Depth { get; private set; }

    public int DeepestDepth { get; private set; }

    public bool HasBegun { get; private set; }

    public bool IsBelowGround => Depth > 0;

    public IReadOnlyDictionary<int, string> RevealedByDepth => revealedByDepth;

    public IEnumerable<int> DepthsWithKills => killedByDepth.Keys;

    //Die Checkpoints bleiben. Ebenen, Karten und Tote gehören zum alten Abstieg und verfallen
    public void Begin(int seed)
    {
        Seed     = seed;
        Depth    = 0;
        HasBegun = true;

        revealedByDepth.Clear();
        killedByDepth.Clear();
    }

    public void Restore(int seed, int deepestDepth, IEnumerable<KeyValuePair<int, string>> revealed, IEnumerable<KeyValuePair<int, IEnumerable<int>>> killed = null)
    {
        Begin(seed);

        DeepestDepth = Math.Max(0, deepestDepth);

        foreach (var (levelDepth, encoded) in revealed ?? [])
        {
            if (!string.IsNullOrEmpty(encoded))
                revealedByDepth[levelDepth] = encoded;
        }

        foreach (var (levelDepth, spawnIndices) in killed ?? [])
        {
            foreach (var spawnIndex in spawnIndices ?? [])
                RememberKill(levelDepth, spawnIndex);
        }
    }

    public void GoTo(int depth)
    {
        Depth        = Math.Max(0, depth);
        DeepestDepth = Math.Max(DeepestDepth, Depth);
    }

    public void Leave()
        => Depth = 0;

    //Die erste Ebene steht jedem offen, jede weitere erst dem, der sie betreten hat
    public bool HasReached(int depth)
        => depth >= 1 && depth <= Math.Max(1, DeepestDepth);

    public void Remember(int depth, ExplorationMap map)
    {
        if (map is not null && map.RevealedCount > 0)
            revealedByDepth[depth] = map.Encode();
    }

    public string GetRevealed(int depth)
        => revealedByDepth.GetValueOrDefault(depth);

    public bool RememberKill(int depth, int spawnIndex)
    {
        if (depth < 1 || spawnIndex < 0)
            return false;

        if (!killedByDepth.TryGetValue(depth, out var killed))
            killedByDepth[depth] = killed = new SortedSet<int>();

        return killed.Add(spawnIndex);
    }

    public bool IsKilled(int depth, int spawnIndex)
        => killedByDepth.TryGetValue(depth, out var killed) && killed.Contains(spawnIndex);

    public IReadOnlyCollection<int> GetKilled(int depth)
        => killedByDepth.TryGetValue(depth, out var killed) ? killed : [];

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
