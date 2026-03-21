using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player : CharacterBody3D
{
    private bool   invincibilityIsRunning;
    private double millisecondsSinceLastHit;

    [Export]
    public int InvicibilityTimeMilliseconds { get; set; } = 1000;

    public bool IsInvicible => CheckInvincibility();

    public override void _Process(double delta)
    {
        base._Process(delta);
        HandleMovementInputs();
        ResolveInvincibility(delta);
    }

    [Export]
    public Vector3 MovementDirection { get; set; }

    public float Movementspeed { get; set; } = 100;

    private void HandleMovementInputs()
    {
        var kek = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        MovementDirection = new Vector3(kek.X, 0, kek.Y);

        Velocity          = MovementDirection * Movementspeed;

        MoveAndSlide();
    }
    private void ResolveInvincibility(double delta)
    {
        if (millisecondsSinceLastHit > InvicibilityTimeMilliseconds)
            invincibilityIsRunning = false;

        if (invincibilityIsRunning)
            millisecondsSinceLastHit += delta * 1000;
    }

    private bool CheckInvincibility()
    {
        if (InvicibilityTimeMilliseconds > millisecondsSinceLastHit)
            return true;

        invincibilityIsRunning   = false;
        millisecondsSinceLastHit = 0;

        return false;
    }
}