using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class SkillArea3D : Node3D
{
    private readonly List<Unit3D> unitsInRange = new();

    private double       activeSec;
    private Cast3D       cast;
    private double       delayLeftSec;
    private bool         hasReachedRadius;
    private bool         hasStarted;
    private double       lingerLeftSec;
    private AreaSettings settings;
    private Vector3      visualBaseScale = Vector3.One;

    [Export]
    public Node3D Visual { get; set; }

    //Der Radius in Metern, den Visual bei der Skalierung aus der Szene zeigt
    [Export]
    public float VisualRadius { get; set; } = 1f;

    [Export]
    public Node3D Impact { get; set; }

    [Export]
    public float LingerSec { get; set; }

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(Cast3D skillCast, AreaSettings areaSettings)
    {
        cast         = skillCast;
        settings     = areaSettings;
        delayLeftSec = areaSettings.DelaySec;
    }

    public override void _Ready()
    {
        if (Visual is not null)
            visualBaseScale = Visual.Scale;

        if (Impact is not null)
            Impact.Visible = false;

        if (settings is not null)
            ShowRadius(settings.DelaySec > 0 ? settings.Radius : settings.GetRadiusAfter(0));
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

    private void HitUnitsWithin(float radiusPx)
    {
        var center = GlobalPosition;

        UnitRegistry3D.FindNear(center, radiusPx, unitsInRange);

        foreach (var unit in unitsInRange)
        {
            if (cast.CanHit(unit) && WorldScale.GroundDistancePx(center, unit.GlobalPosition) <= radiusPx)
                cast.ApplyTo(unit);
        }
    }

    //Die Fläche liegt auf dem Boden, in die Höhe wächst sie nicht
    private void ShowRadius(float radiusPx)
    {
        if (Visual is null || VisualRadius <= 0f)
            return;

        var factor = WorldScale.ToMeters(radiusPx) / VisualRadius;

        Visual.Scale = new Vector3(visualBaseScale.X * factor, visualBaseScale.Y, visualBaseScale.Z * factor);
    }

    private void ShowImpact()
    {
        lingerLeftSec = LingerSec;

        if (Impact is null)
            return;

        Impact.Visible = true;

        foreach (var child in Impact.GetChildren())
        {
            if (child is AudioStreamPlayer sound)
                PlayToTheEnd(sound);
        }
    }

    //Der Ton ist länger als die Fläche und risse mit ihr ab
    private void PlayToTheEnd(AudioStreamPlayer sound)
    {
        sound.Reparent(GetParent());

        sound.Finished += sound.QueueFree;

        sound.Play();
    }
}
