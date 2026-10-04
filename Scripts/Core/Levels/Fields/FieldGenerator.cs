using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

public static class FieldGenerator
{
    //Abstände in Zellen. Tore bleiben GateCornerGap von den Ecken des Bodens weg, Vorlagen GateClearance von den Toren,
    //im Umkreis von GateKeepFree um ein Tor liegt kein Hindernis. Eingang und Ausgang trennen mindestens 70 % der Breite
    public const float MinGateDistanceShare = 0.7f;
    public const int   GateCornerGap        = 3;
    public const int   GateClearance        = 3;
    public const int   GateKeepFree         = 2;
    public const int   TemplateGap          = 2;
    public const int   ArenaFarBand         = 2;
    public const int   MinGroundExtent      = 8;

    private const int SeedStepPerAttempt = 7919;
    private const int GateTries          = 60;
    private const int PlacementTries     = 300;
    private const int SpreadCandidates   = 8;

    //Derselbe Seed ergibt mit denselben Vorlagen und Einstellungen dieselbe Fläche
    public static FieldLayout Generate(IReadOnlyList<RoomBlueprint> blueprints, FieldSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(settings);

        Validate(settings);

        for (var attempt = 0; attempt < Math.Max(1, settings.MaxAttempts); attempt++)
        {
            var random = new SeededRandom(unchecked(seed + attempt * SeedStepPerAttempt));
            var picked = FieldPicker.Pick(blueprints, settings, random);

            if (TryBuild(picked, settings, seed, random, out var field))
                return field;
        }

        throw new LevelGenerationException($"Aus dem Seed {seed} entstand in {settings.MaxAttempts} Versuchen keine Fläche, auf der jede Vorlage Platz hat und erreichbar ist.");
    }

    private static void Validate(FieldSettings settings)
    {
        if (settings.Margin < 0 || settings.Width - settings.Margin * 2 < MinGroundExtent || settings.Height - settings.Margin * 2 < MinGroundExtent)
            throw new ArgumentOutOfRangeException(nameof(settings), $"Innerhalb des Saums braucht eine Fläche mindestens {MinGroundExtent} × {MinGroundExtent} Zellen Boden.");

        if (settings.MinRuins < 0 || settings.MaxRuins < settings.MinRuins)
            throw new ArgumentOutOfRangeException(nameof(settings), "MinRuins darf nicht unter 0 und MaxRuins nicht unter MinRuins liegen.");
    }

    private static bool TryBuild(List<RoomBlueprint> picked, FieldSettings settings, int seed, IRandomSource random, out FieldLayout field)
    {
        field = null;

        var ground   = new CellRect(settings.Margin, settings.Margin, settings.Width - settings.Margin * 2, settings.Height - settings.Margin * 2);
        var hasArena = picked.Count > 0 && picked[0].Role == RoomRole.Boss;

        if (!TryPlaceGates(ground, hasArena, settings, random, out var entrance, out var exit))
            return false;

        var gates = exit is { } placedExit ? new[] { entrance.Cell, placedExit.Cell } : new[] { entrance.Cell };

        if (!TryPlaceTemplates(picked, ground, entrance, gates, settings, random, out var rooms))
            return false;

        var layout = new LevelLayout(seed, settings.Width, settings.Height, rooms, LevelLayout.NoRoom, hasArena ? 0 : LevelLayout.NoRoom);

        PaintGround(layout, ground);

        foreach (var door in rooms.SelectMany(room => room.Doors))
            layout.Open(door);

        var obstacles = ObstacleScatter.Scatter(layout, entrance.Cell, Reserve(rooms, gates), settings.ObstacleShare, random);
        var reached   = layout.FindReachable(entrance.Cell);

        if ((exit is { } reachedExit && !reached.Contains(reachedExit.Cell)) || !rooms.TrueForAll(room => room.Doors.Any(door => reached.Contains(door.Inside))))
            return false;

        var packs = PackScatter.Place(layout, ground, entrance.Cell, settings.CellsPerFieldPack, random);

        field = new FieldLayout(seed, layout, ground, entrance, exit, obstacles, packs);

        return true;
    }

    #region Tore

