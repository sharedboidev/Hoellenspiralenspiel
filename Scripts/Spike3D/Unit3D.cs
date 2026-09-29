using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public abstract partial class Unit3D : CharacterBody3D
{
    public delegate void DamageTakenEventHandler(Unit3D victim, HitResult hit, Unit3D attacker);

    public delegate void DiedEventHandler(Unit3D unit);

    public delegate void LifeChangedEventHandler(Unit3D unit);

    private const float MinPickRadius = 0.4f;

    private readonly List<StatusTick> statusTicks = new();
    private          CollisionShape3D bodyShape;
    private          float            lifeCurrent;

    protected Unit3D()
    {
        StatusEffects = new StatusEffectTracker(Stats);

        Stats.Changed         += OnStatsChanged;
        StatusEffects.Started += OnStatusEffectStarted;
    }

    public StatSheet Stats { get; } = new();

    public StatusEffectTracker StatusEffects { get; }

    public SkillCooldowns SkillCooldowns { get; } = new();

    public abstract Faction Faction { get; }

    public virtual WeaponProfile Weapon => WeaponProfile.Unarmed;

    public virtual float AvailableMana => SkillGate.UnlimitedMana;

    public virtual bool IsTargetable => !IsDead;

    public virtual float CombatTextHeight => 1.6f;

    public bool IsDead => LifeCurrent <= 0;

    public Vector3 BodyCenter => bodyShape?.GlobalPosition ?? GlobalPosition;

    public float PickHeight { get; private set; } = 1f;

    public float PickRadius { get; private set; } = MinPickRadius;

    public float LifeMaximum => Stats.GetFinalWhole(CombatStat.Life);

    public float MovementspeedPx => Stats.GetFinal(CombatStat.Movementspeed);

    public float LifeCurrent
    {
        get => lifeCurrent;
        set
        {
            var clamped = Math.Clamp(value, 0, LifeMaximum);

            if (clamped.Equals(lifeCurrent))
                return;

            lifeCurrent = clamped;

            LifeChanged?.Invoke(this);
        }
    }

    protected Node3D Visual { get; private set; }

    public event DamageTakenEventHandler DamageTaken;
    public event DiedEventHandler        Died;
    public event LifeChangedEventHandler LifeChanged;

    public override void _EnterTree()
        => UnitRegistry3D.Register(this);

    public override void _ExitTree()
        => UnitRegistry3D.Unregister(this);

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;

        bodyShape = GetNodeOrNull<CollisionShape3D>(nameof(CollisionShape3D));
        Visual    = GetNodeOrNull<Node3D>(nameof(Visual));

        MeasurePickVolume();

        Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            sheet.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
            sheet.SetBase(CombatStat.BlockReduction, CombatRules.BaseBlockReduction);
            sheet.SetBase(CombatStat.ProjectileCount, 1);

            ApplyBaseValues(sheet);
        });

        LifeCurrent = LifeMaximum;
    }

    public override void _PhysicsProcess(double delta)
    {
        RegenerateLife(delta);
        AdvanceStatusEffects(delta);

        if (SkillCooldowns.HasAny)
            SkillCooldowns.Advance(delta);

        UnitRegistry3D.Track(this);
    }

    protected abstract void ApplyBaseValues(StatSheet sheet);

    //Angeklickt wird das sichtbare Modell. Die Kollisionsform ist wie in 2D viel kleiner
    private void MeasurePickVolume()
    {
        var body = Visual?.GetNodeOrNull<MeshInstance3D>("Body");

        if (body?.Mesh is null)
            return;

        var bounds = body.Transform * body.GetAabb();

        PickHeight = Math.Max(MinPickRadius, bounds.End.Y);
        PickRadius = Math.Max(MinPickRadius, Math.Max(bounds.Size.X, bounds.Size.Z) / 2f);
    }

    //Liefert den Abstand entlang des Strahls, negativ bei einem Fehlgriff
    public float GetPickDepth(Vector3 rayOrigin, Vector3 rayNormal)
    {
        var from     = GlobalPosition;
        var axis     = Vector3.Up * PickHeight;
        var toOrigin = rayOrigin - from;
        var along    = rayNormal.Dot(axis);
        var length   = axis.Dot(axis);
        var ahead    = rayNormal.Dot(toOrigin);
        var up       = axis.Dot(toOrigin);
        var spread   = length - along * along;
        var onAxis   = spread < 0.0001f ? 0f : Math.Clamp((up - along * ahead) / spread, 0f, 1f);
        var depth    = along * onAxis - ahead;

        if (depth < 0f)
            return -1f;

        var gap = (rayOrigin + rayNormal * depth).DistanceTo(from + axis * onAxis);

        return gap <= PickRadius ? depth : -1f;
    }

    public virtual void SpendMana(float amount) { }

    public SkillUseCheck TryPayFor(SkillResource skill, double minCooldownSec = 0)
    {
        var definition = skill.Definition;
        var check      = SkillGate.Check(definition, SkillCooldowns, AvailableMana);

        if (check != SkillUseCheck.Ready)
            return check;

        SpendMana(definition.ManaCost);
        SkillCooldowns.Start(definition.Id, Math.Max(definition.CooldownSec, minCooldownSec));

        return check;
    }

    public bool IsHostileTo(Unit3D other)
        => other is not null && other.Faction != Faction;

    public float DistancePxTo(Unit3D other)
        => WorldScale.GroundDistancePx(GlobalPosition, other.GlobalPosition);

    public virtual void ReceiveDamage(HitResult hit, Unit3D attacker = null)
    {
        if (!IsTargetable)
            return;

        if (hit.HasLanded)
        {
            LifeCurrent -= hit.FinalDamage;

            if (hit.InflictedEffect is not null && !IsDead)
                StatusEffects.Apply(hit.InflictedEffect);
        }

        CombatText3D.ShowHit(this, hit);

        DamageTaken?.Invoke(this, hit, attacker);
    }

    public bool RollActionFailure()
    {
        var chance = StatusEffects.ActionFailureChance;

        return chance > 0 && GameRandom.Shared.NextFloat() < chance;
    }

    public virtual void SetHighlight(bool active) { }

    protected void MoveOnGround(Vector3 direction, float speedPx)
    {
        Velocity = WorldScale.OnGround(direction) * WorldScale.ToMeters(speedPx);

        MoveAndSlide();
    }

    //Ein Modell deckt alle Richtungen ab, gedreht wird nur die Darstellung
    protected void Face(Vector3 direction)
    {
        var onGround = WorldScale.OnGround(direction);

        if (Visual is null || onGround.LengthSquared() < 0.0001f)
            return;

        Visual.Rotation = new Vector3(0, Mathf.Atan2(-onGround.X, -onGround.Z), 0);
    }

    protected void RaiseDied()
        => Died?.Invoke(this);

    protected virtual void OnStatsRecalculated() { }

    private void OnStatsChanged()
    {
        if (LifeCurrent > LifeMaximum)
            LifeCurrent = LifeMaximum;

        OnStatsRecalculated();

        LifeChanged?.Invoke(this);
    }

    protected virtual void RegenerateLife(double delta)
    {
        if (IsDead)
            return;

        var regeneration = Stats.GetFinalWhole(CombatStat.Liferegeneration);

        if (regeneration > 0 && LifeCurrent < LifeMaximum)
            LifeCurrent += regeneration * (float)delta;
    }

    private void AdvanceStatusEffects(double delta)
    {
        if (!StatusEffects.HasAny)
            return;

        if (IsDead)
        {
            StatusEffects.Clear();

            return;
        }

        statusTicks.Clear();
        StatusEffects.Advance(delta, statusTicks);

        foreach (var tick in statusTicks)
        {
            if (IsDead)
                break;

            LifeCurrent -= tick.Damage;

            CombatText3D.ShowStatusTick(this, tick);
        }
    }

    private void OnStatusEffectStarted(StatusEffectKind kind)
    {
        if (IsInsideTree())
            CombatText3D.ShowStatusStarted(this, kind);
    }
}
