using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    private Player chasedPlayer;
    private bool   isAggressive;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        ChasePlayer(); //In EnemyController o.Ä. auslagern
    }

    public void ChasePlayer()
    {
        if (!isAggressive || chasedPlayer.IsDead)
            return;

        var direction = (chasedPlayer.Position - Position).Normalized();
        Velocity = Movementspeed * direction;

        MoveAndSlide();

        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision      = GetSlideCollision(i);
            var collidedObject = (Node)collision.GetCollider();

            if (collidedObject.Name == nameof(Player))
            {
                // if (!chasedPlayer.IsInvicible)
                //     DealDamageToPlayer();
            }
        }
    }
}