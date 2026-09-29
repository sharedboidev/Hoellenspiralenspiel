using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Tests.Levels;

public static class TestRooms
{
    public static readonly RoomBlueprint Start = Create("start", 3, 3, RoomRole.Start);

    public static readonly RoomBlueprint Exit = Create("exit", 3, 3, RoomRole.Exit);

    public static readonly RoomBlueprint Chamber = Create("chamber", 3, 3);

    public static readonly RoomBlueprint Hall = Create("hall", 5, 4) with { Weight = 2f };

    public static readonly RoomBlueprint Gallery = Create("gallery", 6, 3);

    public static readonly RoomBlueprint Shrine = Create("shrine", 4, 4, RoomRole.Event) with { IsRequired = true };

    public static readonly IReadOnlyList<RoomBlueprint> All = [Start, Exit, Chamber, Hall, Gallery, Shrine];

    //Eine Tür in der Mitte jeder Seite
    public static RoomBlueprint Create(string id, int width, int height, RoomRole role = RoomRole.Normal)
        => new()
        {
            Id     = id,
            Width  = width,
            Height = height,
            Role   = role,
            Doors =
            [
                new DoorSpot(CellSide.North, width / 2),
                new DoorSpot(CellSide.East, height / 2),
                new DoorSpot(CellSide.South, width / 2),
                new DoorSpot(CellSide.West, height / 2)
            ]
        };

    public static string Describe(LevelLayout layout)
    {
        var text = new StringBuilder();

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
                text.Append(GetSign(layout, new Cell(x, y)));

            text.Append('\n');
        }

        foreach (var connection in layout.Connections)
            text.Append($"{connection.FromRoom}-{connection.ToRoom} über {connection.FromDoor.Inside} und {connection.ToDoor.Inside}\n");

        text.Append($"Gruppen: {string.Join(" ", layout.CorridorPacks)}\n");
        text.Append($"Räume: {string.Join(" ", layout.Rooms.Select(room => $"{room.Blueprint.Id}@{room.Rect.X},{room.Rect.Y}/{room.QuarterTurns}"))}\n");

        return text.ToString();
    }

    private static char GetSign(LevelLayout layout, Cell cell)
        => layout.GetKind(cell) switch
        {
            CellKind.Room     => (char)('A' + layout.GetRoomIndex(cell) % 26),
            CellKind.Corridor => layout.CorridorPacks.Contains(cell) ? '!' : '.',
            _                 => ' '
        };
}
