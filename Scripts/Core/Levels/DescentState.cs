using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Der Abstieg in einen Kreis hat einen Seed, aus dem jeder Ort seinen eigenen ableitet. Tiefe 0 heißt, der Held ist nicht im Kreis.
//Karte und Gefallene hängen am Ort, die Überladungen mit Tiefe meinen die Fläche oder Ebene dieser Tiefe
public sealed class DescentState
{
    private const int DungeonSeedStep = 1000;

    private readonly Dictionary<LocationKey, SortedSet<int>> killedByLocation   = new();
    private readonly Dictionary<LocationKey, string>         revealedByLocation = new();

    public int Seed { get; private set; }

    public int Depth { get; private set; }

    public int DeepestDepth { get; private set; }

    public bool HasBegun { get; private set; }

    //Der Inhaltsstand des Kreises, mit dem die Ebenen entstanden sind. 0 heißt unbekannt, etwa aus einem älteren Spielstand
    public int ContentVersion { get; private set; }

    public bool IsBelowGround => Depth > 0;

    public IReadOnlyDictionary<LocationKey, string> RevealedByLocation => revealedByLocation;

    public IEnumerable<LocationKey> LocationsWithKills => killedByLocation.Keys;

    //Die Checkpoints bleiben. Ebenen, Karten und Tote gehören zum alten Abstieg und verfallen
    public void Begin(int seed, int contentVersion = 0)
    {
        Seed           = seed;
        Depth          = 0;
        HasBegun       = true;
        ContentVersion = contentVersion;

        revealedByLocation.Clear();
        killedByLocation.Clear();
    }

    //Haben sich Räume oder Gegner des Kreises seit dem Spielstand geändert, passen Karten und Tote nicht mehr zu seinen Ebenen.
    //Ein unbekannter Stand gilt als passend, damit ein älterer Spielstand seinen Abstieg behält
    public bool IsStale(int currentContentVersion)
        => HasBegun && ContentVersion != 0 && ContentVersion != currentContentVersion;

    //Ein Abstieg mit unbekanntem Stand übernimmt den aktuellen, damit die nächste Änderung ihn erkennt
    public void AdoptContentVersion(int currentContentVersion)
    {
        if (ContentVersion == 0)
            ContentVersion = currentContentVersion;
    }

    public void Restore(int seed,
                        int deepestDepth,
                        IEnumerable<KeyValuePair<LocationKey, string>> revealed,
                        IEnumerable<KeyValuePair<LocationKey, IEnumerable<int>>> killed = null,
                        int contentVersion = 0)
    {
        Begin(seed, contentVersion);

        DeepestDepth = Math.Max(0, deepestDepth);

        foreach (var (location, encoded) in revealed ?? [])
        {
            if (location.IsValid && !string.IsNullOrEmpty(encoded))
                revealedByLocation[location] = encoded;
        }

        foreach (var (location, spawnIndices) in killed ?? [])
        {
            foreach (var spawnIndex in spawnIndices ?? [])
                RememberKill(location, spawnIndex);
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

    public void Remember(LocationKey location, ExplorationMap map)
    {
        if (location.IsValid && map is not null && map.RevealedCount > 0)
            revealedByLocation[location] = map.Encode();
    }

    public void Remember(int depth, ExplorationMap map)
        => Remember(LocationKey.Of(depth), map);

    public string GetRevealed(LocationKey location)
        => revealedByLocation.GetValueOrDefault(location);

    public string GetRevealed(int depth)
        => GetRevealed(LocationKey.Of(depth));

    public bool RememberKill(LocationKey location, int spawnIndex)
    {
        if (!location.IsValid || spawnIndex < 0)
            return false;

        if (!killedByLocation.TryGetValue(location, out var killed))
            killedByLocation[location] = killed = new SortedSet<int>();

        return killed.Add(spawnIndex);
    }

    public bool RememberKill(int depth, int spawnIndex)
        => RememberKill(LocationKey.Of(depth), spawnIndex);

    public bool IsKilled(LocationKey location, int spawnIndex)
        => killedByLocation.TryGetValue(location, out var killed) && killed.Contains(spawnIndex);

    public bool IsKilled(int depth, int spawnIndex)
        => IsKilled(LocationKey.Of(depth), spawnIndex);

    public IReadOnlyCollection<int> GetKilled(LocationKey location)
        => killedByLocation.TryGetValue(location, out var killed) ? killed : [];

    public IReadOnlyCollection<int> GetKilled(int depth)
        => GetKilled(LocationKey.Of(depth));

    public int GetSeedOf(int depth)
        => GetSeedOf(Seed, depth);

    //Eine Fläche nimmt den Seed ihrer Tiefe, so bleiben die Ebenen der älteren Spielstände, wie sie waren.
    //Eine Ebene im Dungeon leitet ihren Seed aus dem der Fläche ab
    public int GetSeedOf(LocationKey location)
    {
        var fieldSeed = GetSeedOf(Seed, location.Field);

        return location.IsInDungeon ? GetSeedOf(fieldSeed, (location.Dungeon + 1) * DungeonSeedStep + location.DungeonLevel) : fieldSeed;
    }

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
