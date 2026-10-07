using System;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Eine Fläche, auf die ein Pfeil aus dem Himmel fällt. Er erscheint erst kurz vor dem Einschlag hoch über ihr, kommt schräg aus der Schussrichtung
//und steckt danach im Boden, bis die Fläche verschwindet. Er zieht im Takt des Bildes, der Einschlag kommt im Takt der Physik
public partial class FallingArea : SkillArea
{
    private double  flownSec;
    private Vector3 startOffset;

    [Export]
    public Node3D Arrow { get; set; }

    //Der Pfeil liegt in seiner Szene entlang -Z, die Spitze vorn. So lang ist er, damit er nach dem Einschlag richtig tief steckt
    [Export]
    public float ArrowLengthMeters { get; set; } = 0.8f;

    //Aus dieser Höhe fällt er, und so lange ist er dabei zu sehen
    [Export]
    public float FallHeightMeters { get; set; } = 9f;

    [Export]
    public float FallSec { get; set; } = 0.35f;

    //So weit zieht er beim Fallen in Schussrichtung weiter
    [Export]
    public float DriftMeters { get; set; } = 2f;

    [Export]
    public float StuckDepthMeters { get; set; } = 0.25f;

    //Nach Launch und vor dem Einhängen in den Szenenbaum aufrufen: die Richtung auf dem Boden, in die der Schuss ging
    public void FallAlong(Vector3 groundDirection)
    {
        var drift = groundDirection.LengthSquared() > 0f ? groundDirection.Normalized() * DriftMeters : Vector3.Zero;

        startOffset = Vector3.Up * FallHeightMeters - drift;
    }

    public override void _Ready()
    {
        base._Ready();

        if (Arrow is null)
            return;

        //Ohne Schussrichtung fällt er senkrecht
        if (startOffset == Vector3.Zero)
            startOffset = Vector3.Up * FallHeightMeters;

        var fall = -startOffset.Normalized();
        var up   = Math.Abs(fall.Y) > 0.99f ? Vector3.Forward : Vector3.Up;

        Arrow.Basis = Basis.LookingAt(fall, up);

        PlaceArrow();
    }

    public override void _Process(double delta)
    {
        flownSec += delta;

        PlaceArrow();
    }

    private void PlaceArrow()
    {
        if (Arrow is null)
            return;

        if (HasImpacted)
        {
            Arrow.Visible  = true;
            Arrow.Position = startOffset.Normalized() * (ArrowLengthMeters / 2f - StuckDepthMeters);

            return;
        }

        var leftSec = DelaySec - flownSec;

        Arrow.Visible = leftSec <= FallSec;

        if (!Arrow.Visible)
            return;

        var left = FallSec > 0f ? (float)Math.Clamp(leftSec / FallSec, 0.0, 1.0) : 0f;

        Arrow.Position = startOffset * left;
    }
}
