using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    protected          Player2D       ChasedPlayer;
    protected          Node           CurrentScene;
    private            ProgressBar    healthbar;
    private            ShaderMaterial hiddenInFogShaderMaterial;
    protected abstract PackedScene    AttackScene { get; }
    public             string         SpawnGroup  { get; set; }

    [Export]
    public int XpGranted { get; set; } = 100;

    [Export]
    public bool IsAggressive { get; set; }

    [Export]
    public float AggroRange { get; set; } = 500f;

    [Export]
    public float AttackRange { get; set; } = 150f;

    [Export]
    public float AttackWindeupTimeSec { get; set; } = 0.3f;

    [Export]
    public float AttackRecoveryTimeSec { get; set; } = 0.2f;

    [Export]
    public string LootTableId { get; set; }

    protected          AnimationTree AnimationTree  { get; set; }
    protected abstract Sprite2D      MovementSprite { get; }

    public override void _Ready()
    {
        base._Ready();

        CurrentScene       = GetTree().CurrentScene;
        AnimationTree      = GetNode<AnimationTree>(nameof(AnimationTree));
        healthbar          = GetNode<ProgressBar>("%Healthbar");
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;

        LoadSpriteNodes();

        PropertyChanged                += OnPropertyChanged;
        AnimationTree.AnimationStarted += AnimationTreeOnAnimationStarted;
    }

    private void AnimationTreeOnAnimationStarted(StringName animname)
    {
        switch (animname)
        {
            case Animation.DieLeft or Animation.DieRight or Animation.DieTop or Animation.DieDown:
                healthbar.Visible = false;
                SetAsOnlyVisibleSprite(DeathSprite);
                DieProperly();

                break;
            case Animation.RunLeft or Animation.RunRight or Animation.RunTop or Animation.RunDown:
                SetAsOnlyVisibleSprite(RunSprite);

                break;
            case Animation.AttackLeft or Animation.AttackRight or Animation.AttackTop or Animation.AttackDown:
                SetAsOnlyVisibleSprite(AttackSprite);

                break;
            case Animation.IdleLeft or Animation.IdleRight or Animation.IdleTop or Animation.IdleDown:
                SetAsOnlyVisibleSprite(IdleSprite);

                break;
        }
    }

    public void SetHighlight(bool active)
        => MovementSprite.SelfModulate = active ? new Color(3f, 1f, 2.0f) : new Color(1, 1, 1);

    private void SetAsOnlyVisibleSprite(Sprite2D sprite)
    {
        if (IdleSprite is not null)
            IdleSprite.Visible = IdleSprite == sprite;

        if (RunSprite is not null)
            RunSprite.Visible = RunSprite == sprite;

        if (AttackSprite is not null)
            AttackSprite.Visible = AttackSprite == sprite;

        if (DeathSprite is not null)
            DeathSprite.Visible = DeathSprite == sprite;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (MovementDirection != Vector2.Zero)
        {
            var direction = MovementDirection;

            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", direction);
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", direction);
            AnimationTree.Set("parameters/StateMachine/MoveState/DeathState/blend_position", direction);
        }
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LifeCurrent))
        {
            healthbar.Value = LifeCurrent;

            if (!healthbar.Visible && LifeCurrent < LifeMaximum)
            {
                healthbar.Visible = true;
                IsAggressive      = true;
            }
        }
        else
        {
            if (e.PropertyName == nameof(MovementDirection) && MovementDirection.Length() > 0.0f)
                SetAsOnlyVisibleSprite(RunSprite); //Hack, die Statemachine im Animationtree Startet die Animation nicht mehr
        }
    }

    protected override void DieProperly()
    {
        var controller = CurrentScene.GetNode<EnemyController>("%" + nameof(EnemyController));
        controller.SpawnedEnemies.Remove(this);

        PropertyChanged                -= OnPropertyChanged;
        AnimationTree.AnimationStarted -= AnimationTreeOnAnimationStarted;

        base.DieProperly();
    }

    protected abstract void ExecuteAttack();

    public void ChasePlayer()
    {
        if (!IsAggressive || IsDead || ChasedPlayer.IsDead)
        {
            MovementDirection = Vector2.Zero;

            return;
        }

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