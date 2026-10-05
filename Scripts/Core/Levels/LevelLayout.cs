using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Neue Werte nur am Ende anhängen. Ground, Obstacle und Void gibt es nur auf freien Flächen
public enum CellKind : byte
{
    Solid,
    Room,
    Corridor,
    Ground,
    Obstacle,
    Void
}

//Eine Mauer auf einer Linie des Gitters. Längs X liegt sie zwischen den Zeilen Line - 1 und Line, sonst zwischen den Spalten.
//Before ist die Seite von Line - 1, also Norden oder Westen, After die Seite von Line.
//Auf jeder Seite liegt Fels, Gang oder der Raum mit dieser Nummer
public readonly record struct WallRun(bool IsAlongX, int Line, int From, int To, int RegionBefore, int RegionAfter)
{
    public int Length => To - From;
}

public sealed class LevelLayout
{
    public const int Rock     = -2;
    public const int Corridor = -1;

    //Für StartRoom und ExitRoom, wenn es keinen solchen Raum gibt, etwa auf einer freien Fläche
    public const int NoRoom = -1;

    private readonly CellKind[]          kinds;
    private readonly HashSet<(Cell, Cell)> openings = new();
    private readonly int[]               roomOf;

    public LevelLayout(int seed, int width, int height, IReadOnlyList<PlacedRoom> rooms, int startRoom, int exitRoom)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        Seed      = seed;
        Width     = width;
        Height    = height;
        Rooms     = rooms;
        StartRoom = startRoom;
        ExitRoom  = exitRoom;
        kinds     = new CellKind[width * height];
        roomOf    = new int[width * height];

        Array.Fill(roomOf, NoRoom);

