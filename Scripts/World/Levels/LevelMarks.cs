using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Verteilt die Spuren eines Themas über eine gebaute Ebene: Bilder an den Mauern und auf dem Boden, gewürfelt aus dem Seed der Ebene.
//Eine Spur an der Mauer hängt an ihrem Mauerstück und öffnet sich mit ihm
public static class LevelMarks
{
    private const string FloorShaderPath = "res://Shaders/Ps1/ps1_floor_mark.gdshader";
    private const string WallMarkPrefix  = "Mark_";
    private const string FloorMarkPrefix = "FloorMark_";
    private const int    SeedOffset      = 7919;
    private const float  FloorLift       = 0.02f;
    private const float  WallGap         = 0.02f;

    private static readonly StringName AlbedoTexture = "albedo_texture";

    private static readonly Dictionary<Texture2D, ShaderMaterial> FloorMaterialOf = new();

    public static Shader FloorShader { get; } = GD.Load<Shader>(FloorShaderPath);

    public static void Place(BuiltLevel level, LevelThemeResource theme)
    {
        var resources = theme.Marks.Where(mark => mark?.Texture is not null).ToList();

        if (resources.Count == 0)
            return;

        var rules     = resources.Select(mark => mark.ToRule(NameOf(mark), mark.Near?.Texture is null ? string.Empty : NameOf(mark.Near))).ToList();
        var random    = new SeededRandom(unchecked(level.Layout.Seed + SeedOffset));
        var runs      = level.Walls.Select(entry => entry.Run).ToList();
        var container = new Node3D { Name = "Marks" };

        level.Root.AddChild(container);

        foreach (var mark in MarkPlacer.PlaceOnWalls(runs, rules, LevelGrid.CellMeters, random))
            AddWallMark(level, mark, resources[mark.Rule]);

        foreach (var mark in MarkPlacer.PlaceOnFloors(level.FloorSpots.Count, rules, random))
            AddFloorMark(level, mark, resources[mark.Rule], container);
    }

    private static string NameOf(LevelMarkResource mark)
        => mark.Texture.ResourcePath.GetFile().GetBaseName();

    private static void AddWallMark(BuiltLevel level, WallMark mark, LevelMarkResource resource)
    {
        var (run, wall) = level.Walls[mark.Run];
        var start       = run.IsAlongX ? level.Grid.GetCorner(mark.Along, run.Line) : level.Grid.GetCorner(run.Line, mark.Along);
        var point       = start + (run.IsAlongX ? Vector3.Right : Vector3.Back) * mark.AlongMeters + Vector3.Up * mark.CenterHeightMeters;
        var local       = wall.GlobalTransform.AffineInverse() * point;
        var side        = mark.IsBefore ? -1f : 1f;
        var quad        = CreateQuad(WallMarkPrefix + NameOf(resource), resource, WallFade.GetMark(resource.Texture));

        quad.Position = new Vector3(local.X, local.Y, side * (wall.Thickness / 2f + WallGap));
        quad.Basis    = mark.IsBefore ? new Basis(Vector3.Up, Mathf.Pi) : Basis.Identity;

        wall.AddMark(quad, mark.IsBefore);
    }

    private static void AddFloorMark(BuiltLevel level, FloorMark mark, LevelMarkResource resource, Node3D container)
    {
        var spot = level.FloorSpots[mark.Spot];
        var quad = CreateQuad(FloorMarkPrefix + NameOf(resource), resource, GetFloorMaterial(resource.Texture));

        container.AddChild(quad);

        quad.GlobalPosition = WorldScale.OnGround(spot.GlobalPosition) + Vector3.Up * FloorLift;
        quad.GlobalBasis    = new Basis(Vector3.Up, mark.TurnRadians) * new Basis(Vector3.Right, -Mathf.Pi / 2f);
    }

    private static MeshInstance3D CreateQuad(string name, LevelMarkResource resource, Material material)
    {
        var quad = new MeshInstance3D
        {
            Name       = name,
            Mesh       = new QuadMesh { Size = new Vector2(resource.WidthMeters, resource.HeightMeters) },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        quad.SetSurfaceOverrideMaterial(0, material);

        return quad;
    }

    private static ShaderMaterial GetFloorMaterial(Texture2D texture)
    {
        if (FloorMaterialOf.TryGetValue(texture, out var known))
            return known;

        var material = new ShaderMaterial { Shader = FloorShader };

        material.SetShaderParameter(AlbedoTexture, texture);

        return FloorMaterialOf[texture] = material;
    }
}
