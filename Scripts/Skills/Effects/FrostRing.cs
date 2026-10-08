using System;
using System.Collections.Generic;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein Kranz kleiner Eiskristalle am Rand einer Fläche, schräg nach außen gestellt, mit derselben Form wie die Splitter aus ShardBurst.
//Er hängt unter Visual einer SkillArea und wächst mit ihr. Jeder Kristall gleicht die Streckung von Visual aus und bleibt so scharfkantig.
//Gegen Ende seiner Lebensdauer schrumpfen sie weg
public partial class FrostRing : Node3D
{
    private const int MeshVariants = 4;

    private readonly List<(MeshInstance3D Mesh, Vector3 Position, Basis Shape)> crystals = new();

    private double ageSec;

    [Export]
    public Material Material { get; set; }

    [Export]
    public int Count { get; set; } = 44;

    //Länge eines Kristalls in Metern. Kleine kommen öfter als große
    [Export]
    public Vector2 LengthMeters { get; set; } = new(0.1f, 0.28f);

    //So steil stehen sie vom Boden nach außen
    [Export]
    public Vector2 TiltDegrees { get; set; } = new(20f, 60f);

    //Anteil des Radius, auf dem sie stehen. Ein wenig Streuung nach innen wirkt weniger wie mit dem Zirkel gezogen
    [Export]
    public Vector2 RadiusShare { get; set; } = new(0.85f, 1f);

    //Sollte zur Lebensdauer der Fläche passen: Ausdehnung plus LingerSec
    [Export]
    public float LifetimeSec { get; set; } = 0.4f;

    [Export]
    public float ShrinkSec { get; set; } = 0.15f;

    public override void _Ready()
    {
        var meshes = new Mesh[MeshVariants];

        for (var i = 0; i < meshes.Length; i++)
            meshes[i] = ShardBurst.CreateShardMesh();

        for (var i = 0; i < Count; i++)
        {
            var angle  = (i + (float)GD.RandRange(-0.4, 0.4)) * Mathf.Tau / Count;
            var tilt   = Mathf.DegToRad((float)GD.RandRange(TiltDegrees.X, TiltDegrees.Y));
            var radius = (float)GD.RandRange(RadiusShare.X, RadiusShare.Y);
            var length = LengthMeters.X + (LengthMeters.Y - LengthMeters.X) * Mathf.Pow(GD.Randf(), 1.6f);
            var width  = length * (float)GD.RandRange(0.2, 0.3);

            var direction = new Vector3(Mathf.Cos(angle) * Mathf.Cos(tilt), Mathf.Sin(tilt), Mathf.Sin(angle) * Mathf.Cos(tilt));
            var shape     = new Basis(new Quaternion(Vector3.Up, direction)) * Basis.FromScale(new Vector3(width, length, width));

            var mesh = new MeshInstance3D
            {
                Mesh       = meshes[i % meshes.Length],
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };

            //Als Override der Fläche, nur den stimmt Ps1Look auf das Raster ab
            mesh.SetSurfaceOverrideMaterial(0, Material);

            AddChild(mesh);

            //Der Fuß steht auf dem Boden, die Mitte des Kristalls liegt eine halbe Länge entlang seiner Richtung
            crystals.Add((mesh, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius), shape));
        }

        Place(1f);
    }

    public override void _Process(double delta)
    {
        ageSec += delta;

        Place(Math.Clamp((float)(LifetimeSec - ageSec) / Math.Max(ShrinkSec, 0.001f), 0f, 1f));
    }

    private void Place(float size)
    {
        var stretch  = GetParentOrNull<Node3D>()?.Scale ?? Vector3.One;
        var undo     = Basis.FromScale(new Vector3(1f / Math.Max(stretch.X, 0.001f), 1f / Math.Max(stretch.Y, 0.001f), 1f / Math.Max(stretch.Z, 0.001f)));
        var shrunken = Basis.FromScale(Vector3.One * Math.Max(size, 0.001f));

        foreach (var (mesh, position, shape) in crystals)
        {
            var tip = undo * (shape * shrunken).Y * 0.5f;

            mesh.Transform = new Transform3D(undo * shape * shrunken, position + tip);
        }
    }
}
