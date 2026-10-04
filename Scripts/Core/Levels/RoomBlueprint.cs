using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Neue Werte nur am Ende anhängen: Szenen speichern die Rolle als Zahl
public enum RoomRole
{
    Normal,
    Start,
    Exit,
    Event,

    //Steht auf der letzten Ebene an der Stelle des Ausgangs, auf der letzten Fläche als Arena am fernen Rand
    Boss,

    //Mauern, Gänge und Ruinen auf einer freien Fläche
    Ruin,

    //Der Eingang in den Dungeon einer Fläche: Falltür, Höhle oder Gebäude
    Entrance
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

    //0 erlaubt die Vorlage beliebig oft, je Ebene oder je Fläche
    public int MaxPerLevel { get; init; }

    //Auf einer Fläche zieht der Aufbau keine Außenmauern um die Vorlage, ihre eigenen Mauern genügen.
    //Jede offene Kante zählt dann als Tür, denn nur durch Türen geht der Generator hinein
    public bool OpenToField { get; init; }
}
