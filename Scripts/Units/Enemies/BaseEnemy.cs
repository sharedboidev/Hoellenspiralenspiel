using System.ComponentModel;
using Godot;
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

    [Export]
    public string LootTableId { get; set; }

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

    public void ChasePlayer()
    {
        if (!IsAggressive || ChasedPlayer.IsDead)
            return;

        var distance  = ChasedPlayer.Position.DistanceTo(Position);
        var isInRange = distance < AttackRange;

        if (isInRange)
            ExecuteAttack();
        else
            RunAtPlayer();
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