using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der Pfeil eines geladenen Schusses. Je weiter geladen, desto größer und heller sein Leuchten, in der Farbe aus GlowColors, durchstoßend zieht er einen Schweif
public partial class ChargedArrow : SkillProjectile
{
    [Export]
    public MeshInstance3D Glow { get; set; }

    [Export]
    public Node3D Trail { get; set; }

    [Export]
    public OmniLight3D Light { get; set; }

    [Export]
    public Gradient GlowColors { get; set; }

    [Export]
    public float GlowMinScale { get; set; } = 0.5f;

    [Export]
    public float GlowMaxScale { get; set; } = 1.6f;

    [Export]
    public float GlowMinEnergy { get; set; } = 1f;

    [Export]
    public float GlowMaxEnergy { get; set; } = 4f;

    public float ChargeShare { get; private set; }

    public bool IsPiercing { get; private set; }

    //Vor dem Einhängen in den Szenenbaum aufrufen. share: 0 bei der Mindestladung, 1 am Maximum
    public void ShowCharge(float share, bool pierces)
    {
        ChargeShare = Mathf.Clamp(share, 0f, 1f);
        IsPiercing  = pierces;

        var color = Glowing.Sample(GlowColors, ChargeShare);

        if (Glow is not null)
            Glowing.Apply(Glow, Mathf.Lerp(GlowMinScale, GlowMaxScale, ChargeShare), Mathf.Lerp(GlowMinEnergy, GlowMaxEnergy, ChargeShare), color);

        if (Light is not null && color is { } lightColor)
            Light.LightColor = lightColor;

        if (Trail is not null)
            Trail.Visible = IsPiercing;
    }
}
