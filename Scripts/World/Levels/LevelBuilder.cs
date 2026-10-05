using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

public sealed class BuiltLevel
{
    public Node3D Root { get; init; }

    public LevelLayout Layout { get; init; }

    public LevelGrid Grid { get; init; }

    public int Depth { get; init; }

    //An diesem Ort hängen Karte und Gefallene im Abstieg
    public LocationKey Location { get; init; }

    public Vector3 HeroStart { get; set; }

    public List<SpawnMarker> RoomMarkers { get; } = new();

    //Gruppen außerhalb der Räume: in den Gängen einer Ebene, auf dem freien Boden einer Fläche
    public List<SpawnMarker> CorridorMarkers { get; } = new();

    //Kellertüren und Treppen einer Ebene, Ausgang und Eingang einer Fläche
    public List<Passage> Exits { get; } = new();

    public List<Passage> Entrances { get; } = new();

    //Die Mauerstücke in der Reihenfolge ihrer Läufe, daran hängen die Spuren
    public List<(WallRun Run, WallSegment Wall)> Walls { get; } = new();

    //Plätze aus den Raumvorlagen, an denen eine Spur auf dem Boden liegen darf
    public List<Node3D> FloorSpots { get; } = new();

    //Die Boss-Räume der Ebene, mit ihren Gittern
    public List<BossArena> Arenas { get; } = new();
}

//Macht aus dem Grundriss eine begehbare Ebene: Böden, Mauern und die Szenen der Räume
public static class LevelBuilder
{
    private const string CellarDoorPath = "res://Scenes/Objects/cellar_door.tscn";
    private const string StairsUpPath   = "res://Scenes/Objects/stairs_up.tscn";
    private const string SurfaceShader  = "res://Shaders/Ps1/ps1_surface.gdshader";
    private const float  FloorThickness = 1f;
    private const float  MetersPerTile  = 2f;
    private const float  TilesPerMeter  = 0.25f;
    private const float  WallThickness  = 0.5f;

    private static readonly StringName AlbedoTexture = "albedo_texture";
    private static readonly StringName WorldUvScale  = "world_uv_scale";

    public static BuiltLevel Build(LevelLayout layout, RoomLibrary rooms, LevelThemeResource theme, Node3D parent, int depth)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(parent);

        var level = new BuiltLevel
        {
            Root     = new Node3D { Name = $"Level{layout.Seed}" },
            Layout   = layout,
            Grid     = new LevelGrid(layout),
            Depth    = depth,
            Location = LocationKey.Of(depth)
        };

        parent.AddChild(level.Root);

        LayFloors(level, theme);
        RaiseWalls(level, theme);
        FurnishRooms(level, rooms, theme);
        MarkPacks(level, level.Layout.CorridorPacks, "CorridorPacks", LevelGrid.CellMeters / 2f);
        LevelMarks.Place(level, theme);

