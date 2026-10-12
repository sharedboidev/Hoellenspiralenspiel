using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der kleine Krater, den der Schlag von Lava Crack in den Boden haut. In seiner Mitte brodelt ein Lavapool, aus dem die Risse abfließen,
//darum liegt er über ihrem Anfang. Den Rand bildet eine niedrige Aufwölbung, innen glühend, außen dunkel, und darauf ein Kranz schräg
//aufgeworfener Gesteinsplatten. Wo ein Riss hinausläuft, ist der Rand eingekerbt. Er glüht so lange wie die Risse, dann erkaltet er
//und zerfällt mit ihnen. Nur zum Ansehen, er macht keinen Schaden
public partial class LavaCrater : Node3D
{
    private const string ShaderPath = "res://Shaders/Ps1/ps1_lava_crater.gdshader";

    private static readonly StringName HeatParameter     = "heat";
    private static readonly StringName FadeParameter     = "fade";
    private static readonly StringName EmissionParameter = "emission_energy";

    private readonly List<float>                                   exitAngles = new();
    private readonly List<LavaCrack>                               cracks     = new();
    private readonly List<(ShaderMaterial Material, float Energy)> glowing    = new();
    private readonly List<(Node3D Node, float RestY)>              sinking    = new();

    private double activeSec;
    private double durationSec;
    private bool   isCooling;
    private float  lightEnergy;

    //Rastet wie der Boden ein, Ps1Look stellt ihn mit ein
    public static Shader Shader { get; } = GD.Load<Shader>(ShaderPath);

    //Mulde und Pool liegen flach auf dem Boden, beide aus einem PlaneMesh von 2 x 2 m
    [Export]
    public MeshInstance3D Bowl { get; set; }

    [Export]
    public MeshInstance3D Pool { get; set; }

    //Die Aufwölbung wird beim Start gebaut: die Innenseite mit HotMaterial, die Außenseite mit RockMaterial. Die Platten nehmen RockMaterial
    [Export]
    public MeshInstance3D Rim { get; set; }

    [Export]
    public ShaderMaterial RockMaterial { get; set; }

    [Export]
    public ShaderMaterial HotMaterial { get; set; }

    [Export]
    public OmniLight3D Glow { get; set; }

    [Export]
    public float RadiusMeters { get; set; } = 0.77f;

    //Die Aufwölbung reicht von diesem Anteil des Radius über den Kamm bis zum äußeren Anteil
    [Export]
    public float RimInnerShare { get; set; } = 0.75f;

    [Export]
    public float RimOuterShare { get; set; } = 1.25f;

    [Export]
    public float RimHeightMeters { get; set; } = 0.1f;

    [Export]
    public int RimSegments { get; set; } = 24;

    //Gesteinsplatten auf dem Kamm, ihre Länge in Metern und wie steil sie nach außen kippen
    [Export]
    public int PlateCount { get; set; } = 11;

    [Export]
    public Vector2 PlateLengthMeters { get; set; } = new(0.27f, 0.47f);

    [Export]
    public Vector2 PlateTiltDegrees { get; set; } = new(25f, 55f);

    //So breit ist die Kerbe, wo ein Riss den Krater verlässt, in Grad zu jeder Seite
    [Export]
    public float NotchDegrees { get; set; } = 24f;

    [Export]
    public float CoolSec { get; set; } = 0.8f;

    [Export]
    public float FadeSec { get; set; } = 0.6f;

    //Die Risse beginnen in center. Der Krater liegt so lange wie sie, gezählt ab jetzt. Lösen sich alle vorher auf, erkaltet er mit ihnen
    public static LavaCrater Open(PackedScene scene, Node parent, Vector3 center, IEnumerable<LavaCrack> cracks, double lifetimeSec)
    {
        var crater = scene.Instantiate<LavaCrater>();

        crater.durationSec = lifetimeSec;

        foreach (var crack in cracks)
        {
            crater.cracks.Add(crack);

            if (crack.Shape is { } shape && crater.FindExitAngle(shape, center) is { } angle)
                crater.exitAngles.Add(angle);
        }

        parent.AddChild(crater);

        crater.GlobalPosition = WorldScale.OnGround(center);

        return crater;
    }

