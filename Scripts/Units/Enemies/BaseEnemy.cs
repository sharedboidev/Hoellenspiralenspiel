using System;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    protected Player2D ChasedPlayer;
    protected Node     CurrentScene;
    public    bool     IsAggressive { get; set; }

    public override void _Ready()
    {
        base._Ready();

        CurrentScene = GetTree().CurrentScene;
        IsAggressive = true;
    }

    protected override void DieProperly()
    {
        var controller = CurrentScene.GetNode<EnemyController>(nameof(EnemyController));
        controller.SpawnedEnemies.Remove(this);

        base.DieProperly();
    }

    public void ChasePlayer()
    {
        if (!IsAggressive || ChasedPlayer.IsDead)
            return;

        var direction = (ChasedPlayer.Position - Position).Normalized();
        MovementDirection = direction;
        Velocity          = Movementspeed * direction;

        MoveAndSlide();

        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision      = GetSlideCollision(i);
            var collidedObject = (Node)collision.GetCollider();

            if (collidedObject is Player2D)
            {
                var fakeHit = new HitResult(9001, HitType.Normal, LifeModificationMode.Damage);

                ChasedPlayer.InstatiateFloatingCombatText(fakeHit, CurrentScene, offset: new Vector2(0, -188));
            }
        }
    }
}