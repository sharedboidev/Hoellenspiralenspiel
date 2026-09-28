using System;
using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public abstract partial class BaseEnemy : BaseUnit
{
    private const float MinAttackspeedRate = 0.1f;

    //Färbt den Gegner, solange er ausholt, damit der Angriff zu sehen ist
    private static readonly Color WindupTint = new(1.6f, 0.7f, 0.7f);

    //Gilt für Gegner, denen kein Skill zugewiesen ist
    private static readonly AttackSkillResource StandardAttack = new() { Id = "attack", DisplayName = AttackDefinition.Standard.Name };

    private readonly AttackCycle     attackCycle = new();
    private          AnimationPlayer animationPlayer;
    private          bool            attackFailed;
    protected        Player2D        ChasedPlayer;
    protected        Node            CurrentScene;
    private          ProgressBar     healthbar;
    private          ShaderMaterial  hiddenInFogShaderMaterial;
    private          bool            isAttackAnimationRunning;
    public           string          SpawnGroup { get; set; }
    public           bool            IsDying    { get; private set; }

    public override Faction Faction      => Faction.Monster;
    public override bool    IsTargetable => !IsDead && !IsDying;

    //Die Angriffswerte aus dem Inspector sind die Waffe des Gegners. Eine ATTACK skaliert damit
    public override WeaponProfile Weapon => new(AttackDamageMin, AttackDamageMax, 1f, AttackCriticalHitChance, AttackDamageType, AttackRange);

    //Der Skill, mit dem der Gegner angreift. Ohne Angabe ist es der Standardangriff im Nahkampf
    [Export]
    public SkillResource AttackSkill { get; set; }

    [Export]
    public int XpGranted { get; set; } = 100;

    [Export]
    public bool IsAggressive { get; set; }

    [Export]
    public float AggroRange { get; set; } = 500f;

    //Abstand in Pixeln zwischen den Körpermitten, ab dem der Gegner angreift
    [Export]
    public float AttackRange { get; set; } = 150f;

    [Export]
    public float AttackWindeupTimeSec { get; set; } = 0.3f;

    [Export]
    public float AttackRecoveryTimeSec { get; set; } = 0.2f;

    [Export]
    public int AttackDamageMin { get; set; } = 1;

    [Export]
    public int AttackDamageMax { get; set; } = 3;

    [Export]
    public DamageType AttackDamageType { get; set; } = DamageType.Crush;

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float AttackCriticalHitChance { get; set; } = 5f;

    [Export]
    public string LootTableId { get; set; }

    protected          AnimationTree AnimationTree  { get; set; }
    protected abstract Sprite2D      MovementSprite { get; }

    public override void _Ready()
    {
        base._Ready();

        CurrentScene       = GetTree().CurrentScene;
        AnimationTree      = GetNode<AnimationTree>(nameof(AnimationTree));
        animationPlayer    = GetNodeOrNull<AnimationPlayer>(nameof(AnimationPlayer));
        healthbar          = GetNode<ProgressBar>("%Healthbar");
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;

        LoadSpriteNodes();

        PropertyChanged                += OnPropertyChanged;
        StatsChanged                   += OnStatsChanged;
        AnimationTree.AnimationStarted += AnimationTreeOnAnimationStarted;
    }

    //Ändert sich das maximale Leben, muss der Balken das neue Maximum kennen
    private void OnStatsChanged()
    {
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;
    }

    private void AnimationTreeOnAnimationStarted(StringName animname)
    {
        switch (animname)
        {
            case Animation.DieLeft or Animation.DieRight or Animation.DieTop or Animation.DieDown:
                SetAsOnlyVisibleSprite(DeathSprite);

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

    public override void SetHighlight(bool active)
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

        if (IsDying)
            return;

        AdvanceAttack(delta);

        if (MovementDirection != Vector2.Zero)
        {
            var direction = MovementDirection;

            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", direction);
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", direction);
            AnimationTree.Set("parameters/StateMachine/MoveState/DeathState/blend_position", direction);
        }
    }

    protected override void ResolveLifeReg(double delta)
    {
        //Ein sterbender Gegner regeneriert nicht zurück ins Leben
        if (!IsDying)
            base.ResolveLifeReg(delta);
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

            if (IsDead)
                BeginDeath();
        }
        else
        {
            if (e.PropertyName == nameof(MovementDirection) && MovementDirection.Length() > 0.0f && !isAttackAnimationRunning)
                SetAsOnlyVisibleSprite(RunSprite); //Hack, die Statemachine im Animationtree Startet die Animation nicht mehr
        }
    }

    public override void ReceiveDamage(HitResult hit)
    {
        //Auch ein abgewehrter Treffer macht den Gegner aggressiv
        if (IsTargetable)
            IsAggressive = true;

        base.ReceiveDamage(hit);
    }

    //Der Tod wird genau einmal ausgelöst: XP und Loot sofort, entfernt wird der Gegner erst nach der Todesanimation
    private void BeginDeath()
    {
        if (IsDying)
            return;

        IsDying           = true;
        healthbar.Visible = false;
        Velocity          = Vector2.Zero;

        attackCycle.Reset();
        EndAttackLook();
        StatusEffects.Clear();

        GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D))?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        var controller = CurrentScene.GetNode<EnemyController>("%" + nameof(EnemyController));
        controller.SpawnedEnemies.Remove(this);

        PropertyChanged -= OnPropertyChanged;

        RaiseDied();

        GetTree().CreateTimer(GetDeathAnimationLengthSec()).Timeout += RemoveCorpse;
    }

    private double GetDeathAnimationLengthSec()
    {
        if (animationPlayer is null || !animationPlayer.HasAnimation(Animation.DieDown))
            return 0;

        return animationPlayer.GetAnimation(Animation.DieDown).Length;
    }

    private void RemoveCorpse()
    {
        if (IsInstanceValid(this) && !IsQueuedForDeletion())
            QueueFree();
    }

    //Gegner setzen Skills auf demselben Weg ein wie der Spieler. Mana und Abklingzeit zählen für sie noch nicht
    protected virtual void ExecuteAttack()
        => SkillExecutor.Execute(this, AttackSkill ?? StandardAttack, new SkillAim(ChasedPlayer.BodyCenter, ChasedPlayer));

    public void ChasePlayer()
    {
        if (!IsAggressive || IsDead || ChasedPlayer.IsDead)
        {
            MovementDirection = Vector2.Zero;

            return;
        }

        //Während Windup und Recovery bleibt der Gegner stehen
        if (!attackCycle.IsReady)
            return;

        if (DistanceTo(ChasedPlayer) < AttackRange)
            StartAttack();
        else
            RunAtPlayer();
    }

    private void StartAttack()
    {
        var toPlayer = ChasedPlayer.BodyCenter - BodyCenter;

        //Chill und andere Modifier auf das Angriffstempo strecken oder stauchen den Takt
        var attackspeedRate = Math.Max(MinAttackspeedRate, Stats.GetTotalMultiplier(CombatStat.Attackspeed));

        Velocity          = Vector2.Zero;
        MovementDirection = Vector2.Zero;
        attackFailed      = RollActionFailure();

        attackCycle.Start(AttackWindeupTimeSec / attackspeedRate, AttackRecoveryTimeSec / attackspeedRate);

        BeginAttackLook(toPlayer, (AttackWindeupTimeSec + AttackRecoveryTimeSec) / attackspeedRate);
    }

    private void AdvanceAttack(double delta)
    {
        if (attackCycle.IsReady)
            return;

        if (attackCycle.Advance(delta))
        {
            Modulate = Colors.White;

            if (attackFailed)
                this.ShowCombatText("Failed", Colors.Yellow, 28);
            else if (!ChasedPlayer.IsDead)
                ExecuteAttack();
        }

        if (attackCycle.IsReady)
            EndAttackLook();
    }

    private void BeginAttackLook(Vector2 direction, double attackSec)
    {
        Modulate = WindupTint;

        var animationName = GetAttackAnimationName(direction);

        if (AttackSprite is null || animationPlayer is null || !animationPlayer.HasAnimation(animationName) || attackSec <= 0)
            return;

        isAttackAnimationRunning = true;
        AnimationTree.Active     = false;

        SetAsOnlyVisibleSprite(AttackSprite);

        animationPlayer.Play(animationName, customSpeed: (float)(animationPlayer.GetAnimation(animationName).Length / attackSec));
    }

    private void EndAttackLook()
    {
        Modulate = Colors.White;

        if (!isAttackAnimationRunning)
            return;

        isAttackAnimationRunning = false;

        animationPlayer.Stop();

        SetAsOnlyVisibleSprite(IdleSprite ?? RunSprite);

        AnimationTree.Active = true;
    }

    private static string GetAttackAnimationName(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            return direction.X > 0 ? Animation.AttackRight : Animation.AttackLeft;

        return direction.Y > 0 ? Animation.AttackDown : Animation.AttackTop;
    }

    private void RunAtPlayer()
    {
        var rawDirection = ChasedPlayer.GlobalPosition - GlobalPosition;
        var direction    = rawDirection.Normalized();

        MovementDirection = direction;
        Velocity          = MovementspeedFinal * direction;

        MoveAndSlide();
    }
}
