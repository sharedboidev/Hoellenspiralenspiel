using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Eine Fläche, die erst trifft, wenn ihre Kugel gelandet ist. Die Kugel fliegt so lange wie die Verzögerung der Fläche im Bogen von ihrem Start zum Einschlag.
//Sie zieht im Takt des Bildes, der Einschlag kommt im Takt der Physik. Bis dahin liegt sie höchstens ein Frame auf dem Boden
public partial class LobbedArea : SkillArea
{
    private double  flownSec;
    private Vector3 startOffset;

    [Export]
    public Node3D Ball { get; set; }

    //Über dem Boden. 2,7 m sind das Anderthalbfache der Körperhöhe des Helden
    [Export]
    public float PeakHeightMeters { get; set; } = 2.7f;

    //Nach Launch und vor dem Einhängen in den Szenenbaum aufrufen. Der Start liegt relativ zum Einschlag
    public void LaunchFrom(Vector3 offsetToStart)
        => startOffset = offsetToStart;

    public override void _Ready()
    {
        base._Ready();

        PlaceBall();
    }

    public override void _Process(double delta)
    {
        flownSec += delta;

        PlaceBall();
    }

    private void PlaceBall()
    {
        if (Ball is null)
            return;

        Ball.Visible = !HasImpacted;

        if (HasImpacted)
            return;

        var progress = DelaySec > 0 ? (float)Math.Min(1.0, flownSec / DelaySec) : 1f;
        var left     = 1f - progress;

        Ball.Position = new Vector3(startOffset.X * left, LobArc.HeightAt(progress, startOffset.Y, PeakHeightMeters), startOffset.Z * left);
    }
}