    //Der Ausgang liegt am gegenüberliegenden Rand, weit genug vom Eingang. Mit Arena kommt der Held über die schmale Seite,
    //damit die Arena am anderen Ende der langen Achse steht
    private static bool TryPlaceGates(CellRect ground, bool hasArena, FieldSettings settings, IRandomSource random, out FieldGate entrance, out FieldGate? exit)
    {
        var minDistance = MinGateDistanceShare * Math.Max(settings.Width, settings.Height);

        for (var attempt = 0; attempt < GateTries; attempt++)
        {
            var side = hasArena ? RollLongAxisSide(ground, random) : (CellSide)random.NextInt(0, 4);

            entrance = RollGate(ground, side, random);

            if (hasArena)
            {
                exit = null;

                return true;
            }

            var candidate = RollGate(ground, side.Opposite(), random);

            if (PackScatter.DistanceSquared(entrance.Cell, candidate.Cell) < minDistance * minDistance)
                continue;

            exit = candidate;

            return true;
        }

        entrance = default;
        exit     = null;

        return false;
    }

    private static CellSide RollLongAxisSide(CellRect ground, IRandomSource random)
    {
        var towardsStart = random.NextInt(0, 2) == 0;

        if (ground.Width >= ground.Height)
            return towardsStart ? CellSide.West : CellSide.East;

        return towardsStart ? CellSide.North : CellSide.South;
    }

    private static FieldGate RollGate(CellRect ground, CellSide side, IRandomSource random)
    {
        var from  = (side.IsAlongX() ? ground.X : ground.Y) + GateCornerGap;
        var to    = (side.IsAlongX() ? ground.Right : ground.Bottom) - GateCornerGap;
        var along = random.NextInt(from, Math.Max(from + 1, to));

        return side switch
        {
            CellSide.North => new FieldGate(new Cell(along, ground.Y), side),
            CellSide.South => new FieldGate(new Cell(along, ground.Bottom - 1), side),
            CellSide.West  => new FieldGate(new Cell(ground.X, along), side),
            _              => new FieldGate(new Cell(ground.Right - 1, along), side)
        };
    }

    #endregion

    #region Vorlagen

    //Zwischen Vorlage und Abgrund bleibt eine Zelle Boden, zwischen zwei Vorlagen zwei, zu den Toren drei.
    //Die Arena kommt zuerst an den fernen Rand, danach die großen Vorlagen vor den kleinen.
    //Eine Ruine ohne Platz entfällt, solange genug Ruinen bleiben, alles andere muss stehen
    private static bool TryPlaceTemplates(List<RoomBlueprint> picked, CellRect ground, FieldGate entrance, Cell[] gates, FieldSettings settings, IRandomSource random, out List<PlacedRoom> rooms)
    {
        rooms = [];

        var inner       = new CellRect(ground.X + 1, ground.Y + 1, ground.Width - 2, ground.Height - 2);
        var ordered     = picked.Select((blueprint, order) => (Blueprint: blueprint, Order: order)).ToList();
        var ruinsPicked = picked.Count(blueprint => blueprint.Role == RoomRole.Ruin);
        var ruinsPlaced = 0;

        ordered = ordered.OrderBy(entry => entry.Blueprint.Role == RoomRole.Boss ? 0 : 1)
                         .ThenByDescending(entry => entry.Blueprint.Width * entry.Blueprint.Height)
                         .ThenBy(entry => entry.Order)
                         .ToList();

        foreach (var (blueprint, _) in ordered)
        {
            var room = blueprint.Role == RoomRole.Boss
                    ? FindArenaPlace(rooms.Count, blueprint, inner, entrance, gates, random)
                    : FindPlace(rooms.Count, blueprint, inner, rooms, gates, random);

            if (room is not null)
            {
                rooms.Add(room);

                if (blueprint.Role == RoomRole.Ruin)
                    ruinsPlaced++;

                continue;
            }

            if (blueprint.Role != RoomRole.Ruin || blueprint.IsRequired)
                return false;
        }

        return ruinsPlaced >= Math.Min(settings.MinRuins, ruinsPicked);
    }

