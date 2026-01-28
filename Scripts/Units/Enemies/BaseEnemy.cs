using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;
using Hoellenspiralenspiel.Scripts.Controllers;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    protected          Player2D    ChasedPlayer;
    protected          Node        CurrentScene;
    private            ProgressBar healthbar;
    protected abstract PackedScene AttackScene { get; }

    [Export]
    public bool IsAggressive { get; set; }

    [Export]
    public float AttackRange { get; set; } = 150f;

    [Export]
    public float AttackWindeupTimeSec { get; set; } = 0.3f;

    [Export]
    public float AttackRecoveryTimeSec { get; set; } = 0.2f;

    public override void _Ready()
    {
        base._Ready();

        CurrentScene       = GetTree().CurrentScene;
        healthbar          = GetNode<ProgressBar>("%Healthbar");
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;

        PropertyChanged += OnPropertyChanged;

        IsAggressive = true;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LifeCurrent))
        {
            healthbar.Value = LifeCurrent;

            if (!healthbar.Visible && LifeCurrent < LifeMaximum)
                healthbar.Visible = true;
        }
    }

    protected override void DieProperly()
    {
        var controller = CurrentScene.GetNode<EnemyController>("%" + nameof(EnemyController));
        controller.SpawnedEnemies.Remove(this);

        base.DieProperly();
    }

    protected abstract void ExecuteAttack();

    public void ChasePlayer(double delta)
    {
        if (!IsAggressive || ChasedPlayer.IsDead)
            return;

        var distance  = ChasedPlayer.Position.DistanceTo(Position);
        var isInRange = distance < AttackRange;

        if (isInRange)
            ExecuteAttack();
        else
            RunAtPlayer();

        // for (var i = 0; i < GetSlideCollisionCount(); i++)
        // {
        //     var collision      = GetSlideCollision(i);
        //     var collidedObject = (Node)collision.GetCollider();
        //
        //     if (collidedObject is Player2D)
        //     {
        //         var fakeHit = new HitResult(9001, HitType.Normal, LifeModificationMode.Damage);
        //
        //         ChasedPlayer.InstatiateFloatingCombatText(fakeHit, CurrentScene, offset: new Vector2(0, -188));
        //     }
        // }
    }

    private void RunAtPlayer()
    {
        var rawDirection = ChasedPlayer.Position - Position;
        var direction    = rawDirection.Normalized();

        MovementDirection = direction;
        Velocity          = Movementspeed * direction;

        MoveAndSlide();
    }
}