using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

//Vorlagen in den Größen, die der Plan für Wollust vorsieht. Bei Ruinen ist jede offene Kante eine Tür
public static class TestFields
{
    public static readonly RoomBlueprint WallRest = Ruin("wall_rest", 2, 1, new DoorSpot(CellSide.South, 0), new DoorSpot(CellSide.South, 1), new DoorSpot(CellSide.West, 0), new DoorSpot(CellSide.East, 0));

    public static readonly RoomBlueprint Passage = Ruin("passage", 4, 1, new DoorSpot(CellSide.West, 0), new DoorSpot(CellSide.East, 0));

    public static readonly RoomBlueprint Courtyard = Ruin("courtyard", 4, 4, Enumerable.Range(0, 4).Select(offset => new DoorSpot(CellSide.South, offset)).ToArray()) with { Weight = 2f };

    public static readonly RoomBlueprint Temple = Ruin("temple", 6, 5, new DoorSpot(CellSide.South, 2), new DoorSpot(CellSide.South, 3), new DoorSpot(CellSide.North, 2)) with
    {
        Weight      = 0.5f,
        MaxPerLevel = 1
    };

    public static readonly RoomBlueprint TowerStump = Ruin("tower_stump", 2, 2, new DoorSpot(CellSide.South, 0));

    public static readonly RoomBlueprint Trapdoor = TestRooms.Create("trapdoor", 3, 3, RoomRole.Entrance);

    public static readonly RoomBlueprint RitualSite = TestRooms.Create("ritual_site", 5, 5, RoomRole.Event) with { OpenToField = true };

    public static readonly RoomBlueprint EyeOfTheStorm = TestRooms.Create("eye_of_the_storm", 5, 5, RoomRole.Event) with { OpenToField = true };

    public static readonly RoomBlueprint JudgementArena = TestRooms.Create("judgement_arena", 9, 9, RoomRole.Boss) with { OpenToField = true };

    public static readonly IReadOnlyList<RoomBlueprint> All = [WallRest, Passage, Courtyard, Temple, TowerStump, Trapdoor, RitualSite, EyeOfTheStorm, JudgementArena];

    public static RoomBlueprint Ruin(string id, int width, int height, params DoorSpot[] doors)
        => new()
        {
            Id          = id,
            Width       = width,
            Height      = height,
            Role        = RoomRole.Ruin,
            Doors       = doors,
            OpenToField = true
        };

    //Abgrund leer, Boden ".", Hindernis "#", Eingang "S", Ausgang "X", Gruppe "!".
    //Vorlagen nach Rolle: Ruine "R", Dungeon-Eingang "D", Event "E", Arena "A", die Zellen mit Tür klein
    public static string Draw(FieldLayout field)
    {
        var text = new StringBuilder();

        for (var y = 0; y < field.Height; y++)
        {
            for (var x = 0; x < field.Width; x++)
                text.Append(GetSign(field, new Cell(x, y)));

            text.Append('\n');
        }

        return text.ToString().TrimEnd('\n') + '\n';
    }

    public static string Describe(FieldLayout field)
    {
        var text = new StringBuilder(Draw(field));

        text.Append($"Eingang {field.Entrance}, Ausgang {(field.Exit is { } exit ? exit.ToString() : "keiner")}, Arena {field.Arena}\n");
        text.Append($"Vorlagen: {string.Join(" ", field.Placed.Select(placed => $"{placed.Blueprint.Id}@{placed.Rect.X},{placed.Rect.Y}/{placed.QuarterTurns}"))}\n");
        text.Append($"Hindernisse: {string.Join(" ", field.ObstacleGroups.Select(group => string.Join("+", group)))}\n");
        text.Append($"Gruppen: {string.Join(" ", field.PackSpots)}\n");

        return text.ToString();
    }

    private static char GetSign(FieldLayout field, Cell cell)
    {
        if (cell == field.Entrance.Cell)
            return 'S';

        if (field.Exit is { } exit && cell == exit.Cell)
            return 'X';

        if (field.PackSpots.Contains(cell))
            return '!';

        var grid = field.Grid;

        switch (grid.GetKind(cell))
        {
            case CellKind.Ground:
                return '.';
            case CellKind.Obstacle:
                return '#';
            case CellKind.Room:
                var placed = field.Placed[grid.GetRoomIndex(cell)];
                var sign   = placed.Blueprint.Role switch
                {
                    RoomRole.Ruin     => 'R',
                    RoomRole.Entrance => 'D',
                    RoomRole.Event    => 'E',
                    RoomRole.Boss     => 'A',
                    _                 => '?'
                };

                return placed.Doors.Any(door => door.Inside == cell) ? char.ToLowerInvariant(sign) : sign;
            default:
                return ' ';
        }
    }
}
