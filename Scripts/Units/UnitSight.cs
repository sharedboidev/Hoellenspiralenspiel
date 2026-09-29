using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Scripts.Units;

//Hält fest, wo der Held steht und wie weit er Gegner sieht, und gibt es an ihre Materialien weiter
public static class UnitSight
{
    private const string ShaderPath        = "res://Shaders/Ps1/ps1_unit.gdshader";
    private const string SurfaceShaderPath = "res://Shaders/Ps1/ps1_surface.gdshader";

    private static readonly StringName SightCenter = "sight_center";
    private static readonly StringName SightRadius = "sight_radius";
    private static readonly StringName SightEdge   = "sight_edge";

    private static readonly List<ShaderMaterial>                       Materials = new();
    private static readonly Dictionary<ShaderMaterial, ShaderMaterial> SharedOf  = new();

    private static readonly Shader SurfaceShader = GD.Load<Shader>(SurfaceShaderPath);

    public static Shader Shader { get; } = GD.Load<Shader>(ShaderPath);

    public static Vector3 Center { get; private set; }

    public static SightRange Range { get; private set; } = SightRange.Unlimited;

    public static void Update(Vector3 center, SightRange range)
    {
        if (center == Center && range == Range)
            return;

        Center = center;
        Range  = range;

        foreach (var material in Materials)
            Apply(material);
    }

    //Beide Shader kennen dieselben Werte, nur so lässt sich einer gegen den anderen tauschen
    public static bool CanAdopt(Material material)
        => material is ShaderMaterial surface && surface.Shader == SurfaceShader;

    //Das eigene Material eines einzelnen Gegners. Wer es nicht mehr braucht, gibt es mit Release zurück
    public static ShaderMaterial CreateOwn(ShaderMaterial source)
    {
        var material = (ShaderMaterial)source.Duplicate();

        material.Shader = Shader;

        return Register(material);
    }

    public static ShaderMaterial Register(ShaderMaterial material)
    {
        Apply(material);

        Materials.Add(material);

        return material;
    }

    //Teile, die bei allen Gegnern einer Art gleich aussehen, teilen sich ein Material
    public static ShaderMaterial GetShared(ShaderMaterial source)
    {
        if (!SharedOf.TryGetValue(source, out var shared))
            SharedOf[source] = shared = CreateOwn(source);

        return shared;
    }

    public static void Release(ShaderMaterial material)
        => Materials.Remove(material);

    private static void Apply(ShaderMaterial material)
    {
        material.SetShaderParameter(SightCenter, Center);
        material.SetShaderParameter(SightRadius, Range.RadiusMeters);
        material.SetShaderParameter(SightEdge, Range.EdgeMeters);
    }
}
