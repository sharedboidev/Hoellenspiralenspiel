using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

public static class FieldPicker
{
    //Vorn die Arena, falls es die letzte Fläche ist, dann der Eingang des Dungeons, das geplante Event und die Ruinen in zufälliger Folge.
    //Eingang und Ausgang der Fläche sind keine Vorlagen, sie setzt der Generator an den Rand
    public static List<RoomBlueprint> Pick(IReadOnlyList<RoomBlueprint> blueprints, FieldSettings settings, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);

        var allowed = blueprints.Where(blueprint => blueprint.MinAreaLevel <= settings.AreaLevel).ToList();
        var picked  = new List<RoomBlueprint>();

        if (settings.IsLastField && RoomPicker.PickByWeight(WithRole(allowed, RoomRole.Boss), random) is { } arena)
            picked.Add(arena);

        if (RoomPicker.PickByWeight(WithRole(allowed, RoomRole.Entrance), random) is { } entrance)
            picked.Add(entrance);

        if (!string.IsNullOrEmpty(settings.EventRoomId))
            picked.Add(FindPlannedEvent(blueprints, settings.EventRoomId));

        picked.AddRange(PickRuins(WithRole(allowed, RoomRole.Ruin), settings, random));

        return picked;
    }

    //Der Plan des Abstiegs entscheidet über das Event, nicht das Bereichslevel
    private static RoomBlueprint FindPlannedEvent(IReadOnlyList<RoomBlueprint> blueprints, string eventRoomId)
        => blueprints.FirstOrDefault(blueprint => blueprint.Role == RoomRole.Event && blueprint.Id == eventRoomId)
           ?? throw new LevelGenerationException($"Das geplante Event {eventRoomId} fehlt unter den Vorlagen der Fläche.");

    private static List<RoomBlueprint> PickRuins(List<RoomBlueprint> ruins, FieldSettings settings, IRandomSource random)
    {
        var picked  = ruins.Where(ruin => ruin.IsRequired).ToList();
        var fillers = ruins.Where(ruin => !ruin.IsRequired).ToList();
        var wanted  = random.NextInt(settings.MinRuins, settings.MaxRuins + 1);

        while (picked.Count < wanted && fillers.Count > 0)
        {
            var next = RoomPicker.PickByWeight(fillers, random);

            picked.Add(next);

            if (next.MaxPerLevel > 0 && picked.Count(ruin => ruin == next) >= next.MaxPerLevel)
                fillers.Remove(next);
        }

        LevelGenerator.Shuffle(picked, random);

        return picked;
    }

    private static List<RoomBlueprint> WithRole(List<RoomBlueprint> blueprints, RoomRole role)
        => blueprints.Where(blueprint => blueprint.Role == role).ToList();
}
