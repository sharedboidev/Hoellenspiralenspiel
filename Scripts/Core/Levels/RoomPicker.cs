using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public static class RoomPicker
{
    //Der Start steht vorn, der Ausgang hinten, dazwischen Pflichträume und gewürfelte Räume in zufälliger Folge
    public static List<RoomBlueprint> Pick(IReadOnlyList<RoomBlueprint> blueprints, LevelSettings settings, IRandomSource random)
    {
        var allowed = blueprints.Where(blueprint => blueprint.MinAreaLevel <= settings.AreaLevel).ToList();
        var start   = PickByWeight(allowed.Where(blueprint => blueprint.Role == RoomRole.Start).ToList(), random);
        var exit    = PickByWeight(allowed.Where(blueprint => blueprint.Role == RoomRole.Exit).ToList(), random);

        if (start is null || exit is null)
            throw new LevelGenerationException($"Für das Bereichslevel {settings.AreaLevel} fehlt eine Vorlage für den Start oder den Ausgang.");

        var between = allowed.Where(blueprint => blueprint.IsRequired && IsFiller(blueprint)).ToList();
        var fillers = allowed.Where(blueprint => !blueprint.IsRequired && IsFiller(blueprint)).ToList();

        while (between.Count + 2 < settings.RoomCount && fillers.Count > 0)
        {
            var next = PickByWeight(fillers, random);

            between.Add(next);

            if (next.MaxPerLevel > 0 && between.Count(blueprint => blueprint == next) >= next.MaxPerLevel)
                fillers.Remove(next);
        }

        LevelGenerator.Shuffle(between, random);

        return [start, .. between, exit];
    }

    private static bool IsFiller(RoomBlueprint blueprint)
        => blueprint.Role is RoomRole.Normal or RoomRole.Event;

    private static RoomBlueprint PickByWeight(List<RoomBlueprint> blueprints, IRandomSource random)
    {
        if (blueprints.Count == 0)
            return null;

        var total = blueprints.Sum(blueprint => System.Math.Max(0f, blueprint.Weight));

        if (total <= 0f)
            return blueprints[random.NextInt(0, blueprints.Count)];

        var roll = random.NextFloat() * total;

        foreach (var blueprint in blueprints)
        {
            roll -= System.Math.Max(0f, blueprint.Weight);

            if (roll < 0f)
                return blueprint;
        }

        return blueprints[^1];
    }
}
