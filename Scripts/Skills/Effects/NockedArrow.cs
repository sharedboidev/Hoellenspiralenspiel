using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der Pfeil, der beim Laden im Bogen liegt. Er liegt in seiner Szene entlang -Z, die Spitze vorn, die Kerbe bei NockOffsetMeters.
//Unter der Mindestladung ist er ein gewöhnlicher Pfeil, dann leuchtet er mit der Ladung, über voller Ladung zieht er einen Schweif.
//Die Farbe des Leuchtens folgt GlowColors von der Mindestladung (0) bis zum Maximum (1)
public partial class NockedArrow : Node3D
{
    [Export]
    public MeshInstance3D Glow { get; set; }

    [Export]
    public Node3D Trail { get; set; }

    [Export]
    public Gradient GlowColors { get; set; }

    //So weit hinter der Mitte liegt die Kerbe, die an der Sehne sitzt
    [Export]
    public float NockOffsetMeters { get; set; } = 0.4f;

    [Export]
    public float GlowMinScale { get; set; } = 0.4f;

    [Export]
    public float GlowMaxScale { get; set; } = 1.4f;

    [Export]
    public float GlowMinEnergy { get; set; } = 0.8f;

    [Export]
    public float GlowMaxEnergy { get; set; } = 4f;

    public void Show(float share, bool canFire, bool pierces)
    {
        share = Mathf.Clamp(share, 0f, 1f);

        if (Glow is not null)
        {
            Glow.Visible = canFire;

            Glowing.Apply(Glow, Mathf.Lerp(GlowMinScale, GlowMaxScale, share), Mathf.Lerp(GlowMinEnergy, GlowMaxEnergy, share), Glowing.Sample(GlowColors, share));
        }

        if (Trail is not null)
            Trail.Visible = pierces;
    }
}
