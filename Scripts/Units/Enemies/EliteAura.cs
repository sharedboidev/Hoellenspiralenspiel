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

    private Color          color;
    private float          pulse = LightDimEnergy;
    private float          radiusMeters;
    private ShaderMaterial ringMaterial;
    private float          visibility = 1f;

    public OmniLight3D Light { get; private set; }

    //Wie gut der Träger zu sehen ist. Das Licht der Aura folgt dem, der Ring blendet wie der Körper ein
    public float Visibility
    {
        get => visibility;
        set
        {
            visibility = Mathf.Clamp(value, 0f, 1f);

            ShowLight();
        }
    }

    public static EliteAura Create(Color color, float radiusMeters)
        => new() { Name = "EliteAura", color = color, radiusMeters = radiusMeters };

    public override void _Ready()
    {
        AddChild(CreateRing());

        Light = new OmniLight3D
        {
            LightColor    = color,
            OmniRange     = radiusMeters * LightRangeRadii,
            ShadowEnabled = false,
            Position      = Vector3.Up * LightLiftMeters
        };

        AddChild(Light);
        ShowLight();

        var pulsing = CreateTween().SetLoops();

        pulsing.TweenMethod(Callable.From<float>(SetPulse), LightDimEnergy, LightFullEnergy, PulseSec / 2).SetTrans(Tween.TransitionType.Sine);
        pulsing.TweenMethod(Callable.From<float>(SetPulse), LightFullEnergy, LightDimEnergy, PulseSec / 2).SetTrans(Tween.TransitionType.Sine);
    }

    public override void _ExitTree()
        => UnitSight.Release(ringMaterial);

    private void SetPulse(float energy)
    {
        pulse = energy;

        ShowLight();
    }

    private void ShowLight()
    {
        if (Light is not null)
            Light.LightEnergy = pulse * visibility;
    }

    private MeshInstance3D CreateRing()
    {
        ringMaterial = UnitSight.Register(new ShaderMaterial { Shader = UnitSight.Shader });

        ringMaterial.SetShaderParameter("albedo", Colors.Black);
        ringMaterial.SetShaderParameter("emission", color);
        ringMaterial.SetShaderParameter("emission_energy", RingEnergy);

        //Der Ring liegt am Boden und gehört nicht zur Figur, er bekommt keinen Umriss
        ringMaterial.SetShaderParameter("outlined", false);

        return new MeshInstance3D
        {
            Name             = "Ring",
            Mesh             = new TorusMesh { InnerRadius = radiusMeters * 0.88f, OuterRadius = radiusMeters, Rings = 16, RingSegments = 4 },
            MaterialOverride = ringMaterial,
            CastShadow       = GeometryInstance3D.ShadowCastingSetting.Off,
            Position         = Vector3.Up * RingLiftMeters,
            Scale            = new Vector3(1, RingFlatness, 1)
        };
    }
}
