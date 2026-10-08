using System;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein gezackter Blitz zwischen zwei Punkten, der kurz flackert und verlischt. Das Band zeigt zur Kamera und wird bei jedem Flackern neu gezackt.
//Das Licht steht am Ende, wo der Blitz einschlägt
public partial class ChainArc : Node3D
{
    private ImmediateMesh mesh;
    private double        elapsedSec;
    private double        sinceFlickerSec;
    private Vector3       from;
    private Vector3       to;
    private float         lightEnergy;

    [Export]
    public MeshInstance3D Bolt { get; set; }

    [Export]
    public OmniLight3D Flash { get; set; }

    [Export]
    public float WidthMeters { get; set; } = 0.08f;

    //Knicke je Meter, mindestens zwei Abschnitte
    [Export]
    public float KinksPerMeter { get; set; } = 1.5f;

    //So weit weicht ein Knick höchstens zur Seite aus
    [Export]
    public float JitterMeters { get; set; } = 0.25f;

    [Export]
    public double LifetimeSec { get; set; } = 0.25;

    [Export]
    public double FlickerSec { get; set; } = 0.05;

    [Export]
    public Color Color { get; set; } = new(0.75f, 0.9f, 1f);

    //Vor dem Einhängen in den Szenenbaum aufrufen, beide Punkte in Weltkoordinaten
    public void Launch(Vector3 start, Vector3 end)
    {
        from = start;
        to   = end;
    }

    public override void _Ready()
    {
        //Die Punkte gelten in der Welt, gleich wo der Blitz hängt
        TopLevel       = true;
        GlobalPosition = from;
        mesh           = new ImmediateMesh();

        if (Bolt is not null)
            Bolt.Mesh = mesh;

        if (Flash is not null)
        {
            lightEnergy          = Flash.LightEnergy;
            Flash.GlobalPosition = to;
        }

        Draw(1f);
    }

    public override void _Process(double delta)
    {
        elapsedSec      += delta;
        sinceFlickerSec += delta;

        if (elapsedSec >= LifetimeSec)
        {
            QueueFree();

            return;
        }

        var alpha = 1f - (float)(elapsedSec / Math.Max(0.001, LifetimeSec));

        if (Flash is not null)
            Flash.LightEnergy = lightEnergy * alpha;

        if (sinceFlickerSec < FlickerSec)
            return;

        sinceFlickerSec = 0;

        Draw(alpha);
    }

    private void Draw(float alpha)
    {
        mesh.ClearSurfaces();

        var span   = to - from;
        var length = span.Length();

        if (length < 0.01f)
            return;

        var along    = span / length;
        var toCamera = GetViewport()?.GetCamera3D() is { } camera ? (camera.GlobalPosition - from).Normalized() : Vector3.Up;
        var side     = along.Cross(toCamera);

        if (side.LengthSquared() < 0.0001f)
            side = along.Cross(Vector3.Up).LengthSquared() > 0.0001f ? along.Cross(Vector3.Up) : Vector3.Right;

        side = side.Normalized();

        var up       = side.Cross(along).Normalized();
        var segments = Math.Max(2, (int)Math.Ceiling(length * KinksPerMeter));
        var color    = new Color(Color, alpha);
        var half     = side * WidthMeters / 2f;

        mesh.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);

        for (var i = 0; i <= segments; i++)
        {
            //Anfang und Ende sitzen fest, dazwischen springt jeder Knick zur Seite und nach oben oder unten
            var point = span * i / segments;

            if (i > 0 && i < segments)
                point += side * Jitter() + up * Jitter();

            mesh.SurfaceSetColor(color);
            mesh.SurfaceAddVertex(point + half);
            mesh.SurfaceSetColor(color);
            mesh.SurfaceAddVertex(point - half);
        }

        mesh.SurfaceEnd();
    }

    private float Jitter()
        => (GD.Randf() * 2f - 1f) * JitterMeters;
}
