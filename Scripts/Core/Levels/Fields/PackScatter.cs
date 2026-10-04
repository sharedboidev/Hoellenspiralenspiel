using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

public static class PackScatter
{
    //Nicht näher als 4 Zellen am Eingang und nicht näher als 2 an einer Vorlage, also mindestens eine freie Zelle dazwischen
    public const float GapToEntrance  = 4f;
    public const int   GapToTemplates = 1;
    public const float MinSpacing     = 2f;

    private const float SpacingPerPackCell = 0.8f;
    private const float SpacingShrink      = 0.8f;

    //Eine Gruppe je cellsPerPack Zellen Boden, verstreut wie Punkte einer Poisson-Scheibe: Jede neue Gruppe hält Abstand zu allen bisherigen.
    //Reicht der Platz nicht, schrumpft der Abstand. Gruppen stehen nie am Eingang, nie dicht an einer Vorlage und nie am Abgrund
    public static List<Cell> Place(LevelLayout layout, CellRect ground, Cell entrance, int cellsPerPack, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(random);

        var spots = new List<Cell>();

        if (cellsPerPack <= 0)
            return spots;

        var groundCells = new List<Cell>();

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                if (layout.GetKind(new Cell(x, y)) == CellKind.Ground)
                    groundCells.Add(new Cell(x, y));
            }
        }

        var wanted      = groundCells.Count / cellsPerPack;
        var awayFromRim = new CellRect(ground.X + 1, ground.Y + 1, ground.Width - 2, ground.Height - 2);
        var candidates  = groundCells.Where(cell => awayFromRim.Contains(cell)
                                                    && DistanceSquared(cell, entrance) >= GapToEntrance * GapToEntrance
                                                    && layout.Rooms.All(room => !room.Rect.IsCloserThan(new CellRect(cell.X, cell.Y, 1, 1), GapToTemplates)))
                                     .ToList();

        LevelGenerator.Shuffle(candidates, random);

        for (var spacing = MathF.Max(MinSpacing, MathF.Sqrt(cellsPerPack) * SpacingPerPackCell); spots.Count < wanted && spacing >= MinSpacing; spacing *= SpacingShrink)
        {
            foreach (var cell in candidates)
            {
                if (spots.Count >= wanted)
                    break;

                if (spots.TrueForAll(spot => DistanceSquared(spot, cell) >= spacing * spacing))
                    spots.Add(cell);
            }
        }

        return spots;
    }

    public static float DistanceSquared(Cell from, Cell to)
    {
        var x = from.X - to.X;
        var y = from.Y - to.Y;

        return x * x + y * y;
    }
}
