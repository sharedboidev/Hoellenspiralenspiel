using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der Schweif eines Wirbels: ein leuchtender Bogen in Hüfthöhe, der an der Waffe des Wirbelnden beginnt und hinter ihr herzieht,
//vorne breit und hell, hinten spitz und blass. Er hängt als Kind am Wirbelnden, läuft also mit ihm mit, und liest jedes Frame
//seine Blickrichtung, die beim Wirbel mit der Waffe dreht. So kann er weder hinterherhinken noch stehen bleiben.
//Er wächst mit dem Weg, den die Waffe zurücklegt, bis TrailDegrees, und zieht sich nach Dismiss in FadeSec in die Waffe zurück
public partial class WhirlTrail : Node3D
{
    private const float MinSpanRad = 0.001f;

    private ImmediateMesh mesh;
    private BaseUnit      owner;
    private float         radius;
    private float         headRad;
    private float         spunRad;
    private double        fadedSec;
    private bool          isFading;

    [Export]
    public MeshInstance3D Band { get; set; }

    //So weit reicht der Schweif hinter der Waffe
    [Export(PropertyHint.Range, "10, 360, 1")]
    public float TrailDegrees { get; set; } = 200f;

    //Breite des Bands an der Waffe in Teilen des Radius
    [Export(PropertyHint.Range, "0.05, 1, 0.01")]
    public float WidthFraction { get; set; } = 0.4f;

    [Export]
    public float HeightMeters { get; set; } = 0.9f;

    //So lange braucht das Ende des Schweifs nach dem Wirbel, um die Waffe einzuholen
    [Export]
    public float FadeSec { get; set; } = 0.2f;

    //Für den vollen Schweif, ein kürzerer braucht weniger
    [Export]
    public int Segments { get; set; } = 48;

    [Export]
    public Color Color { get; set; } = new(0.8f, 0.9f, 1f);

    //Deckkraft am Ende des Schweifs, an der Waffe deckt er ganz
    [Export(PropertyHint.Range, "0, 1, 0.05")]
    public float TailAlpha { get; set; } = 0.15f;

    //Hängt den Schweif an den Wirbelnden. Ohne Szene oder mit einer fremden Wurzel gibt es keinen
    public static WhirlTrail Show(PackedScene scene, BaseUnit owner, float radiusMeters)
    {
        if (scene is null || owner is null)
            return null;

        var trail = scene.InstantiateOrNull<WhirlTrail>();

        if (trail is null)
        {
            GD.PushWarning($"{scene.ResourcePath} hat keinen {nameof(WhirlTrail)} als Wurzel.");

            return null;
        }

        trail.owner  = owner;
        trail.radius = Math.Max(0.05f, radiusMeters);

        owner.AddChild(trail);

        return trail;
    }

    //Der Wirbel ist vorbei, der Schweif zieht sich in die Waffe zurück und verschwindet
    public void Dismiss()
        => isFading = true;

    public override void _Ready()
    {
        mesh = new ImmediateMesh();

        if (Band is not null)
        {
            Band.Mesh     = mesh;
            Band.Position = new Vector3(0, HeightMeters, 0);
        }

        //Der Wirbelnde dreht nur seine Darstellung, nicht sich selbst. Falls doch, bleibt der Schweif am Boden ausgerichtet
        GlobalBasis = Basis.Identity;
        headRad     = GetHeadRad();

        Draw();
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(owner))
        {
            QueueFree();

            return;
        }

        var head = GetHeadRad();

        spunRad += Mathf.Wrap(head - headRad, -Mathf.Pi, Mathf.Pi);
        spunRad =  Math.Clamp(spunRad, -Mathf.DegToRad(TrailDegrees), Mathf.DegToRad(TrailDegrees));
        headRad =  head;

        if (isFading)
        {
            fadedSec += delta;

            if (fadedSec >= FadeSec)
            {
                QueueFree();

                return;
            }
        }

        Draw();
    }

    //Die Blickrichtung des Wirbelnden als Winkel, wie BaseUnit.Face ihn in die Darstellung schreibt
    private float GetHeadRad()
    {
        var facing = owner?.FacingDirection ?? Vector3.Zero;

        return facing.LengthSquared() < 0.0001f ? headRad : Mathf.Atan2(-facing.X, -facing.Z);
    }

    private float GetShownSpanRad()
    {
        if (!isFading)
            return spunRad;

        return spunRad * (float)Math.Clamp(1 - fadedSec / Math.Max(0.001f, FadeSec), 0, 1);
    }

    private void Draw()
    {
        mesh.ClearSurfaces();

        var span = GetShownSpanRad();

        if (Math.Abs(span) <= MinSpanRad)
            return;

        var tail  = headRad - span;
        var width = radius * WidthFraction;
        var steps = Math.Max(2, (int)Math.Ceiling(Segments * Math.Abs(span) / Mathf.DegToRad(TrailDegrees)));

        mesh.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);

        for (var i = 0; i <= steps; i++)
        {
            var along  = (float)i / steps;
            var angle  = Mathf.Lerp(tail, headRad, along);
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
