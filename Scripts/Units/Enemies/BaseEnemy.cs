using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.UI;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    protected          Player2D       ChasedPlayer;
    protected          Node           CurrentScene;
    private            FogOfWar       fogOfWar;
    private            ProgressBar    healthbar;
    private            ShaderMaterial hiddenInFogShaderMaterial;
    protected abstract PackedScene    AttackScene { get; }
    public             string         SpawnGroup  { get; set; }

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

    protected AnimationTree AnimationTree { get; set; }

    public override void _Ready()
    {
        base._Ready();

        CurrentScene       = GetTree().CurrentScene;
        AnimationTree      = GetNode<AnimationTree>(nameof(AnimationTree));
        healthbar          = GetNode<ProgressBar>("%Healthbar");
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;

        //ConfigureFogOfWarVisibilityShader();

        PropertyChanged += OnPropertyChanged;
    }

    private void ConfigureFogOfWarVisibilityShader()
    {
        fogOfWar = CurrentScene.GetNode<FogOfWar>("%" + nameof(FogOfWar));

        hiddenInFogShaderMaterial        = new ShaderMaterial();
        hiddenInFogShaderMaterial.Shader = GD.Load<Shader>("res://Shaders/hidden_in_fog.gdshader");

        var mySprite = GetNode<Sprite2D>(nameof(Sprite2D));
        mySprite.Material = hiddenInFogShaderMaterial;

        UpdateShaderParams();
    }

    private void UpdateShaderParams()
    {
        if (hiddenInFogShaderMaterial != null && fogOfWar != null)
        {
            hiddenInFogShaderMaterial.SetShaderParameter("fog_texture", fogOfWar.GetFogTexture());
            hiddenInFogShaderMaterial.SetShaderParameter("fog_offset", fogOfWar.GetFogOffset());
            hiddenInFogShaderMaterial.SetShaderParameter("fog_scale", fogOfWar.GetFogScale());
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (MovementDirection != Vector2.Zero)
        {
            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
            //AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        //UpdateShaderParams();
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