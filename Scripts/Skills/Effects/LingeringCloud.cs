using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein Nebel am Boden, der liegen bleibt und wächst. Jeder Gegner, der ihn berührt, bekommt sofort den Effekt des Skills
//und danach je Puls erneut, solange er darin steht. Nach seiner Dauer löst er sich auf und gibt keinen Effekt mehr
public partial class LingeringCloud : Node3D
{
    private static readonly StringName DensityParameter   = "density";
    private static readonly StringName DissipateParameter = "dissipate";

    private readonly Dictionary<ulong, double> lastPulseSec = new();
    private readonly List<BaseUnit>            unitsInRange = new();

    private double        activeSec;
    private BaseUnit      caster;
    private Faction       faction;
    private bool          isFading;
    private float         lightEnergy;
    private CloudSettings settings;
    private Vector3       visualBaseScale = Vector3.One;

    [Export]
    public Node3D Visual { get; set; }

    //Der Radius in Metern, den Visual bei der Skalierung aus der Szene zeigt
    [Export]
    public float VisualRadius { get; set; } = 1f;

    [Export]
    public OmniLight3D Light { get; set; }

    //So lange zieht der Nebel am Anfang auf, gewirkt hat er da schon
    [Export]
    public float FadeInSec { get; set; } = 0.3f;

    //So lange löst er sich am Ende auf. Er steigt dabei auf und verschwindet von unten nach oben, siehe ps1_mist
    [Export]
    public float FadeOutSec { get; set; } = 1.5f;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(BaseUnit cloudCaster, CloudSettings cloudSettings)
    {
        caster   = cloudCaster;
        faction  = cloudCaster.Faction;
        settings = cloudSettings;
    }

    public override void _Ready()
    {
        if (Visual is not null)
            visualBaseScale = Visual.Scale;

        if (Light is not null)
            lightEnergy = Light.LightEnergy;

        if (settings is not null)
            ShowRadius(settings.GetRadiusAfter(0));

        ShowDensity(0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (settings is null || isFading)
            return;

        activeSec += delta;

        if (settings.IsOver(activeSec))
        {
            FadeOut();

            return;
        }

        var radius = settings.GetRadiusAfter(activeSec);

        ShowRadius(radius);
        ShowDensity(FadeInSec > 0f ? Mathf.Min(1f, (float)(activeSec / FadeInSec)) : 1f);
        PulseUnitsWithin(radius);
    }

    private void PulseUnitsWithin(float radiusPx)
    {
        var center = GlobalPosition;
        var source = IsInstanceValid(caster) ? caster : null;

        UnitRegistry.FindNear(center, radiusPx, unitsInRange);

        foreach (var unit in unitsInRange)
        {
            if (!IsInstanceValid(unit) || !unit.IsTargetable || unit.Faction == faction)
                continue;

            var offset = unit.GlobalPosition - center;

            if (!AreaSettings.Contains(WorldScale.ToPx(offset.X), WorldScale.ToPx(offset.Z), radiusPx + unit.BodyRadiusPx))
                continue;

            var id = unit.GetInstanceId();

            if (lastPulseSec.TryGetValue(id, out var last) && !settings.IsDue(activeSec, last))
                continue;

            lastPulseSec[id] = activeSec;

            unit.TryApplyStatusEffect(StatusEffectRules.CreateApplication(settings.Effect, source));
        }
    }

    private void FadeOut()
    {
        isFading = true;

        var fade = CreateTween();

        fade.TweenMethod(Callable.From<float>(ShowDissipation), 0f, 1f, FadeOutSec);
        fade.TweenCallback(Callable.From(QueueFree));
    }

    //Die Fläche liegt auf dem Boden, in die Höhe wächst sie nicht
    private void ShowRadius(float radiusPx)
    {
        if (Visual is null || VisualRadius <= 0f)
            return;

        var factor = WorldScale.ToMeters(radiusPx) / VisualRadius;

        Visual.Scale = new Vector3(visualBaseScale.X * factor, visualBaseScale.Y, visualBaseScale.Z * factor);
    }

    private void ShowDensity(float density)
    {
        SetOnLayers(DensityParameter, density);

        if (Light is not null)
            Light.LightEnergy = lightEnergy * density;
    }

    private void ShowDissipation(float dissipation)
    {
        SetOnLayers(DissipateParameter, dissipation);

        if (Light is not null)
            Light.LightEnergy = lightEnergy * (1f - dissipation);
    }

    private void SetOnLayers(StringName parameter, float value)
    {
        if (Visual is null)
            return;

        foreach (var mesh in Visual.FindChildren("*", nameof(GeometryInstance3D), true, false).OfType<GeometryInstance3D>())
            mesh.SetInstanceShaderParameter(parameter, value);
    }
}
