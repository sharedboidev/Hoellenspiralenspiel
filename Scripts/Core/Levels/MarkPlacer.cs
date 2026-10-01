using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public enum MarkPlace
{
    Wall,
    Floor
}

//Eine Spur, die der Aufbau der Ebene verteilt: ein Bild an der Mauer oder auf dem Boden, etwa ein blutiger Handabdruck
public sealed record MarkRule
{
    public string Id { get; init; } = string.Empty;

    public MarkPlace Place { get; init; }

    public float WidthMeters { get; init; } = 1f;

    public float HeightMeters { get; init; } = 1f;

    //An der Mauer je Zelle und Seite mit Boden davor, am Boden je Versuch auf der Ebene.
    //Ein Begleiter würfelt je Zelle neben seiner Spur
    public float Chance { get; init; }

    //0 erlaubt die Spur beliebig oft je Ebene, am Boden dann einen Versuch
    public int MaxPerLevel { get; init; }

    //Höhe der Mitte über dem Boden, nur an der Mauer, gewürfelt zwischen beiden Werten
    public float MinCenterHeight { get; init; } = 1.2f;

    public float MaxCenterHeight { get; init; } = 1.6f;

    //Die Id einer anderen Spur an der Mauer. Gesetzt, erscheint diese Spur nie allein, sondern nur in der Zelle jener Spur und den beiden Nachbarzellen
    public string Near { get; init; } = string.Empty;

    //Nur am Boden: auch in den Zellen der Gänge, nicht nur an den Plätzen, die die Räume anbieten
    public bool InCorridors { get; init; }

    public bool IsCompanion => !string.IsNullOrEmpty(Near);
}

//Eine Spur am Mauerstück Run, an dessen Zelle Along, auf der Seite Before (Norden oder Westen) oder der anderen.
//AlongMeters zählt vom Anfang der Zelle, CenterHeightMeters vom Boden
public readonly record struct WallMark(int Rule, int Run, int Along, bool IsBefore, float AlongMeters, float CenterHeightMeters);

//Eine Spur auf dem Boden an einem der Plätze, die die Räume anbieten, gedreht um TurnRadians
public readonly record struct FloorMark(int Rule, int Spot, float TurnRadians);

public static class MarkPlacer
{
    //Läuft die Mauerstücke ab und würfelt je Zelle und Seite mit Boden davor. Die erste Regel, die trifft, bekommt die Stelle,
    //ihre Begleiter würfeln danach in der Zelle und ihren Nachbarn
    public static List<WallMark> PlaceOnWalls(IReadOnlyList<WallRun> runs, IReadOnlyList<MarkRule> rules, float cellMeters, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(random);

        var marks  = new List<WallMark>();
        var counts = new int[rules.Count];

        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];

            for (var along = run.From; along < run.To; along++)
            {
                if (run.RegionBefore != LevelLayout.Rock)
                    TryPlace(marks, counts, rules, run, index, along, true, cellMeters, random);

                if (run.RegionAfter != LevelLayout.Rock)
                    TryPlace(marks, counts, rules, run, index, along, false, cellMeters, random);
            }
        }

        return marks;
    }

    //Würfelt je Regel bis zu MaxPerLevel Mal und nimmt jeden Platz höchstens einmal. Die Plätze der Räume kommen zuerst,
    //dahinter die Zellen der Gänge, die nur Regeln mit InCorridors nehmen
    public static List<FloorMark> PlaceOnFloors(int roomSpotCount, int corridorSpotCount, IReadOnlyList<MarkRule> rules, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(random);

        var marks = new List<FloorMark>();
        var free  = new List<int>();

        for (var spot = 0; spot < roomSpotCount + corridorSpotCount; spot++)
            free.Add(spot);

        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];

            if (rule.Place != MarkPlace.Floor || rule.IsCompanion)
                continue;

            var tries = Math.Max(1, rule.MaxPerLevel);

            for (var attempt = 0; attempt < tries; attempt++)
            {
                var allowed = rule.InCorridors ? free.Count : free.Count(spot => spot < roomSpotCount);

                if (allowed == 0)
                    break;

                if (random.NextFloat() >= rule.Chance)
                    continue;

                var pick = random.NextInt(0, allowed);
                var spot = rule.InCorridors ? free[pick] : free.Where(candidate => candidate < roomSpotCount).ElementAt(pick);

                marks.Add(new FloorMark(index, spot, random.NextFloat() * MathF.Tau));

                free.Remove(spot);
            }
        }

        return marks;
    }

    private static void TryPlace(List<WallMark> marks, int[] counts, IReadOnlyList<MarkRule> rules, WallRun run, int runIndex, int along, bool isBefore, float cellMeters, IRandomSource random)
    {
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];

            if (rule.Place != MarkPlace.Wall || rule.IsCompanion || !HasRoom(rule, counts[index]) || random.NextFloat() >= rule.Chance)
                continue;

            counts[index]++;

            marks.Add(Create(index, rule, runIndex, along, isBefore, cellMeters, random));

            PlaceCompanions(marks, counts, rules, run, runIndex, along, isBefore, rule.Id, cellMeters, random);

            return;
        }
    }

    //Begleiter bleiben auf demselben Mauerstück und derselben Seite wie ihre Spur
    private static void PlaceCompanions(List<WallMark> marks, int[] counts, IReadOnlyList<MarkRule> rules, WallRun run, int runIndex, int along, bool isBefore, string parentId, float cellMeters, IRandomSource random)
    {
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];

            if (rule.Place != MarkPlace.Wall || !rule.IsCompanion || rule.Near != parentId)
                continue;

            for (var cell = Math.Max(run.From, along - 1); cell <= Math.Min(run.To - 1, along + 1); cell++)
            {
                if (!HasRoom(rule, counts[index]) || random.NextFloat() >= rule.Chance)
                    continue;

                counts[index]++;

                marks.Add(Create(index, rule, runIndex, cell, isBefore, cellMeters, random));
            }
        }
    }

    private static WallMark Create(int index, MarkRule rule, int runIndex, int along, bool isBefore, float cellMeters, IRandomSource random)
        => new(index, runIndex, along, isBefore, RollAlong(rule, cellMeters, random), random.NextRange(rule.MinCenterHeight, rule.MaxCenterHeight));

    private static bool HasRoom(MarkRule rule, int count)
        => rule.MaxPerLevel <= 0 || count < rule.MaxPerLevel;

    //Die Spur bleibt in ihrer Zelle. Ist sie breiter als die Zelle, sitzt sie in deren Mitte
    private static float RollAlong(MarkRule rule, float cellMeters, IRandomSource random)
    {
        var margin = Math.Min(rule.WidthMeters / 2f, cellMeters / 2f);

        return random.NextRange(margin, cellMeters - margin);
    }
}
