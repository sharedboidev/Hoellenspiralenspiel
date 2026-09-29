using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public static class LevelGenerator
{
    private const int   SeedStepPerAttempt = 7919;
    private const int   PlacementTries     = 200;
    private const int   ExitCandidates     = 30;
    private const int   ExitTries          = 600;
    private const int   DoorPairsToTry     = 6;
    private const int   UsedDoorPenalty    = 6;
    private const float DetourPenalty      = 1000f;
    private const int   PackGapToDoors     = 2;
    private const int   PackGapToStart     = 3;
    private const int   PackGapToPacks     = 4;

    //Derselbe Seed ergibt mit denselben Vorlagen und Einstellungen dieselbe Ebene
    public static LevelLayout Generate(IReadOnlyList<RoomBlueprint> blueprints, LevelSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.MinGap < 1 || settings.MaxGap < settings.MinGap)
            throw new ArgumentOutOfRangeException(nameof(settings), "Zwischen zwei Räumen braucht es mindestens eine freie Zelle, und MaxGap darf nicht unter MinGap liegen.");

        for (var attempt = 0; attempt < Math.Max(1, settings.MaxAttempts); attempt++)
        {
            var random = new SeededRandom(unchecked(seed + attempt * SeedStepPerAttempt));
            var picked = RoomPicker.Pick(blueprints, settings, random);

            if (TryBuild(picked, settings, seed, random, out var layout))
                return layout;
        }

        throw new LevelGenerationException($"Aus dem Seed {seed} entstand in {settings.MaxAttempts} Versuchen keine Ebene, in der jeder Raum erreichbar ist.");
    }

    private static bool TryBuild(List<RoomBlueprint> picked, LevelSettings settings, int seed, IRandomSource random, out LevelLayout layout)
    {
        layout = null;

        if (!TryPlaceRooms(picked, settings, random, out var rooms))
            return false;

        var exit = rooms.Count - 1;

        layout = CreateLayout(rooms, settings, seed, exit);

        var tree  = GrowTree(layout, random);
        var loops = PickLoops(layout, tree, settings, random);

        foreach (var (from, to) in tree)
        {
            if (!TryConnect(layout, from, to))
                return false;
        }

        foreach (var (from, to) in loops)
            TryConnect(layout, from, to);

        if (!IsEveryRoomReachable(layout))
            return false;

        PlaceCorridorPacks(layout, settings, random);

        return true;
    }

    #region Räume setzen

    private static bool TryPlaceRooms(List<RoomBlueprint> picked, LevelSettings settings, IRandomSource random, out List<PlacedRoom> rooms)
    {
        rooms = [new PlacedRoom(0, picked[0], 0, 0, RollTurns(picked[0], random))];

        for (var index = 1; index < picked.Count; index++)
        {
            var isExit = index == picked.Count - 1;
            var room   = isExit ? FindFarthestPlace(index, picked[index], rooms, settings, random) : FindPlace(index, picked[index], rooms, settings, random, PlacementTries);

            if (room is null)
                return false;

            rooms.Add(room);
        }

        return true;
    }

    private static int RollTurns(RoomBlueprint blueprint, IRandomSource random)
        => blueprint.CanRotate ? random.NextInt(0, 4) : 0;

    private static PlacedRoom FindPlace(int index, RoomBlueprint blueprint, List<PlacedRoom> rooms, LevelSettings settings, IRandomSource random, int tries)
    {
        for (var attempt = 0; attempt < tries; attempt++)
        {
            var candidate = RollPlaceNextTo(index, blueprint, rooms[random.NextInt(0, rooms.Count)].Rect, settings, random);

            if (rooms.TrueForAll(room => !room.Rect.IsCloserThan(candidate.Rect, settings.MinGap)))
                return candidate;
        }

        return null;
    }

    //Der Ausgang liegt so weit vom Start weg, wie es die Ebene hergibt
    private static PlacedRoom FindFarthestPlace(int index, RoomBlueprint blueprint, List<PlacedRoom> rooms, LevelSettings settings, IRandomSource random)
    {
        PlacedRoom farthest = null;
        var        found    = 0;

        for (var attempt = 0; attempt < ExitTries && found < ExitCandidates; attempt++)
        {
            var candidate = FindPlace(index, blueprint, rooms, settings, random, 1);

            if (candidate is null)
                continue;

            found++;

            if (farthest is null || candidate.Rect.GapTo(rooms[0].Rect) > farthest.Rect.GapTo(rooms[0].Rect))
                farthest = candidate;
        }

        return farthest;
    }

    private static PlacedRoom RollPlaceNextTo(int index, RoomBlueprint blueprint, CellRect anchor, LevelSettings settings, IRandomSource random)
    {
        var turns  = RollTurns(blueprint, random);
        var width  = turns % 2 == 1 ? blueprint.Height : blueprint.Width;
        var height = turns % 2 == 1 ? blueprint.Width : blueprint.Height;
        var side   = (CellSide)random.NextInt(0, 4);
        var gap    = random.NextInt(settings.MinGap, settings.MaxGap + 1);
        var x      = random.NextInt(anchor.X - width + 1, anchor.Right);
        var y      = random.NextInt(anchor.Y - height + 1, anchor.Bottom);

        switch (side)
        {
            case CellSide.North:
                y = anchor.Y - gap - height;

                break;
            case CellSide.South:
                y = anchor.Bottom + gap;

                break;
            case CellSide.West:
                x = anchor.X - gap - width;

                break;
            default:
                x = anchor.Right + gap;

                break;
        }

        return new PlacedRoom(index, blueprint, x, y, turns);
    }

    private static LevelLayout CreateLayout(List<PlacedRoom> rooms, LevelSettings settings, int seed, int exit)
    {
        var left   = rooms.Min(room => room.Rect.X);
        var top    = rooms.Min(room => room.Rect.Y);
        var right  = rooms.Max(room => room.Rect.Right);
        var bottom = rooms.Max(room => room.Rect.Bottom);
        var margin = Math.Max(1, settings.Margin);
        var moved  = rooms.Select(room => room.MoveBy(margin - left, margin - top)).ToList();

        return new LevelLayout(seed, right - left + margin * 2, bottom - top + margin * 2, moved, 0, exit);
    }

    #endregion

    #region Verbindungen wählen

    //Der kürzeste Baum über alle Räume. Start und Ausgang hängen nur direkt aneinander, wenn es nicht anders geht
    private static List<(int From, int To)> GrowTree(LevelLayout layout, IRandomSource random)
    {
        var tree     = new List<(int, int)>();
        var isInTree = new bool[layout.Rooms.Count];

        isInTree[layout.StartRoom] = true;

        for (var added = 1; added < layout.Rooms.Count; added++)
        {
            var nearest  = (From: -1, To: -1);
            var shortest = float.MaxValue;

            for (var from = 0; from < layout.Rooms.Count; from++)
            {
                for (var to = 0; to < layout.Rooms.Count; to++)
                {
                    if (!isInTree[from] || isInTree[to])
                        continue;

                    var distance = GetDistance(layout, from, to) + random.NextFloat() * 0.5f;

                    if (distance >= shortest)
                        continue;

                    nearest  = (from, to);
                    shortest = distance;
                }
            }

            isInTree[nearest.To] = true;

            tree.Add(nearest);
        }

        return tree;
    }

    private static float GetDistance(LevelLayout layout, int from, int to)
    {
        var gap = layout.Rooms[from].Rect.GapTo(layout.Rooms[to].Rect);

        return IsStartToExit(layout, from, to) ? gap + DetourPenalty : gap;
    }

    private static bool IsStartToExit(LevelLayout layout, int from, int to)
        => (from == layout.StartRoom && to == layout.ExitRoom) || (from == layout.ExitRoom && to == layout.StartRoom);

    private static List<(int From, int To)> PickLoops(LevelLayout layout, List<(int From, int To)> tree, LevelSettings settings, IRandomSource random)
    {
        var wanted = (int)MathF.Round(layout.Rooms.Count * Math.Max(0f, settings.LoopShare));
        var reach  = settings.MaxGap * 3;
        var nearby = new List<(int From, int To)>();

        for (var from = 0; from < layout.Rooms.Count; from++)
        {
            for (var to = from + 1; to < layout.Rooms.Count; to++)
            {
                if (IsStartToExit(layout, from, to) || tree.Contains((from, to)) || tree.Contains((to, from)))
                    continue;

                if (layout.Rooms[from].Rect.GapTo(layout.Rooms[to].Rect) <= reach)
                    nearby.Add((from, to));
            }
        }

        var shortest = nearby.OrderBy(pair => layout.Rooms[pair.From].Rect.GapTo(layout.Rooms[pair.To].Rect))
                             .ThenBy(pair => pair.From)
                             .ThenBy(pair => pair.To)
                             .Take(wanted * 2)
                             .ToList();

        Shuffle(shortest, random);

        return shortest.Take(wanted).ToList();
    }

    internal static void Shuffle<T>(List<T> items, IRandomSource random)
    {
        for (var last = items.Count - 1; last > 0; last--)
        {
            var other = random.NextInt(0, last + 1);

            (items[last], items[other]) = (items[other], items[last]);
        }
    }

    #endregion

    #region Gänge graben

    private static bool TryConnect(LevelLayout layout, int fromRoom, int toRoom)
    {
        var pairs = new List<(PlacedDoor From, PlacedDoor To, int Cost)>();

        foreach (var fromDoor in layout.Rooms[fromRoom].Doors)
        {
            foreach (var toDoor in layout.Rooms[toRoom].Doors)
                pairs.Add((fromDoor, toDoor, fromDoor.Outside.StepsTo(toDoor.Outside) + GetUsePenalty(layout, fromDoor) + GetUsePenalty(layout, toDoor)));
        }

        foreach (var (fromDoor, toDoor, _) in pairs.OrderBy(pair => pair.Cost).Take(DoorPairsToTry))
        {
            var path = CorridorRouter.FindPath(layout, fromDoor.Outside, toDoor.Outside);

            if (path is null)
                continue;

            foreach (var cell in path)
                layout.DigCorridor(cell);

            layout.Open(fromDoor);
            layout.Open(toDoor);
            layout.Connections.Add(new Connection(fromRoom, fromDoor, toRoom, toDoor, path));

            return true;
        }

        return false;
    }

    private static int GetUsePenalty(LevelLayout layout, PlacedDoor door)
        => layout.IsOpening(door.Inside, door.Outside) ? UsedDoorPenalty : 0;

    private static bool IsEveryRoomReachable(LevelLayout layout)
    {
        var reached = new HashSet<Cell>();
        var waiting = new Queue<Cell>();

        waiting.Enqueue(layout.GetCenterCell(layout.StartRoom));
        reached.Add(layout.GetCenterCell(layout.StartRoom));

        while (waiting.Count > 0)
        {
            var cell = waiting.Dequeue();

            foreach (var side in SideExtensions.All)
            {
                if (layout.CanStep(cell, side) && reached.Add(cell.Step(side)))
                    waiting.Enqueue(cell.Step(side));
            }
        }

        return layout.Rooms.All(room => reached.Contains(layout.GetCenterCell(room.Index)));
    }

    #endregion

    #region Gruppen in Gängen

    private static void PlaceCorridorPacks(LevelLayout layout, LevelSettings settings, IRandomSource random)
    {
        if (settings.CellsPerCorridorPack <= 0)
            return;

        var corridor = new List<Cell>();
        var doorways = layout.Connections.SelectMany(connection => new[] { connection.FromDoor.Outside, connection.ToDoor.Outside }).Distinct().ToList();
        var start    = layout.Rooms[layout.StartRoom].Rect;

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                if (layout.GetKind(new Cell(x, y)) == CellKind.Corridor)
                    corridor.Add(new Cell(x, y));
            }
        }

        var wanted = corridor.Count / settings.CellsPerCorridorPack;
        var free   = corridor.Where(cell => doorways.TrueForAll(doorway => doorway.StepsTo(cell) >= PackGapToDoors)
                                            && start.GapTo(new CellRect(cell.X, cell.Y, 1, 1)) >= PackGapToStart)
                             .ToList();

        Shuffle(free, random);

        foreach (var cell in free)
        {
            if (layout.CorridorPacks.Count >= wanted)
                return;

            if (layout.CorridorPacks.TrueForAll(pack => pack.StepsTo(cell) >= PackGapToPacks))
                layout.CorridorPacks.Add(cell);
        }
    }

    #endregion
}
