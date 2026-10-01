using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Hält fest, wo der Held steht, in welchem Raum und wie weit sein Licht reicht. Daraus folgt, welches Mauerwerk die Sicht freigibt
public static class WallFade
{
    private const string MasonryShaderPath = "res://Shaders/Ps1/ps1_wall.gdshader";
    private const string MarkShaderPath    = "res://Shaders/Ps1/ps1_wall_mark.gdshader";
    private const string PlinthShaderPath  = "res://Shaders/Ps1/ps1_surface.gdshader";
    private const float  TilesPerMeter     = 0.25f;
    private const int    MaxWallsOnRay     = 12;

    private static readonly StringName AlbedoTexture = "albedo_texture";
    private static readonly StringName WorldUvScale  = "world_uv_scale";
    private static readonly StringName Tint          = "tint";
    private static readonly StringName FadeCenter    = "fade_center";
    private static readonly StringName FadeRadius    = "fade_radius";
    private static readonly StringName FadeEdge      = "fade_edge";
    private static readonly StringName FadeSideRamp  = "fade_side_ramp";
    private static readonly StringName HeroHeight    = "hero_height";
    private static readonly StringName CoverRadius   = "cover_radius";
    private static readonly StringName CoverEdge     = "cover_edge";

    private static readonly Dictionary<Texture2D, ShaderMaterial>          MasonryOf = new();
    private static readonly Dictionary<Texture2D, ShaderMaterial>          MarkOf    = new();
    private static readonly Dictionary<(Texture2D, float), ShaderMaterial> PlinthOf  = new();
    private static readonly List<WallSegment>                              Walls     = new();

    private static bool areWallsStale;
    private static int  knownZones = -1;

    public static Shader MasonryShader { get; } = GD.Load<Shader>(MasonryShaderPath);

    public static Shader MarkShader { get; } = GD.Load<Shader>(MarkShaderPath);

    public static WallFadeView View { get; private set; }

    public static int RoomOfHero { get; private set; } = WallOpeningRule.NoRoom;

    public static void Register(WallSegment wall)
    {
        Walls.Add(wall);

        areWallsStale = true;
    }

    public static void Unregister(WallSegment wall)
        => Walls.Remove(wall);

    public static void Update(Vector3 hero, float heroHeight, float radiusMeters, Vector3 camera)
    {
        var view = new WallFadeView(ToPoint(hero), heroHeight, ToPoint(camera), radiusMeters);
        var room = RoomZone.GetIdAt(hero);

        if (room != RoomOfHero || knownZones != RoomZone.Version || areWallsStale)
            Refresh(room);

        if (view == View)
            return;

        View = view;

        foreach (var material in MasonryOf.Values)
            Apply(material);

        foreach (var material in MarkOf.Values)
            Apply(material);
    }

    public static ShaderMaterial GetMasonry(Texture2D texture)
    {
        if (MasonryOf.TryGetValue(texture, out var known))
            return known;

        return MasonryOf[texture] = Fade(Create(MasonryShader, texture, Vector3.One));
    }

    //Eine Spur auf dem Mauerwerk öffnet sich mit ihm. Ihr Bild folgt dem Netz, nicht der Welt
    public static ShaderMaterial GetMark(Texture2D texture)
    {
        if (MarkOf.TryGetValue(texture, out var known))
            return known;

        var material = new ShaderMaterial { Shader = MarkShader };

        material.SetShaderParameter(AlbedoTexture, texture);

        return MarkOf[texture] = Fade(material);
    }

    public static ShaderMaterial GetPlinth(Texture2D texture, float brightness)
    {
        if (PlinthOf.TryGetValue((texture, brightness), out var known))
            return known;

        return PlinthOf[(texture, brightness)] = Create(GD.Load<Shader>(PlinthShaderPath), texture, Vector3.One * brightness);
    }

    //Wahr, wenn auf der Strecke Mauerwerk steht, durch das man nicht sieht
    public static bool IsHidden(PhysicsDirectSpaceState3D space, Vector3 from, Vector3 to)
    {
        var passed = new Godot.Collections.Array<Rid>();

        for (var wall = 0; wall < MaxWallsOnRay; wall++)
        {
            var query = PhysicsRayQueryParameters3D.Create(from, to, CollisionLayers.Walls, passed);
            var hit   = space.IntersectRay(query);

            if (hit.Count == 0)
                return false;

            if (hit["collider"].AsGodotObject() is not WallSegment segment || !segment.IsSeeThroughAt(hit["position"].AsVector3()))
                return true;

            passed.Add(hit["rid"].AsRid());
        }

        return true;
    }

    public static bool IsHidden(Node3D asker, Vector3 from, Vector3 to)
        => IsHidden(asker.GetWorld3D().DirectSpaceState, from, to);

    //Wie weit sich ein Mauerstück öffnet, hängt nur davon ab, in welchem Raum der Held steht. Neu entschieden wird deshalb nur, wenn sich das ändert
    private static void Refresh(int room)
    {
        RoomOfHero    = room;
        knownZones    = RoomZone.Version;
        areWallsStale = false;

        foreach (var wall in Walls)
        {
            if (GodotObject.IsInstanceValid(wall) && wall.IsInsideTree())
                wall.Refresh(room);
        }
    }

    private static WorldPoint ToPoint(Vector3 vector)
        => new(vector.X, vector.Y, vector.Z);

    private static ShaderMaterial Create(Shader shader, Texture2D texture, Vector3 tint)
    {
        var material = new ShaderMaterial { Shader = shader };

        material.SetShaderParameter(AlbedoTexture, texture);
        material.SetShaderParameter(WorldUvScale, TilesPerMeter);
        material.SetShaderParameter(Tint, tint);

        return material;
    }

    private static ShaderMaterial Fade(ShaderMaterial material)
    {
        material.SetShaderParameter(FadeEdge, WallFadeRule.EdgeMeters);
        material.SetShaderParameter(FadeSideRamp, WallFadeRule.SideRampMeters);
        material.SetShaderParameter(CoverRadius, WallFadeRule.CoverMeters);
        material.SetShaderParameter(CoverEdge, WallFadeRule.CoverEdgeMeters);

        Apply(material);

        return material;
    }

    private static void Apply(ShaderMaterial material)
    {
        material.SetShaderParameter(FadeCenter, new Vector3(View.Hero.X, View.Hero.Y, View.Hero.Z));
        material.SetShaderParameter(FadeRadius, View.Radius);
        material.SetShaderParameter(HeroHeight, View.HeroHeight);
    }
}
