using System;
using System.Collections.Generic;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Splitter, die aus einem Punkt nach außen fliegen, sich drehen, kurz am Boden abprallen und schrumpfend verschwinden. Nur zum Ansehen, sie treffen nichts.
//Die meisten sind klein, wenige mittelgroß. Der Boden liegt auf der Höhe des Knotens. Er hängt sich beim Start an den Level,
//so überdauert er eine Fläche, die kürzer lebt als ihre Splitter
public partial class ShardBurst : Node3D
{
    private const int   MeshVariants  = 4;
    private const float GroundOffset  = 0.02f;
    private const float ShrinkSec     = 0.15f;
    private const float BounceDamping = 0.3f;

    private readonly List<Shard> shards = new();

    private double ageSec;

    [Export]
    public Material Material { get; set; }

    [Export]
    public int Count { get; set; } = 36;

    //Länge eines Splitters in Metern. Kleine kommen viel öfter als große
    [Export]
    public Vector2 LengthMeters { get; set; } = new(0.12f, 0.45f);

    //Tempo nach außen und nach oben in Metern je Sekunde
    [Export]
    public Vector2 SpeedMeters { get; set; } = new(2.5f, 6.5f);

    [Export]
    public Vector2 UpSpeedMeters { get; set; } = new(1.5f, 4.5f);

    [Export]
    public float Gravity { get; set; } = 12f;

    [Export]
    public Vector2 LifetimeSec { get; set; } = new(0.45f, 0.8f);

    //Aus dieser Höhe über dem Boden fliegen sie los, etwa der Mitte des Körpers
    [Export]
    public float StartHeight { get; set; } = 0.8f;

    public override void _Ready()
    {
        var meshes = new Mesh[MeshVariants];

        for (var i = 0; i < meshes.Length; i++)
            meshes[i] = CreateShardMesh();

        for (var i = 0; i < Count; i++)
            shards.Add(Launch(meshes[i % meshes.Length]));

        Callable.From(MoveToLevel).CallDeferred();
    }

    public override void _Process(double delta)
    {
        ageSec += delta;

        var dt     = (float)delta;
        var flying = 0;

        foreach (var shard in shards)
        {
            if (ageSec >= shard.LifetimeSec)
            {
                shard.Mesh.Visible = false;

                continue;
            }

            flying++;

            shard.Velocity.Y -= Gravity * dt;
            shard.Position   += shard.Velocity * dt;

            if (shard.Position.Y < GroundOffset)
            {
                shard.Position.Y =  GroundOffset;
                shard.Velocity   *= BounceDamping;
                shard.Velocity.Y =  Math.Abs(shard.Velocity.Y);
                shard.Spin       *= BounceDamping;
            }

            shard.Angle += shard.Spin * dt;

            var shrink = Math.Clamp((float)(shard.LifetimeSec - ageSec) / ShrinkSec, 0f, 1f);

            shard.Mesh.Transform = new Transform3D(new Basis(shard.Axis, shard.Angle) * shard.Start.Scaled(Vector3.One * shrink), shard.Position);
        }

        if (flying == 0)
            QueueFree();
    }

    private Shard Launch(Mesh mesh)
    {
        var angle     = (float)GD.RandRange(0, Mathf.Tau);
        var outward   = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        var velocity  = outward * (float)GD.RandRange(SpeedMeters.X, SpeedMeters.Y) + Vector3.Up * (float)GD.RandRange(UpSpeedMeters.X, UpSpeedMeters.Y);
        var length    = LengthMeters.X + (LengthMeters.Y - LengthMeters.X) * Mathf.Pow(GD.Randf(), 1.6f);
        var thickness = length * (float)GD.RandRange(0.2, 0.3);
        var start     = new Basis(new Quaternion(Vector3.Up, velocity.Normalized())).Scaled(new Vector3(thickness, length, thickness));

        var instance = new MeshInstance3D
        {
            Mesh       = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        //Als Override der Fläche, nur den stimmt Ps1Look auf das Raster ab
        instance.SetSurfaceOverrideMaterial(0, Material);

        AddChild(instance);

        var shard = new Shard
        {
            Mesh        = instance,
            Position    = new Vector3(outward.X * 0.1f, StartHeight * (float)GD.RandRange(0.7, 1.2), outward.Z * 0.1f),
            Velocity    = velocity,
            Start       = start,
            Axis        = RandomAxis(),
            Spin        = (float)GD.RandRange(6, 18) * (GD.Randf() < 0.5f ? -1f : 1f),
            LifetimeSec = GD.RandRange(LifetimeSec.X, LifetimeSec.Y)
        };

        instance.Transform = new Transform3D(start, shard.Position);

        return shard;
    }

    //Eine Doppelpyramide mit drei unregelmäßigen Kanten, spitz an beiden Enden, Länge 1 entlang Y. Die Mitte sitzt nicht genau in der Mitte.
    //FrostRing baut seine Kristalle aus derselben Form
    public static Mesh CreateShardMesh()
    {
        var waist  = (float)GD.RandRange(0.3, 0.6) - 0.5f;
        var top    = new Vector3(0f, 0.5f, 0f);
        var bottom = new Vector3(0f, -0.5f, 0f);
        var ring   = new Vector3[3];

        for (var i = 0; i < ring.Length; i++)
        {
            var angle  = i * Mathf.Tau / 3f + (float)GD.RandRange(-0.4, 0.4);
            var radius = (float)GD.RandRange(0.35, 0.5);

            ring[i] = new Vector3(Mathf.Cos(angle) * radius, waist, Mathf.Sin(angle) * radius);
        }

        var surface = new SurfaceTool();

        surface.Begin(Mesh.PrimitiveType.Triangles);

        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Length];

            AddFace(surface, top, b, a);
            AddFace(surface, bottom, a, b);
        }

        return surface.Commit();
    }

    //Jede Fläche mit eigener Normale nach außen, so bleiben die Kanten scharf. Sie steht in beiden Umlaufrichtungen darin,
    //so zeigt sie nach außen, egal welche Richtung als Vorderseite zählt
    private static void AddFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = (b - a).Cross(c - a).Normalized();

        if (normal.Dot((a + b + c) / 3f) < 0f)
            normal = -normal;

        AddTriangle(surface, normal, a, b, c);
        AddTriangle(surface, normal, a, c, b);
    }

    private static void AddTriangle(SurfaceTool surface, Vector3 normal, params Vector3[] corners)
    {
        foreach (var corner in corners)
        {
            surface.SetNormal(normal);
            surface.SetUV(new Vector2(corner.X + 0.5f, corner.Y + 0.5f));
            surface.AddVertex(corner);
        }
    }

    private static Vector3 RandomAxis()
    {
        var axis = new Vector3(GD.Randf() - 0.5f, GD.Randf() - 0.5f, GD.Randf() - 0.5f);

        return axis.LengthSquared() < 0.0001f ? Vector3.Right : axis.Normalized();
    }

    private void MoveToLevel()
    {
        var level = GetParent()?.GetParent();

        if (IsInstanceValid(level))
            Reparent(level);
    }

    private sealed class Shard
    {
        public MeshInstance3D Mesh;
        public Vector3        Position;
        public Vector3        Velocity;
        public Basis          Start;
        public Vector3        Axis;
        public float          Angle;
        public float          Spin;
        public double         LifetimeSec;
    }
}