    //Die Arena liegt am Rand gegenüber dem Eingang, höchstens ArenaFarBand Zellen davon entfernt
    private static PlacedRoom FindArenaPlace(int index, RoomBlueprint blueprint, CellRect inner, FieldGate entrance, Cell[] gates, IRandomSource random)
    {
        for (var attempt = 0; attempt < PlacementTries; attempt++)
        {
            var turns  = RollTurns(blueprint, random);
            var width  = turns % 2 == 1 ? blueprint.Height : blueprint.Width;
            var height = turns % 2 == 1 ? blueprint.Width : blueprint.Height;

            if (width > inner.Width || height > inner.Height)
                return null;

            var band = random.NextInt(0, ArenaFarBand + 1);
            var x    = random.NextInt(inner.X, inner.Right - width + 1);
            var y    = random.NextInt(inner.Y, inner.Bottom - height + 1);

            switch (entrance.Side.Opposite())
            {
                case CellSide.East:
                    x = Math.Max(inner.X, inner.Right - width - band);

                    break;
                case CellSide.West:
                    x = Math.Min(inner.Right - width, inner.X + band);

                    break;
                case CellSide.South:
                    y = Math.Max(inner.Y, inner.Bottom - height - band);

                    break;
                default:
                    y = Math.Min(inner.Bottom - height, inner.Y + band);

                    break;
            }

            var candidate = new PlacedRoom(index, blueprint, x, y, turns);

            if (IsFree(candidate.Rect, [], gates))
                return candidate;
        }

        return null;
    }

    //Von einigen freien Plätzen nimmt die Vorlage den, der am weitesten von allem bisher Gesetzten entfernt liegt. So verteilen sich die Ruinen
    private static PlacedRoom FindPlace(int index, RoomBlueprint blueprint, CellRect inner, List<PlacedRoom> rooms, Cell[] gates, IRandomSource random)
    {
        PlacedRoom best      = null;
        var        bestSpace = -1;
        var        found     = 0;

        for (var attempt = 0; attempt < PlacementTries && found < SpreadCandidates; attempt++)
        {
            var turns  = RollTurns(blueprint, random);
            var width  = turns % 2 == 1 ? blueprint.Height : blueprint.Width;
            var height = turns % 2 == 1 ? blueprint.Width : blueprint.Height;

            if (width > inner.Width || height > inner.Height)
                continue;

            var candidate = new PlacedRoom(index, blueprint, random.NextInt(inner.X, inner.Right - width + 1), random.NextInt(inner.Y, inner.Bottom - height + 1), turns);

            if (!IsFree(candidate.Rect, rooms, gates))
                continue;

            found++;

            var space = GetSpace(candidate.Rect, rooms, gates);

            if (space <= bestSpace)
                continue;

            best      = candidate;
            bestSpace = space;
        }

        return best;
    }

    private static int RollTurns(RoomBlueprint blueprint, IRandomSource random)
        => blueprint.CanRotate ? random.NextInt(0, 4) : 0;

    private static bool IsFree(CellRect rect, List<PlacedRoom> rooms, Cell[] gates)
        => rooms.TrueForAll(room => !room.Rect.IsCloserThan(rect, TemplateGap)) && Array.TrueForAll(gates, gate => !rect.IsCloserThan(new CellRect(gate.X, gate.Y, 1, 1), GateClearance));

    private static int GetSpace(CellRect rect, List<PlacedRoom> rooms, Cell[] gates)
    {
        var space = int.MaxValue;

        foreach (var room in rooms)
            space = Math.Min(space, rect.GapTo(room.Rect));

        foreach (var gate in gates)
            space = Math.Min(space, rect.GapTo(new CellRect(gate.X, gate.Y, 1, 1)));

        return space;
    }

    #endregion

    #region Boden

    private static void PaintGround(LevelLayout layout, CellRect ground)
    {
        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                var cell = new Cell(x, y);

                if (layout.GetKind(cell) != CellKind.Room)
                    layout.Paint(cell, ground.Contains(cell) ? CellKind.Ground : CellKind.Void);
            }
        }
    }

    //Kein Hindernis vor einer Tür, auch nicht eine Zelle weiter, und keins im Umkreis von zwei Zellen um ein Tor
    private static HashSet<Cell> Reserve(List<PlacedRoom> rooms, Cell[] gates)
    {
        var reserved = new HashSet<Cell>();

        foreach (var door in rooms.SelectMany(room => room.Doors))
        {
            reserved.Add(door.Outside);
            reserved.Add(door.Outside.Step(door.Side));
        }

        foreach (var gate in gates)
        {
            for (var y = -GateKeepFree; y <= GateKeepFree; y++)
            {
                for (var x = -GateKeepFree; x <= GateKeepFree; x++)
                    reserved.Add(new Cell(gate.X + x, gate.Y + y));
            }
        }

        return reserved;
    }

    #endregion
}
