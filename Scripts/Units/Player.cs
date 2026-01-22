using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player : BaseUnit
{
    private bool   invincibilityIsRunning;
    private double millisecondsSinceLastHit;

    [Export]
    public int InvicibilityTimeMilliseconds { get; set; } = 1000;

    public bool IsInvicible => CheckInvincibility();

    public override void _Process(double delta)
    {
        base._Process(delta);

        ResolveInvincibility(delta);
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