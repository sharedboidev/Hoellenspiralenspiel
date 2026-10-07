using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der Ring am Boden unter dem Helden, solange er einen Schuss lädt. Er erscheint mit der Mindestladung, wächst mit ihr und pulsiert.
//Der Ring der Szene hat den Radius 1 m, die Skalierung macht daraus den Radius der Ladung
public partial class ChargeAura : Node3D
{
    private double pulseSec;
    private float  radius;

    [Export]
    public MeshInstance3D Ring { get; set; }

    [Export]
    public float MinRadiusMeters { get; set; } = 0.35f;

    [Export]
    public float MaxRadiusMeters { get; set; } = 0.9f;

    [Export]
    public float PulsesPerSec { get; set; } = 2f;

    //So viel wächst und schrumpft der Ring beim Pulsieren, als Anteil seines Radius
    [Export]
    public float PulseShare { get; set; } = 0.12f;

    [Export]
    public float MinEnergy { get; set; } = 1.5f;

    [Export]
    public float MaxEnergy { get; set; } = 4f;

    public float Radius => radius;

    public void Show(float share, bool isVisible)
    {
        share   = Mathf.Clamp(share, 0f, 1f);
        radius  = Mathf.Lerp(MinRadiusMeters, MaxRadiusMeters, share);
        Visible = isVisible;

        if (Ring is not null)
            Glowing.Apply(Ring, radius, Mathf.Lerp(MinEnergy, MaxEnergy, share));
    }

    public override void _Process(double delta)
    {
        if (!Visible || Ring is null)
            return;

        pulseSec += delta;

        var pulse = 1f + PulseShare * Mathf.Sin((float)(pulseSec * PulsesPerSec * Mathf.Tau));

        Ring.Scale = new Vector3(radius * pulse, radius, radius * pulse);
    }
}
