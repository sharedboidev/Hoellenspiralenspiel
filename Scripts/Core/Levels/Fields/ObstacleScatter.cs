using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

public static class ObstacleScatter
{
    public const int MinGroupSize = 1;
    public const int MaxGroupSize = 6;

    private const int SeedTries = 600;

    //Verstreut Gruppen aus 1 bis 6 Zellen über den freien Boden, bis der Anteil erreicht ist. Zwischen zwei Gruppen bleibt eine Zelle Platz,
    //auch über Eck. Eine Gruppe, nach der nicht mehr jeder Boden vom Ursprung aus erreichbar wäre, entfällt wieder
    public static List<IReadOnlyList<Cell>> Scatter(LevelLayout layout, Cell origin, IReadOnlySet<Cell> reserved, float share, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(reserved);
        ArgumentNullException.ThrowIfNull(random);

        var ground = new List<Cell>();

        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                if (layout.GetKind(new Cell(x, y)) == CellKind.Ground)
                    ground.Add(new Cell(x, y));
            }
        }

        var groups      = new List<IReadOnlyList<Cell>>();
        var target      = (int)MathF.Round(ground.Count * Math.Clamp(share, 0f, 1f));
        var groundCount = ground.Count;
        var placed      = 0;

        for (var attempt = 0; attempt < SeedTries && placed < target && ground.Count > 0; attempt++)
        {
            var size  = Math.Min(random.NextInt(MinGroupSize, MaxGroupSize + 1), target - placed);
            var start = ground[random.NextInt(0, ground.Count)];

            if (!CanTake(layout, start, reserved))
                continue;

            var group = Grow(layout, start, size, reserved, random);

            foreach (var cell in group)
                layout.Paint(cell, CellKind.Obstacle);

            if (CountReachableGround(layout, origin) != groundCount - group.Count)
            {
                foreach (var cell in group)
                    layout.Paint(cell, CellKind.Ground);

                continue;
            }

            groundCount -= group.Count;
            placed      += group.Count;

            groups.Add(group);
        }

        return groups;
    }

    private static List<Cell> Grow(LevelLayout layout, Cell start, int size, IReadOnlySet<Cell> reserved, IRandomSource random)
    {
        var group    = new List<Cell> { start };
        var frontier = new List<Cell>();

        AddNeighbours(layout, start, group, frontier, reserved);

        while (group.Count < size && frontier.Count > 0)
        {
            var index = random.NextInt(0, frontier.Count);
            var next  = frontier[index];

            frontier.RemoveAt(index);
            group.Add(next);

            AddNeighbours(layout, next, group, frontier, reserved);
        }

        return group;
    }

    private static void AddNeighbours(LevelLayout layout, Cell cell, List<Cell> group, List<Cell> frontier, IReadOnlySet<Cell> reserved)
    {
        foreach (var side in SideExtensions.All)
        {
            var next = cell.Step(side);

            if (!group.Contains(next) && !frontier.Contains(next) && CanTake(layout, next, reserved))
                frontier.Add(next);
        }
    }

    private static bool CanTake(LevelLayout layout, Cell cell, IReadOnlySet<Cell> reserved)
    {
        if (layout.GetKind(cell) != CellKind.Ground || reserved.Contains(cell))
            return false;

        for (var y = -1; y <= 1; y++)
        {
            for (var x = -1; x <= 1; x++)
            {
                if (layout.GetKind(new Cell(cell.X + x, cell.Y + y)) == CellKind.Obstacle)
                    return false;
            }
        }

        return true;
    }

    private static int CountReachableGround(LevelLayout layout, Cell origin)
    {
        var count = 0;

        foreach (var cell in layout.FindReachable(origin))
        {
            if (layout.GetKind(cell) == CellKind.Ground)
                count++;
        }

        return count;
    }
}
