using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

//Eine freie Fläche in Zellen von 4 m. Der Saum am Rand ist Abgrund, alles darin Boden, Vorlagen und Hindernisse
public sealed record FieldSettings
{
    public int Width { get; init; } = 32;

    public int Height { get; init; } = 24;

    //So viele Zellen Abgrund liegen rund um den Boden
    public int Margin { get; init; } = 2;

    public int MinRuins { get; init; } = 5;

    public int MaxRuins { get; init; } = 8;

    //Anteil des freien Bodens, auf dem Felsen, tote Bäume und Knochen liegen
    public float ObstacleShare { get; init; } = 0.08f;

    //Auf so viele Zellen Boden kommt eine Gruppe Gegner, 0 lässt die Fläche leer
    public int CellsPerFieldPack { get; init; } = 40;

    public int AreaLevel { get; init; } = 1;

    //Auf der letzten Fläche eines Kreises steht statt des Ausgangs die Arena, falls es eine Vorlage dafür gibt
    public bool IsLastField { get; init; }

    //Die Id des Events, das der Plan des Abstiegs dieser Fläche gibt. Leer heißt: kein Event
    public string EventRoomId { get; init; } = string.Empty;

    public int MaxAttempts { get; init; } = 20;

    //Die Fläche in dieser Tiefe eines Kreises: Jede liegt ein Bereichslevel höher als die davor, die letzte bekommt die Arena
    public FieldSettings ForDepth(int firstAreaLevel, int depth, int fieldCount)
        => this with
        {
            AreaLevel = firstAreaLevel + depth - 1,
            IsLastField = depth >= Math.Max(1, fieldCount)
        };
}
