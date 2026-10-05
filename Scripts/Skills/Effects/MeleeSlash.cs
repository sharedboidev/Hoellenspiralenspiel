using System;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein Hieb als leuchtender Bogen in Hüfthöhe. Er läuft wie die Waffe von rechts nach links und zieht einen Schweif hinter sich her,
//der vorne breit und hell ist und hinten spitz ausläuft. Das Netz entsteht jedes Frame neu
public partial class MeleeSlash : Node3D
{
    private ImmediateMesh mesh;
    private double        elapsedSec;
    private float         arcDegrees;
    private float         radius;

    [Export]
    public MeshInstance3D Band { get; set; }

    //Gilt, wenn der Skill keinen eigenen Bogen mitgibt
    [Export(PropertyHint.Range, "1, 360, 1")]
    public float ArcDegrees { get; set; } = 100f;

    //Breite des Bands an der Spitze in Teilen des Radius
    [Export(PropertyHint.Range, "0.05, 1, 0.01")]
    public float WidthFraction { get; set; } = 0.3f;

    [Export]
    public float HeightMeters { get; set; } = 1f;

    [Export]
    public float SweepSec { get; set; } = 0.15f;

    //So lange nach der Spitze folgt das Ende des Schweifs
    [Export]
    public float TrailSec { get; set; } = 0.12f;

    [Export]
    public int Segments { get; set; } = 24;

    [Export]
    public Color Color { get; set; } = new(1f, 0.95f, 0.85f);

    //Deckkraft am Ende des Schweifs, die Spitze deckt ganz
    [Export(PropertyHint.Range, "0, 1, 0.05")]
    public float TailAlpha { get; set; } = 0.25f;

    //Vor dem Einhängen in den Szenenbaum aufrufen. Ein Bogen von 0 nimmt den der Szene
    public void Launch(Vector3 facing, float radiusMeters, float sweepDegrees = 0f)
    {
        radius     = Math.Max(0.05f, radiusMeters);
        arcDegrees = sweepDegrees > 0f ? sweepDegrees : ArcDegrees;

        if (facing.LengthSquared() > 0.0001f)
            Rotation = new Vector3(0, Mathf.Atan2(-facing.X, -facing.Z), 0);
    }

    public override void _Ready()
    {
        if (radius <= 0f)
            radius = 1f;

        if (arcDegrees <= 0f)
            arcDegrees = ArcDegrees;

        mesh = new ImmediateMesh();

        if (Band is not null)
        {
            Band.Mesh     = mesh;
            Band.Position = new Vector3(0, HeightMeters, 0);
        }

        Draw();
    }

    public override void _Process(double delta)
    {
        elapsedSec += delta;

        if (elapsedSec >= SweepSec + TrailSec)
        {
            QueueFree();

            return;
        }

        Draw();
    }

    private void Draw()
    {
        mesh.ClearSurfaces();

        var head = (float)Math.Clamp(elapsedSec / SweepSec, 0, 1);
        var tail = (float)Math.Clamp((elapsedSec - TrailSec) / SweepSec, 0, 1);

        if (head - tail <= 0.001f)
            return;

        var start = Mathf.DegToRad(-arcDegrees / 2f + arcDegrees * tail);
        var end   = Mathf.DegToRad(-arcDegrees / 2f + arcDegrees * head);
        var width = radius * WidthFraction;
        var steps = Math.Max(2, (int)Math.Ceiling(Segments * (head - tail)));

        mesh.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);

        for (var i = 0; i <= steps; i++)
        {
            var along  = (float)i / steps;
            var angle  = Mathf.Lerp(start, end, along);
            var inner  = radius - width * along;
            var shine  = new Color(Color, Mathf.Lerp(TailAlpha, 1f, along));
            var toward = new Vector3(-Mathf.Sin(angle), 0, -Mathf.Cos(angle));

            mesh.SurfaceSetNormal(Vector3.Up);
            mesh.SurfaceSetColor(shine);
            mesh.SurfaceAddVertex(toward * radius);
            mesh.SurfaceSetNormal(Vector3.Up);
            mesh.SurfaceSetColor(shine);
            mesh.SurfaceAddVertex(toward * inner);
        }

        mesh.SurfaceEnd();
    }
}
