using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public static class CorridorRouter
{
    private const float StepCost    = 1f;
    private const float SharedCost  = 0.6f;
    private const float TurnCost    = 1.5f;
    private const float WallHugCost = 0.4f;
    private const int   NoDirection = 4;

    //Gänge laufen lieber gerade, teilen sich vorhandene Strecken und halten Abstand zu fremden Räumen
    public static List<Cell> FindPath(LevelLayout layout, Cell from, Cell to)
    {
        if (!IsFree(layout, from) || !IsFree(layout, to))
            return null;

        var costs    = new Dictionary<(Cell Cell, int Direction), float>();
        var cameFrom = new Dictionary<(Cell Cell, int Direction), (Cell Cell, int Direction)>();
        var waiting  = new PriorityQueue<(Cell Cell, int Direction), float>();
        var start    = (from, NoDirection);

        costs[start] = 0f;

        waiting.Enqueue(start, 0f);

        while (waiting.TryDequeue(out var current, out var priority))
        {
            if (current.Cell == to)
                return Trace(cameFrom, current);

            if (priority > costs[current] + to.StepsTo(current.Cell) * SharedCost + 0.0001f)
                continue;

            foreach (var side in SideExtensions.All)
            {
                var next = (Cell: current.Cell.Step(side), Direction: (int)side);

                if (!IsFree(layout, next.Cell))
                    continue;

                var cost = costs[current] + GetCost(layout, next.Cell, to) + (current.Direction is NoDirection || current.Direction == next.Direction ? 0f : TurnCost);

                if (costs.TryGetValue(next, out var known) && known <= cost)
                    continue;

                costs[next]    = cost;
                cameFrom[next] = current;

                waiting.Enqueue(next, cost + to.StepsTo(next.Cell) * SharedCost);
            }
        }

        return null;
    }

    private static bool IsFree(LevelLayout layout, Cell cell)
        => layout.Contains(cell) && layout.GetKind(cell) != CellKind.Room;

    private static float GetCost(LevelLayout layout, Cell cell, Cell goal)
    {
        if (layout.GetKind(cell) == CellKind.Corridor)
            return SharedCost;

        if (cell == goal)
            return StepCost;

        foreach (var side in SideExtensions.All)
        {
            if (layout.GetKind(cell.Step(side)) == CellKind.Room)
                return StepCost + WallHugCost;
        }

        return StepCost;
    }

    private static List<Cell> Trace(Dictionary<(Cell Cell, int Direction), (Cell Cell, int Direction)> cameFrom, (Cell Cell, int Direction) last)
    {
        var path = new List<Cell> { last.Cell };

        while (cameFrom.TryGetValue(last, out var previous))
        {
            path.Add(previous.Cell);

            last = previous;
        }

        path.Reverse();

        return path;
    }
}