        foreach (var room in rooms)
        {
            for (var y = room.Rect.Y; y < room.Rect.Bottom; y++)
            {
                for (var x = room.Rect.X; x < room.Rect.Right; x++)
                {
                    kinds[y * width + x]  = CellKind.Room;
                    roomOf[y * width + x] = room.Index;
                }
            }
        }
    }

    public int Seed { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<PlacedRoom> Rooms { get; }

    public List<Connection> Connections { get; } = new();

    public List<Cell> CorridorPacks { get; } = new();

    public int StartRoom { get; }

    public int ExitRoom { get; }

    public bool Contains(Cell cell)
        => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    public CellKind GetKind(Cell cell)
        => Contains(cell) ? kinds[cell.Y * Width + cell.X] : CellKind.Solid;

    public bool IsFloor(Cell cell)
        => GetKind(cell) is CellKind.Room or CellKind.Corridor or CellKind.Ground;

    public int GetRoomIndex(Cell cell)
        => Contains(cell) ? roomOf[cell.Y * Width + cell.X] : NoRoom;

    //Freier Boden gehört wie ein Gang zu keinem Raum
    public int GetRegion(Cell cell)
        => GetKind(cell) switch
        {
            CellKind.Room     => GetRoomIndex(cell),
            CellKind.Corridor => Corridor,
            CellKind.Ground   => Corridor,
            _                 => Rock
        };

    public void DigCorridor(Cell cell)
    {
        if (GetKind(cell) == CellKind.Room)
            throw new InvalidOperationException($"Durch den Raum bei {cell} führt kein Gang.");

        if (Contains(cell))
            kinds[cell.Y * Width + cell.X] = CellKind.Corridor;
    }

    //Für freie Flächen: Boden, Hindernis oder Abgrund. Die Zellen einer Vorlage bleiben, was sie sind
    public void Paint(Cell cell, CellKind kind)
    {
        if (kind == CellKind.Room)
            throw new ArgumentException("Raumzellen entstehen nur aus Vorlagen.", nameof(kind));

        if (GetKind(cell) == CellKind.Room)
            throw new InvalidOperationException($"Die Zelle {cell} gehört zu einer Vorlage.");

        if (Contains(cell))
            kinds[cell.Y * Width + cell.X] = kind;
    }

    public void Open(PlacedDoor door)
        => openings.Add((door.Inside, door.Outside));

    public bool IsOpening(Cell from, Cell to)
        => openings.Contains((from, to)) || openings.Contains((to, from));

    //Mauern meldet die Zelle mit Boden. Zwischen zwei Böden steht eine Mauer, wenn sie nicht zusammengehören und keine Tür dazwischen liegt
    public bool HasWall(Cell cell, CellSide side)
    {
        if (!IsFloor(cell))
            return false;

        var other = cell.Step(side);

        if (!IsFloor(other))
            return true;

        if (GetKind(cell) == CellKind.Corridor && GetKind(other) == CellKind.Corridor)
            return false;

        if (GetKind(cell) == CellKind.Ground && GetKind(other) == CellKind.Ground)
            return false;

        if (GetKind(cell) == CellKind.Room && GetRoomIndex(cell) == GetRoomIndex(other))
            return false;

        return !IsOpening(cell, other);
    }

    public bool CanStep(Cell from, CellSide side)
        => IsFloor(from) && IsFloor(from.Step(side)) && !HasWall(from, side);

    //Alles, was vom Ursprung aus zu Fuß erreichbar ist: durch Türen, aber nicht durch Mauern, Hindernisse oder den Abgrund
    public HashSet<Cell> FindReachable(Cell origin)
    {
        var reached = new HashSet<Cell>();

        if (!IsFloor(origin))
            return reached;

        var waiting = new Queue<Cell>();

        reached.Add(origin);
        waiting.Enqueue(origin);

        while (waiting.Count > 0)
        {
            var cell = waiting.Dequeue();

            foreach (var side in SideExtensions.All)
            {
                if (CanStep(cell, side) && reached.Add(cell.Step(side)))
                    waiting.Enqueue(cell.Step(side));
            }
        }

        return reached;
    }

    //Ein Stück endet, wo die Mauer endet oder wo sich ändert, was auf einer ihrer Seiten liegt
    public List<WallRun> GetWallRuns()
    {
        var runs = new List<WallRun>();

        for (var line = 0; line <= Height; line++)
            CollectRuns(runs, true, line, Width, along => (new Cell(along, line - 1), new Cell(along, line), CellSide.South));

        for (var line = 0; line <= Width; line++)
            CollectRuns(runs, false, line, Height, along => (new Cell(line - 1, along), new Cell(line, along), CellSide.East));

        return runs;
    }

    private void CollectRuns(List<WallRun> runs, bool isAlongX, int line, int length, Func<int, (Cell Before, Cell After, CellSide Across)> getEdge)
    {
        var from = NoRoom;
        var kind = (Before: Rock, After: Rock);

        for (var along = 0; along <= length; along++)
        {
            var edge   = getEdge(along);
            var isWall = along < length && (HasWall(edge.Before, edge.Across) || HasWall(edge.After, edge.Across.Opposite()));
            var sides  = isWall ? (GetRegion(edge.Before), GetRegion(edge.After)) : (Rock, Rock);

            if (from != NoRoom && (!isWall || sides != kind))
            {
                runs.Add(new WallRun(isAlongX, line, from, along, kind.Before, kind.After));

                from = NoRoom;
            }

            if (!isWall || from != NoRoom)
                continue;

            from = along;
            kind = sides;
        }
    }

    public List<CellRect> GetCorridorRects()
        => GetRects(CellKind.Corridor);

    //Zerlegt die Zellen dieser Arten in Rechtecke, erst zu Streifen je Zeile, dann gleiche Streifen untereinander zu einem
    public List<CellRect> GetRects(params CellKind[] wanted)
    {
        var rects = new List<CellRect>();
        var open  = new Dictionary<(int From, int To), int>();

        for (var y = 0; y <= Height; y++)
        {
            var strips = new HashSet<(int From, int To)>();
            var from   = NoRoom;

            for (var x = 0; x <= Width; x++)
            {
                var cell     = new Cell(x, y);
                var isWanted = Contains(cell) && Array.IndexOf(wanted, GetKind(cell)) >= 0;

                if (isWanted && from == NoRoom)
                    from = x;

                if (isWanted || from == NoRoom)
                    continue;

                strips.Add((from, x));

                from = NoRoom;
            }

            foreach (var strip in new List<(int From, int To)>(open.Keys))
            {
                if (strips.Contains(strip))
                    continue;

                rects.Add(new CellRect(strip.From, open[strip], strip.To - strip.From, y - open[strip]));

                open.Remove(strip);
            }

            foreach (var strip in strips)
                open.TryAdd(strip, y);
        }

        rects.Sort((left, right) => left.Y != right.Y ? left.Y.CompareTo(right.Y) : left.X.CompareTo(right.X));

        return rects;
    }

    public Cell GetCenterCell(int room)
    {
        var rect = Rooms[room].Rect;

        return new Cell(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }
}
