using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public sealed record LevelSettings
{
    //Start, Ausgang und Pflichträume zählen mit
    public int RoomCount { get; init; } = 10;

    //Freie Zellen zwischen zwei Räumen. Mindestens eine, sonst passt kein Gang dazwischen
    public int MinGap { get; init; } = 2;

    public int MaxGap { get; init; } = 4;

    //Zusätzliche Verbindungen als Anteil der Räume, sie machen aus dem Baum ein Netz mit Rundwegen
    public float LoopShare { get; init; } = 0.35f;

    public int AreaLevel { get; init; } = 1;

    //Auf der letzten Ebene eines Kreises steht statt des Ausgangs der Boss-Raum, falls das Thema einen hat
    public bool IsLastLevel { get; init; }

    public int Margin { get; init; } = 2;

    //Auf so viele Zellen Gang kommt eine Gruppe, 0 lässt die Gänge leer
    public int CellsPerCorridorPack { get; init; } = 14;

    public int MaxAttempts { get; init; } = 20;
}

public sealed class LevelGenerationException : Exception
{
    public LevelGenerationException(string message)
            : base(message) { }
}
