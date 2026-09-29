using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class EliteAura : Node3D
{
    private const float  RingLiftMeters  = 0.04f;
    private const float  RingFlatness    = 0.3f;
    private const float  RingEnergy      = 2f;
    private const float  LightLiftMeters = 0.6f;
    private const float  LightDimEnergy  = 0.8f;
    private const float  LightFullEnergy = 1.8f;
    private const float  LightRangeRadii = 5f;
    private const double PulseSec        = 1.4;

    private static readonly Shader SurfaceShader = ResourceLoader.Load<Shader>("res://Shaders/Ps1/ps1_surface.gdshader");

    private Color color;
    private float radiusMeters;

    public OmniLight3D Light { get; private set; }

    public static EliteAura Create(Color color, float radiusMeters)
        => new() { Name = "EliteAura", color = color, radiusMeters = radiusMeters };

    public override void _Ready()
    {
        AddChild(CreateRing());

        Light = new OmniLight3D
        {
            LightColor    = color,
            LightEnergy   = LightDimEnergy,
            OmniRange     = radiusMeters * LightRangeRadii,
            ShadowEnabled = false,
            Position      = Vector3.Up * LightLiftMeters
        };

        AddChild(Light);

        var pulse = CreateTween().SetLoops();

        pulse.TweenProperty(Light, "light_energy", LightFullEnergy, PulseSec / 2).SetTrans(Tween.TransitionType.Sine);
        pulse.TweenProperty(Light, "light_energy", LightDimEnergy, PulseSec / 2).SetTrans(Tween.TransitionType.Sine);
    }

    private MeshInstance3D CreateRing()
    {
        var material = new ShaderMaterial { Shader = SurfaceShader };

        material.SetShaderParameter("albedo", Colors.Black);
        material.SetShaderParameter("emission", color);
        material.SetShaderParameter("emission_energy", RingEnergy);

        return new MeshInstance3D
        {
            Name             = "Ring",
            Mesh             = new TorusMesh { InnerRadius = radiusMeters * 0.88f, OuterRadius = radiusMeters, Rings = 16, RingSegments = 4 },
            MaterialOverride = material,
            CastShadow       = GeometryInstance3D.ShadowCastingSetting.Off,
            Position         = Vector3.Up * RingLiftMeters,
            Scale            = new Vector3(1, RingFlatness, 1)
        };
    }
}
