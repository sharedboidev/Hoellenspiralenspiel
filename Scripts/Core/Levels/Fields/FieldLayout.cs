using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Levels.Fields;

//Ein Tor auf der äußersten Zeile oder Spalte des Bodens. Side zeigt nach außen, zum Abgrund
public readonly record struct FieldGate(Cell Cell, CellSide Side);

//Der Grundriss einer freien Fläche. Zellen, Vorlagen und Türen liegen in Grid, so lesen Karte und Erkundung ihn wie eine Ebene
public sealed class FieldLayout
{
    public FieldLayout(int seed, LevelLayout grid, CellRect ground, FieldGate entrance, FieldGate? exit, IReadOnlyList<IReadOnlyList<Cell>> obstacleGroups, IReadOnlyList<Cell> packSpots)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(obstacleGroups);
        ArgumentNullException.ThrowIfNull(packSpots);

        Seed           = seed;
        Grid           = grid;
        Ground         = ground;
        Entrance       = entrance;
        Exit           = exit;
        ObstacleGroups = obstacleGroups;
        PackSpots      = packSpots;
    }

    public int Seed { get; }

    public LevelLayout Grid { get; }

    public int Width => Grid.Width;

    public int Height => Grid.Height;

    //Der Boden innerhalb des Saums
    public CellRect Ground { get; }

    public FieldGate Entrance { get; }

    //Fehlt auf der letzten Fläche, dort steht die Arena am fernen Rand
    public FieldGate? Exit { get; }

    public IReadOnlyList<PlacedRoom> Placed => Grid.Rooms;

    //Der Index der Arena in Placed, sonst LevelLayout.NoRoom
    public int Arena => Grid.ExitRoom;

    public IReadOnlyList<IReadOnlyList<Cell>> ObstacleGroups { get; }

    public IReadOnlyList<Cell> PackSpots { get; }

    public IEnumerable<PlacedRoom> WithRole(RoomRole role)
        => Placed.Where(placed => placed.Blueprint.Role == role);
}
