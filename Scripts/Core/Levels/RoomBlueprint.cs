using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public enum RoomRole
{
    Normal,
    Start,
    Exit,
    Event
}

//Offset zählt an Nord und Süd von Westen, an Ost und West von Norden
public readonly record struct DoorSpot(CellSide Side, int Offset)
{
    public DoorSpot TurnClockwise(int heightBefore)
        => Side switch
        {
            CellSide.North => new DoorSpot(CellSide.East, Offset),
            CellSide.East  => new DoorSpot(CellSide.South, heightBefore - 1 - Offset),
            CellSide.South => new DoorSpot(CellSide.West, Offset),
            _              => new DoorSpot(CellSide.North, heightBefore - 1 - Offset)
        };
}

public sealed record RoomBlueprint
{
    public string Id { get; init; } = string.Empty;

    public int Width { get; init; } = 3;

    public int Height { get; init; } = 3;

    public IReadOnlyList<DoorSpot> Doors { get; init; } = [];

    public RoomRole Role { get; init; }

    public float Weight { get; init; } = 1f;

    public int MinAreaLevel { get; init; } = 1;

    public bool IsRequired { get; init; }

    public bool CanRotate { get; init; } = true;

    //0 erlaubt die Vorlage beliebig oft
    public int MaxPerLevel { get; init; }
}
