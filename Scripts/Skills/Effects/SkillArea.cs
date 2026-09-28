using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

public partial class SkillArea : Node2D
{
    private double       activeSec;
    private SkillCast    cast;
    private double       delayLeftSec;
    private bool         hasReachedRadius;
    private bool         hasStarted;
    private double       lingerLeftSec;
    private AreaSettings settings;
    private Vector2      visualBaseScale = Vector2.One;

    [Export]
    public Node2D Visual { get; set; }

    //Der Radius, den Visual bei der Skalierung aus der Szene zeigt
    [Export]
    public float VisualRadius { get; set; } = 100f;

    [Export]
    public AnimatedSprite2D Indicator { get; set; }

    [Export]
    public PackedScene ImpactScene { get; set; }

    [Export]
    public Vector2 ImpactScale { get; set; } = Vector2.One;

    [Export]
    public float LingerSec { get; set; }

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(SkillCast skillCast, AreaSettings areaSettings)
    {
        cast         = skillCast;
        settings     = areaSettings;
        delayLeftSec = areaSettings.DelaySec;
    }

    public override void _Ready()
    {
        if (Visual is not null)
            visualBaseScale = Visual.Scale;

        if (settings is null)
            return;

        ShowRadius(settings.DelaySec > 0 ? settings.Radius : settings.GetRadiusAfter(0));
        PlayIndicator();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (cast is null)
            return;

        if (delayLeftSec > 0)
        {
            delayLeftSec -= delta;

            if (delayLeftSec > 0)
                return;

            delta = -delayLeftSec;
        }

        if (!hasStarted)
        {
            hasStarted = true;

            ShowImpact();
        }
        else
            activeSec += delta;

        if (!hasReachedRadius)
        {
            var radius = settings.GetRadiusAfter(activeSec);

            ShowRadius(radius);
            HitUnitsWithin(radius);

            hasReachedRadius = activeSec >= settings.ExpansionSec;

            return;
        }

        lingerLeftSec -= delta;

        if (lingerLeftSec <= 0)
            QueueFree();
    }

    private void HitUnitsWithin(float radius)
    {
        var center = GlobalPosition;
        var units  = UnitRegistry.Units;

        for (var i = units.Count - 1; i >= 0; i--)
        {
            var unit = units[i];

            if (!cast.CanHit(unit))
                continue;

            var offset = unit.BodyCenter - center;

            if (AreaSettings.Contains(offset.X, offset.Y, radius))
                cast.ApplyTo(unit);
        }
    }

    private void ShowRadius(float radius)
    {
        if (Visual is null || VisualRadius <= 0f)
            return;

        Visual.Scale = visualBaseScale * (radius / VisualRadius);
    }

    private void PlayIndicator()
    {
        if (Indicator?.SpriteFrames is null)
            return;

        var animation = Indicator.Animation;
        var frames    = Indicator.SpriteFrames;
        var speed     = frames.GetAnimationSpeed(animation) * Indicator.SpeedScale;

        if (speed <= 0 || settings.DelaySec <= 0f)
        {
            Indicator.Play();

            return;
        }

        var frameUnits = 0f;

        for (var frame = 0; frame < frames.GetFrameCount(animation); frame++)
            frameUnits += frames.GetFrameDuration(animation, frame);

        Indicator.Play(customSpeed: (float)(frameUnits / speed / settings.DelaySec));
    }

    private void ShowImpact()
    {
        lingerLeftSec = LingerSec;

        if (ImpactScene is null)
            return;

        var impact = ImpactScene.Instantiate<Node2D>();

        impact.Scale = ImpactScale;

        AddChild(impact);

        foreach (var child in impact.GetChildren())
        {
            if (child is AudioStreamPlayer2D sound)
                sound.Play();
        }
    }
}
