using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Macht aus dem Grundriss einer Fläche einen begehbaren Ort: Boden, unsichtbare Mauern am Abgrund, Requisiten auf den Hindernissen,
//die Vorlagen wie Räume, Eingang und Ausgang an den Toren und die Gruppen auf dem freien Boden
public static class FieldBuilder
{
    private const string EntryPath      = "res://Scenes/Objects/field_entry.tscn";
    private const string ExitPath       = "res://Scenes/Objects/field_exit.tscn";
    private const float  FenceHeight    = 4f;
    private const float  FenceThickness = 1f;
    private const float  GateInset      = 0.9f;
    private const int    PropSeedOffset = 86269;

    //Die Rolle einer Vorlage ergibt sich aus ihrem Platz an der Fläche
    public static RoomLibrary CollectTemplates(FieldResource field)
    {
        var library = new RoomLibrary().Add(field.Ruins, RoomRole.Ruin).Add(field.Events, RoomRole.Event);

        if (field.Entrance is not null)
            library.Add([field.Entrance], RoomRole.Entrance);

        if (field.Arena is not null)
            library.Add([field.Arena], RoomRole.Boss);

        return library;
    }

    public static BuiltLevel Build(FieldLayout field, RoomLibrary rooms, FieldResource look, LevelThemeResource theme, Node3D parent, int depth)
    {
        var level = new BuiltLevel
        {
            Root     = new Node3D { Name = $"Field{field.Seed}" },
            Layout   = field.Grid,
            Grid     = LevelGrid.CenteredOn(field.Ground),
            Depth    = depth,
            Location = LocationKey.Of(depth)
        };

        parent.AddChild(level.Root);

        LevelBuilder.LayFloors(level, field.Grid.GetRects(CellKind.Ground, CellKind.Obstacle), look.GroundTexture ?? theme.FloorTexture, "Ground");
        LevelBuilder.LayFloors(level, field.Placed.Select(placed => placed.Rect), theme.FloorTexture, "Floors");
        FenceAbyss(level, field.Ground);
        LevelBuilder.RaiseWalls(level, theme, run => IsClosedTemplate(field, run.RegionBefore) || IsClosedTemplate(field, run.RegionAfter));
        LevelBuilder.FurnishRooms(level, rooms, theme);
        PlaceProps(level, field, look);
        PlaceGates(level, field);
        LevelBuilder.MarkPacks(level, field.PackSpots, "FieldPacks", LevelGrid.CellMeters);
        LevelMarks.Place(level, theme);

        return level;
    }

    //Mauern zieht der Aufbau nur zwischen Boden und einer Vorlage, die keine eigenen mitbringt. Am Abgrund und um Hindernisse stehen keine
    private static bool IsClosedTemplate(FieldLayout field, int region)
        => region >= 0 && region < field.Placed.Count && !field.Placed[region].Blueprint.OpenToField;

    //Unsichtbare Mauern rund um den Boden. Hinter ihnen liegt nichts, der Nebel deckt den Abgrund
    private static void FenceAbyss(BuiltLevel level, CellRect ground)
    {
        var fences = new Node3D { Name = "Fences" };
        var width  = ground.Width * LevelGrid.CellMeters;
        var depth  = ground.Height * LevelGrid.CellMeters;
        var center = level.Grid.GetCenter(ground) + Vector3.Up * (FenceHeight / 2f);
        var reachX = width / 2f + FenceThickness / 2f;
        var reachZ = depth / 2f + FenceThickness / 2f;

        level.Root.AddChild(fences);

        fences.AddChild(CreateFence("North", center + Vector3.Forward * reachZ, new Vector3(width + FenceThickness * 2f, FenceHeight, FenceThickness)));
        fences.AddChild(CreateFence("South", center + Vector3.Back * reachZ, new Vector3(width + FenceThickness * 2f, FenceHeight, FenceThickness)));
        fences.AddChild(CreateFence("West", center + Vector3.Left * reachX, new Vector3(FenceThickness, FenceHeight, depth)));
        fences.AddChild(CreateFence("East", center + Vector3.Right * reachX, new Vector3(FenceThickness, FenceHeight, depth)));
    }

    private static StaticBody3D CreateFence(string side, Vector3 center, Vector3 size)
    {
        var fence = new StaticBody3D
        {
            Name           = $"Fence{side}",
            Position       = center,
            CollisionLayer = CollisionLayers.Walls,
            CollisionMask  = 0
        };

        fence.AddChild(new CollisionShape3D { Name = nameof(CollisionShape3D), Shape = new BoxShape3D { Size = size }, DebugFill = false });

        return fence;
    }

    //Je Zelle mit Hindernis eine Requisite, gewählt und gedreht nach dem Seed der Fläche
    private static void PlaceProps(BuiltLevel level, FieldLayout field, FieldResource look)
    {
        var container = new Node3D { Name = "Props" };
        var scenes    = look.Props.Where(prop => prop is not null).ToList();
        var random    = new SeededRandom(unchecked(field.Seed + PropSeedOffset));

        level.Root.AddChild(container);

        if (scenes.Count == 0 && field.ObstacleGroups.Count > 0)
            GD.PushWarning($"Der Fläche {look.DisplayName} fehlen Requisiten, ihre Hindernisse bleiben unsichtbar.");

        foreach (var cell in field.ObstacleGroups.SelectMany(group => group))
        {
            var prop = scenes.Count > 0 ? scenes[random.NextInt(0, scenes.Count)].Instantiate<Node3D>() : CreateInvisibleBlock();

            prop.Name            = $"Prop{cell.X}x{cell.Y}";
            prop.Position        = level.Grid.GetCenter(cell);
            prop.RotationDegrees = Vector3.Up * random.NextRange(0f, 360f);

            container.AddChild(prop);
        }
    }

    //Ohne Requisite versperrt wenigstens ein Block die Zelle, so stimmen Wege und Grundriss überein
    private static StaticBody3D CreateInvisibleBlock()
        => CreateFence("Block", Vector3.Up * (FenceHeight / 2f), new Vector3(LevelGrid.CellMeters * 0.8f, FenceHeight, LevelGrid.CellMeters * 0.8f));

    private static void PlaceGates(BuiltLevel level, FieldLayout field)
    {
        var entry = CreateGate<FieldEntry>(level, field.Entrance, EntryPath);

        level.Entrances.Add(entry);

        level.HeroStart = entry.ArrivalPoint;

        if (field.Exit is { } exit)
            level.Exits.Add(CreateGate<FieldExit>(level, exit, ExitPath));
    }

    //Ein Tor steht am äußeren Rand seiner Zelle. Seine Rückseite zeigt zum Abgrund, angekommen wird davor auf dem Boden
    private static T CreateGate<T>(BuiltLevel level, FieldGate gate, string path) where T : Passage
    {
        var passage = GD.Load<PackedScene>(path).Instantiate<T>();
        var outward = gate.Side switch
        {
            CellSide.North => Vector3.Forward,
            CellSide.South => Vector3.Back,
            CellSide.West  => Vector3.Left,
            _              => Vector3.Right
        };

        passage.Name = typeof(T).Name;

        level.Root.AddChild(passage);

        passage.GlobalPosition = level.Grid.GetCenter(gate.Cell) + outward * (LevelGrid.CellMeters / 2f - GateInset);
        passage.GlobalRotation = Vector3.Up * Mathf.Atan2(-outward.X, -outward.Z);

        return passage;
    }
}
