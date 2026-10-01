using Godot;

namespace Hoellenspiralenspiel.Scripts.Environment;

//Erscheint im Boss-Raum, sobald der Boss gefallen ist, und führt zurück in den Hub. Es wächst aus dem Boden, dreht sich und pulsiert
public partial class BossPortal : Passage
{
    private const float MinScale          = 0.05f;
    private const float SpinRadiansPerSec = 0.9f;
    private const float PulsesPerSec      = 1.4f;

    private double age;

    [Export]
    public Node3D Look { get; set; }

    //Die Scheibe, die sich dreht
    [Export]
    public Node3D Vortex { get; set; }

    [Export]
    public OmniLight3D Light { get; set; }

    [Export]
    public double OpeningSec { get; set; } = 1.2;

    [Export]
    public float LightEnergy { get; set; } = 3f;

    //Anteil, um den das Licht schwankt
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float Pulse { get; set; } = 0.5f;

    public override bool IsOpen => age >= OpeningSec;

    public override void _Process(double delta)
    {
        age += delta;

        var share = OpeningSec <= 0 ? 1f : Mathf.Clamp((float)(age / OpeningSec), MinScale, 1f);

        if (Look is not null)
            Look.Scale = Vector3.One * share;

        Vortex?.RotateObjectLocal(Vector3.Back, (float)delta * SpinRadiansPerSec);

        if (Light is not null)
            Light.LightEnergy = LightEnergy * share * (1f + Pulse * 0.5f * Mathf.Sin((float)age * Mathf.Tau * PulsesPerSec));
    }
}
