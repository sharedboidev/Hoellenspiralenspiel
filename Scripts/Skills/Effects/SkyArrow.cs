using System;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Der Pfeil, den der Held in den Himmel schießt. Er ist nur zu sehen: Er fliegt gerade seine Richtung entlang und verschwindet nach seiner Lebenszeit.
//Der Pfeil liegt in seiner Szene entlang -Z, die Spitze vorn
public partial class SkyArrow : Node3D
{
    private Vector3 direction = Vector3.Up;
    private double  leftSec;

    [Export]
    public float SpeedMetersPerSec { get; set; } = 30f;

    [Export]
    public float LifetimeSec { get; set; } = 0.45f;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(Vector3 flightDirection)
    {
        if (flightDirection.LengthSquared() > 0f)
            direction = flightDirection.Normalized();

        leftSec = LifetimeSec;
    }

    public override void _Ready()
    {
        var up = Math.Abs(direction.Y) > 0.99f ? Vector3.Forward : Vector3.Up;

        Basis = Basis.LookingAt(direction, up);
    }

    public override void _Process(double delta)
    {
        GlobalPosition += direction * (float)(SpeedMetersPerSec * delta);

        leftSec -= delta;

        if (leftSec <= 0)
            QueueFree();
    }
}