        return level;
    }

    private static void LayFloors(BuiltLevel level, LevelThemeResource theme)
        => LayFloors(level, level.Layout.Rooms.Select(room => room.Rect).Concat(level.Layout.GetCorridorRects()), theme.FloorTexture, "Floors");

    internal static void LayFloors(BuiltLevel level, IEnumerable<CellRect> rects, Texture2D texture, string containerName)
    {
        var floors   = new Node3D { Name = containerName };
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(SurfaceShader) };

        material.SetShaderParameter(AlbedoTexture, texture);
        material.SetShaderParameter(WorldUvScale, TilesPerMeter);

        level.Root.AddChild(floors);

        foreach (var rect in rects)
            floors.AddChild(CreateFloor(level.Grid, rect, material));
    }

    private static StaticBody3D CreateFloor(LevelGrid grid, CellRect rect, Material material)
    {
        var size  = new Vector2(rect.Width, rect.Height) * LevelGrid.CellMeters;
        var floor = new StaticBody3D
        {
            Name           = $"Floor{rect.X}x{rect.Y}",
            Position       = grid.GetCenter(rect),
            CollisionLayer = CollisionLayers.Ground,
            CollisionMask  = 0
        };

        floor.AddChild(new CollisionShape3D
        {
            Name      = nameof(CollisionShape3D),
            Shape     = new BoxShape3D { Size = new Vector3(size.X, FloorThickness, size.Y) },
            Position  = Vector3.Down * (FloorThickness / 2f),
            DebugFill = false
        });

        var mesh = new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new PlaneMesh
            {
                Size           = size,
                SubdivideWidth = GetCuts(size.X),
                SubdivideDepth = GetCuts(size.Y)
            }
        };

        mesh.SetSurfaceOverrideMaterial(0, material);

        floor.AddChild(mesh);

        return floor;
    }

    private static int GetCuts(float meters)
        => Math.Max(0, Mathf.CeilToInt(meters / MetersPerTile) - 1);

    //Jedes Stück ragt an beiden Enden um die halbe Dicke über, so schließen sich die Ecken. Ohne Filter steht jede Mauer des Grundrisses
    internal static void RaiseWalls(BuiltLevel level, LevelThemeResource theme, Func<WallRun, bool> include = null)
    {
        var walls = new Node3D { Name = "Walls" };

        level.Root.AddChild(walls);

        foreach (var run in level.Layout.GetWallRuns())
        {
            if (include?.Invoke(run) == false)
                continue;

            var from   = run.IsAlongX ? level.Grid.GetCorner(run.From, run.Line) : level.Grid.GetCorner(run.Line, run.From);
            var length = run.Length * LevelGrid.CellMeters;

            var wall = new WallSegment
            {
                Name            = $"Wall{(run.IsAlongX ? "X" : "Z")}{run.Line}x{run.From}",
                Position        = from + (run.IsAlongX ? Vector3.Right : Vector3.Back) * (length / 2f),
                RotationDegrees = run.IsAlongX ? Vector3.Zero : Vector3.Up * 90f,
                Length          = length + WallThickness,
                Thickness       = WallThickness
            };

            Dress(wall, theme);

            walls.AddChild(wall);

            level.Walls.Add((run, wall));
        }
    }

    //Mauerstücke mit eigener Textur behalten ihr Aussehen, alle anderen folgen dem Thema
    private static void Dress(WallSegment wall, LevelThemeResource theme)
    {
        if (wall.Texture is not null)
            return;

        wall.Texture          = theme.WallTexture;
        wall.Height           = theme.WallHeight;
        wall.PlinthHeight     = theme.PlinthHeight;
        wall.PlinthBrightness = theme.PlinthBrightness;
    }

    internal static void FurnishRooms(BuiltLevel level, RoomLibrary rooms, LevelThemeResource theme)
    {
        var container = new Node3D { Name = "Rooms" };

        level.Root.AddChild(container);

        foreach (var placed in level.Layout.Rooms)
        {
            var room = rooms.Create(placed.Blueprint);

            room.Name            = $"Room{placed.Index}_{placed.Blueprint.Id}";
            room.Position        = level.Grid.GetCenter(placed.Rect);
            room.RotationDegrees = Vector3.Down * (90f * placed.QuarterTurns);

            room.GetNodeOrNull(RoomTemplate.PreviewName)?.Free();

            foreach (var wall in room.GetAllChildren<WallSegment>())
                Dress(wall, theme);

            //Eine offene Ruine verbirgt nichts, nur geschlossene Vorlagen bekommen eine Zone
            if (!placed.Blueprint.OpenToField)
                room.AddChild(RoomZone.Create(new Vector2(placed.Blueprint.Width, placed.Blueprint.Height) * LevelGrid.CellMeters, theme.WallHeight));

            container.AddChild(room);

            level.FloorSpots.AddRange(room.GetAllChildren<FloorMarkSpot>());

            if (room is BossArena arena)
            {
                arena.BuildGates(placed, level.Layout, level.Grid);

                level.Arenas.Add(arena);
            }

            //Der Name des Markers ist der Name seiner Gruppe. Ohne den Raum im Namen riefe ein Treffer alle gleichnamigen Gruppen der Ebene
            foreach (var marker in room.GetAllChildren<SpawnMarker>())
            {
                marker.Name = $"{room.Name}_{marker.Name}";

                level.RoomMarkers.Add(marker);
            }

            if (placed.Index == level.Layout.StartRoom)
            {
                level.HeroStart = room.GetAllChildren<HeroStart>().FirstOrDefault()?.GlobalPosition ?? room.Position;

                level.Entrances.AddRange(FindEntrances(room));
            }

            //Der Boss-Raum hat keine Kellertür, aus ihm führt nur das Portal des Bosses zurück
            if (placed.Index == level.Layout.ExitRoom && placed.Blueprint.Role != RoomRole.Boss)
                level.Exits.AddRange(FindExits(room));
        }
    }

    //Ein Ausgang ohne Tür wäre eine Sackgasse. Fehlt sie in der Vorlage, steht eine in der Mitte des Raums
    private static IEnumerable<CellarDoor> FindExits(RoomTemplate room)
    {
        var doors = room.GetAllChildren<CellarDoor>();

        if (doors.Length > 0)
            return doors;

        GD.PushWarning($"Der Raumvorlage {room.Name} für den Ausgang fehlt die Kellertür.");

        var door = GD.Load<PackedScene>(CellarDoorPath).Instantiate<CellarDoor>();

        room.AddChild(door);

        return [door];
    }

    private static IEnumerable<StairsUp> FindEntrances(RoomTemplate room)
    {
        var stairs = room.GetAllChildren<StairsUp>();

        if (stairs.Length > 0)
            return stairs;

        GD.PushWarning($"Der Raumvorlage {room.Name} für den Start fehlt die Treppe hinauf.");

        var fallback = GD.Load<PackedScene>(StairsUpPath).Instantiate<StairsUp>();

        room.AddChild(fallback);

        return [fallback];
    }

    internal static void MarkPacks(BuiltLevel level, IEnumerable<Cell> cells, string containerName, float scatterMeters)
    {
        var container = new Node3D { Name = containerName };

        level.Root.AddChild(container);

        foreach (var cell in cells)
        {
            var marker = new SpawnMarker
            {
                Name          = $"Pack{cell.X}x{cell.Y}",
                Position      = level.Grid.GetCenter(cell),
                ScatterRadius = WorldScale.ToPx(scatterMeters)
            };

            container.AddChild(marker);

            level.CorridorMarkers.Add(marker);
        }
    }
}
