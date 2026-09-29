using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public sealed class ExplorationMap
{
    private readonly bool[] isRevealed;

    public ExplorationMap(int width, int height)
    {
        Width      = Math.Max(0, width);
        Height     = Math.Max(0, height);
        isRevealed = new bool[Width * Height];
    }

    public int Width { get; }

    public int Height { get; }

    public int RevealedCount { get; private set; }

    public event Action Changed;

    public bool IsRevealed(Cell cell)
        => Contains(cell) && isRevealed[cell.Y * Width + cell.X];

    public bool Reveal(Cell cell)
    {
        if (!Contains(cell) || isRevealed[cell.Y * Width + cell.X])
            return false;

        isRevealed[cell.Y * Width + cell.X] = true;

        RevealedCount++;

        return true;
    }

    //Deckt auf, was vom Standort aus zu erreichen ist, ohne den Umkreis zu verlassen. Hinter einer Mauer bleibt die Karte dunkel
    public int RevealAround(LevelLayout layout, Cell origin, float radiusCells)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (!layout.IsFloor(origin))
            return 0;

        var found   = Reveal(origin) ? 1 : 0;
        var visited = new HashSet<Cell> { origin };
        var waiting = new Queue<Cell>();

        waiting.Enqueue(origin);

        while (waiting.Count > 0)
        {
            var cell = waiting.Dequeue();

            foreach (var side in SideExtensions.All)
            {
                var next = cell.Step(side);

                if (!layout.CanStep(cell, side) || !IsWithin(origin, next, radiusCells) || !visited.Add(next))
                    continue;

                if (Reveal(next))
                    found++;

                waiting.Enqueue(next);
            }
        }

        if (found > 0)
            Changed?.Invoke();

        return found;
    }

    public string Encode()
    {
        var bytes = new byte[(isRevealed.Length + 7) / 8];

        for (var i = 0; i < isRevealed.Length; i++)
        {
            if (isRevealed[i])
                bytes[i / 8] |= (byte)(1 << (i % 8));
        }

        return Convert.ToBase64String(bytes);
    }

    //Passt der Text nicht zur Größe der Ebene, stammt er von einer anderen Ebene und zählt nicht
    public bool TryRestore(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return false;

        var bytes = new byte[(isRevealed.Length + 7) / 8];

        if (!Convert.TryFromBase64String(encoded, bytes, out var written) || written != bytes.Length)
            return false;

        RevealedCount = 0;

        for (var i = 0; i < isRevealed.Length; i++)
        {
            isRevealed[i] = (bytes[i / 8] & (1 << (i % 8))) != 0;

            if (isRevealed[i])
                RevealedCount++;
        }

        Changed?.Invoke();

        return true;
    }

    private bool Contains(Cell cell)
        => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    private static bool IsWithin(Cell origin, Cell cell, float radiusCells)
    {
        var x = cell.X - origin.X;
        var y = cell.Y - origin.Y;

        return x * x + y * y <= radiusCells * radiusCells;
    }
}
