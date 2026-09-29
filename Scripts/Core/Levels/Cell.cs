using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public enum CellSide
{
    North,
    East,
    South,
    West
}

//Y wächst nach Süden und ist in der Welt die Achse Z
public readonly record struct Cell(int X, int Y)
{
    public Cell Step(CellSide side)
        => side switch
        {
            CellSide.North => this with { Y = Y - 1 },
            CellSide.East  => this with { X = X + 1 },
            CellSide.South => this with { Y = Y + 1 },
            _              => this with { X = X - 1 }
        };

    public int StepsTo(Cell other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}

public readonly record struct CellRect(int X, int Y, int Width, int Height)
{
    public int Right  => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(Cell cell)
        => cell.X >= X && cell.X < Right && cell.Y >= Y && cell.Y < Bottom;

    public bool IsCloserThan(CellRect other, int gap)
        => X < other.Right + gap && other.X < Right + gap && Y < other.Bottom + gap && other.Y < Bottom + gap;

    public int GapTo(CellRect other)
        => Math.Max(0, Math.Max(other.X - Right, X - other.Right)) + Math.Max(0, Math.Max(other.Y - Bottom, Y - other.Bottom));
}

public static class SideExtensions
{
    public static readonly CellSide[] All = [CellSide.North, CellSide.East, CellSide.South, CellSide.West];

    public static CellSide Opposite(this CellSide side)
        => side.Turn(2);

    //Im Uhrzeigersinn, von oben gesehen
    public static CellSide Turn(this CellSide side, int quarterTurns)
        => (CellSide)((((int)side + quarterTurns) % 4 + 4) % 4);

    public static bool IsAlongX(this CellSide side)
        => side is CellSide.North or CellSide.South;
}