    public override void _Ready()
    {
        var scale = new Vector3(RadiusMeters, 1f, RadiusMeters);

        if (Bowl is not null)
            Bowl.Scale = scale;

        if (Pool is not null)
            Pool.Scale = scale;

        if (Glow is not null)
            lightEnergy = Glow.LightEnergy;

        //Jeder Krater kühlt für sich ab, darum eigene Materialien
        var rock = Own(RockMaterial);
        var hot  = Own(HotMaterial);

        BuildRim(rock, hot);
        PlacePlates(rock);
        ShowCooling(1f, 0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isCooling)
            return;

        activeSec += delta;

        if (activeSec < durationSec && !AreCracksGone())
            return;

        isCooling = true;

        var tween = CreateTween();

        tween.TweenMethod(Callable.From<float>(value => ShowCooling(1f - value, 0f)), 0f, 1f, CoolSec);
        tween.TweenMethod(Callable.From<float>(value => ShowCooling(0f, value)), 0f, 1f, FadeSec);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private bool AreCracksGone()
        => cracks.Count > 0 && cracks.TrueForAll(crack => !IsInstanceValid(crack) || crack.IsCooling);

    private ShaderMaterial Own(ShaderMaterial material)
    {
        if (material is null)
            return null;

        var copy = (ShaderMaterial)material.Duplicate();

        glowing.Add((copy, copy.GetShaderParameter(EmissionParameter).AsSingle()));

        return copy;
    }

    //Die Richtung, in der der Riss den Rand kreuzt
    private float? FindExitAngle(FissureShape shape, Vector3 center)
    {
        var x = WorldScale.ToPx(center.X);
        var y = WorldScale.ToPx(center.Z);
        var r = WorldScale.ToPx(RadiusMeters * RimInnerShare);

        foreach (var point in shape.Points)
        {
            var dx = point.X - x;
            var dy = point.Y - y;

            if (dx * dx + dy * dy >= r * r)
                return Mathf.Atan2(dy, dx);
        }

        return shape.Points.Count > 1 ? Mathf.Atan2(shape.Points[^1].Y - y, shape.Points[^1].X - x) : null;
    }

    //Mulde und Pool zerfallen im Punktmuster, Rand und Platten sinken in den Boden
    private void ShowCooling(float heat, float fade)
    {
        foreach (var mesh in new[] { Bowl, Pool })
        {
            mesh?.SetInstanceShaderParameter(HeatParameter, heat);
            mesh?.SetInstanceShaderParameter(FadeParameter, fade);
        }

        foreach (var (material, energy) in glowing)
            material.SetShaderParameter(EmissionParameter, energy * heat);

        if (Rim is not null)
            Rim.Scale = new Vector3(1f, Mathf.Max(0.01f, 1f - fade), 1f);

        foreach (var (node, restY) in sinking)
            node.Position = node.Position with { Y = restY - fade * PlateLengthMeters.Y };

        if (Glow is not null)
            Glow.LightEnergy = lightEnergy * heat;
    }

    //Ein Ring aus schrägen Flächen: innen am Boden, oben ein zackiger Kamm, außen wieder am Boden. Kanten bleiben hart wie bei der PS1
    private void BuildRim(ShaderMaterial rock, ShaderMaterial hot)
    {
        if (Rim is null || RimSegments < 3)
            return;

        var random = GameRandom.Shared;
        var inner  = new Vector3[RimSegments];
        var top    = new Vector3[RimSegments];
        var outer  = new Vector3[RimSegments];

        for (var i = 0; i < RimSegments; i++)
        {
            var angle  = Mathf.Tau * (i + random.NextRange(-0.3f, 0.3f)) / RimSegments;
            var dir    = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var radius = RadiusMeters * random.NextRange(0.94f, 1.06f);

            inner[i] = dir * radius * RimInnerShare;
            top[i]   = dir * radius * random.NextRange(0.88f, 1f) + Vector3.Up * (RimHeightMeters * random.NextRange(0.4f, 1.4f) * GetNotch(angle));
            outer[i] = dir * radius * RimOuterShare * random.NextRange(0.95f, 1.1f);
        }

        var mesh = new ArrayMesh();

        AddRing(mesh, inner, top, hot);
        AddRing(mesh, top, outer, rock);

        Rim.Mesh = mesh;
    }

    private static void AddRing(ArrayMesh mesh, Vector3[] from, Vector3[] to, Material material)
    {
        var tool = new SurfaceTool();

        tool.Begin(Mesh.PrimitiveType.Triangles);

        for (var i = 0; i < from.Length; i++)
        {
            var j = (i + 1) % from.Length;

            AddTriangle(tool, from[i], from[j], to[j]);
            AddTriangle(tool, from[i], to[j], to[i]);
        }

        tool.GenerateNormals();
        tool.SetMaterial(material);
        tool.Commit(mesh);
    }

    //Kantige Platten auf dem Kamm, nach außen gekippt und halb im Boden, als hätte der Schlag den Boden aufgebrochen. In den Kerben liegt keine
    private void PlacePlates(ShaderMaterial rock)
    {
        var random = GameRandom.Shared;

        for (var i = 0; i < PlateCount; i++)
        {
            var angle = Mathf.Tau * (i + random.NextRange(-0.35f, 0.35f)) / PlateCount;

            if (GetNotch(angle) < 0.6f)
                continue;

            var length = random.NextRange(PlateLengthMeters.X, PlateLengthMeters.Y);
            var plate  = new MeshInstance3D
            {
                Mesh             = CreatePlateMesh(random, length, length * random.NextRange(0.45f, 0.7f), length * random.NextRange(0.18f, 0.28f)),
                MaterialOverride = rock,
                CastShadow       = GeometryInstance3D.ShadowCastingSetting.Off
            };

            var dir    = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var radius = RadiusMeters * random.NextRange(0.85f, 1.05f);
            var tilt   = Mathf.DegToRad(random.NextRange(PlateTiltDegrees.X, PlateTiltDegrees.Y));

            //Die lange Seite (X) liegt längs des Rands, +Z zeigt zur Mitte. Gekippt um X hebt sich die Innenkante
            var along = new Vector3(-dir.Z, 0f, dir.X);
            var frame = new Basis(along, Vector3.Up, -dir);

            plate.Basis    = frame * new Basis(Vector3.Right, -tilt) * new Basis(Vector3.Up, random.NextRange(-0.2f, 0.2f));
            plate.Position = dir * radius + Vector3.Up * (RimHeightMeters * 0.5f);

            AddChild(plate);
            sinking.Add((plate, plate.Position.Y));
        }
    }

    //Ein schiefer Quader, jede Ecke etwas verschoben. Längs X, dick in Y, breit in Z
    private static ArrayMesh CreatePlateMesh(IRandomSource random, float length, float width, float thickness)
    {
        var corners = new Vector3[8];

        for (var i = 0; i < corners.Length; i++)
        {
            var x = (i & 1) == 0 ? -0.5f : 0.5f;
            var y = (i & 2) == 0 ? -0.5f : 0.5f;
            var z = (i & 4) == 0 ? -0.5f : 0.5f;

            corners[i] = new Vector3((x + random.NextRange(-0.15f, 0.15f)) * length,
                                     (y + random.NextRange(-0.3f, 0.3f)) * thickness,
                                     (z + random.NextRange(-0.15f, 0.15f)) * width);
        }

        int[][] faces = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]];

