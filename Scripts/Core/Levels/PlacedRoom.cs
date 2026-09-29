using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public readonly record struct PlacedDoor(CellSide Side, Cell Inside)
{
    public Cell Outside => Inside.Step(Side);
}

public sealed class PlacedRoom
{
    public PlacedRoom(int index, RoomBlueprint blueprint, int x, int y, int quarterTurns)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        Index        = index;
        Blueprint    = blueprint;
        QuarterTurns = (quarterTurns % 4 + 4) % 4;

        var isTurnedSideways = QuarterTurns % 2 == 1;

        Rect  = new CellRect(x, y, isTurnedSideways ? blueprint.Height : blueprint.Width, isTurnedSideways ? blueprint.Width : blueprint.Height);
        Doors = PlaceDoors();
    }

    public int Index { get; }

    public RoomBlueprint Blueprint { get; }

    public int QuarterTurns { get; }

    public CellRect Rect { get; }

    public IReadOnlyList<PlacedDoor> Doors { get; }

    public PlacedRoom MoveBy(int x, int y)
        => new(Index, Blueprint, Rect.X + x, Rect.Y + y, QuarterTurns);

    private List<PlacedDoor> PlaceDoors()
    {
        var doors = new List<PlacedDoor>(Blueprint.Doors.Count);

        foreach (var door in Blueprint.Doors)
        {
            var turned = door;
            var height = Blueprint.Height;
            var width  = Blueprint.Width;

            for (var turn = 0; turn < QuarterTurns; turn++)
            {
                turned          = turned.TurnClockwise(height);
                (width, height) = (height, width);
            }

            doors.Add(new PlacedDoor(turned.Side, GetBorderCell(turned)));
        }

        return doors;
    }

    private Cell GetBorderCell(DoorSpot door)
        => door.Side switch
        {
            CellSide.North => new Cell(Rect.X + door.Offset, Rect.Y),
            CellSide.South => new Cell(Rect.X + door.Offset, Rect.Bottom - 1),
            CellSide.West  => new Cell(Rect.X, Rect.Y + door.Offset),
            _              => new Cell(Rect.Right - 1, Rect.Y + door.Offset)
        };
}

public sealed record Connection(int FromRoom, PlacedDoor FromDoor, int ToRoom, PlacedDoor ToDoor, IReadOnlyList<Cell> Path);