        var tool = new SurfaceTool();

        tool.Begin(Mesh.PrimitiveType.Triangles);

        foreach (var face in faces)
        {
            AddTriangle(tool, corners[face[0]], corners[face[1]], corners[face[2]], Vector3.Zero);
            AddTriangle(tool, corners[face[0]], corners[face[2]], corners[face[3]], Vector3.Zero);
        }

        tool.GenerateNormals();

        return tool.Commit();
    }

    //Godot nimmt Dreiecke im Uhrzeigersinn als Vorderseite, das Kreuzprodukt der Kanten zeigt dann von ihr weg.
    //Ohne inside zeigt die Vorderseite nach oben, sonst von inside weg
    private static void AddTriangle(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c, Vector3? inside = null)
    {
        var away = inside is { } point ? (a + b + c) / 3f - point : Vector3.Up;

        if ((b - a).Cross(c - a).Dot(away) > 0f)
            (b, c) = (c, b);

        tool.AddVertex(a);
        tool.AddVertex(b);
        tool.AddVertex(c);
    }

    //Wo ein Riss hinausläuft, ist der Rand fast flach, damit die Lava sichtbar abfließt
    private float GetNotch(float angle)
    {
        var notch = 1f;

        foreach (var exit in exitAngles)
        {
            var distance = Mathf.Abs(Mathf.RadToDeg(Mathf.AngleDifference(angle, exit)));

            notch = Mathf.Min(notch, Mathf.Clamp((distance - NotchDegrees * 0.4f) / (NotchDegrees * 0.6f), 0.08f, 1f));
        }

        return notch;
    }
}
