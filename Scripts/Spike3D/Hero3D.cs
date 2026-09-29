using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class Hero3D : Unit3D
{
    public delegate void ManaChangedEventHandler();

    private const float  ImpactFraction     = 0.5f;
    private const float  EngageFraction     = 0.9f;
    private const int    NoSlot             = -1;
    private const double StuckTimeoutSec    = 0.4;
    private const float  StuckSpeedFraction = 0.1f;
    private const float  PickSearchPx       = 600f;
    private const float  SwingArcDegrees    = 70f;

    private static readonly List<Unit3D> UnitsNearPoint = new();

    private readonly AttackCycle    attackCycle = new();
    private          PathFollower3D approachPath;
    private          double         approachStuckSec;
    private          Vector3        attackAimPoint;
    private          Unit3D         attackTarget;
    private          bool           hasDied;
    private          int            heldSlot = NoSlot;
    private          Unit3D         hoveredUnit;
    private          double         invulnerableTimeLeftSec;
    private          OmniLight3D    light;
    private          float          lightBaseRange;
    private          float          manaCurrent;
    private          SkillResource  orderedSkill;
    private          Vector3        spawnPosition;
    private          bool           swingFailed;
    private          Tween          swingLook;
    private          SkillResource  swingSkill;
    private          Node3D         weaponPivot;

    public Hero3D()
        => Weapon.ApplyTo(Stats);

    //Der Platz in der Liste ist der Platz der Skill-Leiste: linke Maustaste, rechte Maustaste, Q, E, R, F, 1 bis 4
    [Export]
    public Array<SkillResource> Skills { get; set; } = new();

    [Export]
    public int LifeBonus { get; set; } = 50;

    [Export]
    public float Movementspeed { get; set; } = 1000f;

    [Export]
    public float RespawnInvulnerabilitySec { get; set; } = 2f;

    public override Faction Faction => Faction.Player;

    public override float AvailableMana => ManaCurrent;

    public override float CombatTextHeight => 2.1f;

    public float ManaMaximum => Stats.GetFinalWhole(CombatStat.Mana);

    public float ManaCurrent
    {
        get => manaCurrent;
        set
        {
            var clamped = Math.Clamp(value, 0, ManaMaximum);

            if (clamped.Equals(manaCurrent))
                return;

            manaCurrent = clamped;

            ManaChanged?.Invoke();
        }
    }

    public event ManaChangedEventHandler ManaChanged;

    public override void _Ready()
    {
        base._Ready();

        ManaCurrent   = ManaMaximum;
        spawnPosition = GlobalPosition;
        approachPath  = new PathFollower3D(this);
        weaponPivot   = Visual?.GetNodeOrNull<Node3D>("WeaponPivot");
        light         = GetNodeOrNull<OmniLight3D>("Light");

        if (light is not null)
            lightBaseRange = light.OmniRange;

        ApplyLightRadius();

        LifeChanged += OnLifeChanged;
    }

    protected override void ApplyBaseValues(StatSheet sheet)
    {
        sheet.SetBase(CombatStat.Strength, 1);
        sheet.SetBase(CombatStat.Dexterity, 1);
        sheet.SetBase(CombatStat.Intelligence, 1);
        sheet.SetBase(CombatStat.Constitution, 1);
        sheet.SetBase(CombatStat.Awareness, 1);
        sheet.SetBase(CombatStat.Dodge, 6);
        sheet.SetBase(CombatStat.Life, LifeBonus);
        sheet.SetBase(CombatStat.Movementspeed, Movementspeed);
        sheet.SetBase(CombatStat.Manaregeneration, 0.5f);
        sheet.SetBase(CombatStat.LightRadius, 100);
    }

    protected override void OnStatsRecalculated()
    {
        if (ManaCurrent > ManaMaximum)
            ManaCurrent = ManaMaximum;

        ApplyLightRadius();
    }

    private void ApplyLightRadius()
    {
        if (light is not null)
            light.OmniRange = lightBaseRange * Stats.GetFinal(CombatStat.LightRadius) / 100f;
    }

    private void OnLifeChanged(Unit3D unit)
    {
        if (IsDead && !hasDied)
            Die();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDead)
            return;

        invulnerableTimeLeftSec = Math.Max(0, invulnerableTimeLeftSec - delta);

        RegenerateMana(delta);
        UpdateHoveredUnit();
        RepeatHeldSkill();
        AdvanceAttack(delta);
        Move(GetWantedDirection(delta), delta);
    }

    private void RegenerateMana(double delta)
    {
        if (ManaCurrent < ManaMaximum)
            ManaCurrent += Stats.GetFinal(CombatStat.Manaregeneration) * (float)delta;
    }

    public override void SpendMana(float amount)
        => ManaCurrent -= amount;

    #region Zielen

    public Vector3 GetMouseGroundPoint()
    {
        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return GlobalPosition;

        var mouse  = GetViewport().GetMousePosition();
        var origin = camera.ProjectRayOrigin(mouse);
        var normal = camera.ProjectRayNormal(mouse);

        return Mathf.IsZeroApprox(normal.Y) ? GlobalPosition : origin - normal * (origin.Y / normal.Y);
    }

    public Unit3D FindHostileUnitUnderMouse()
    {
        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return null;

        var mouse = GetViewport().GetMousePosition();

        return FindHostileUnitOnRay(camera.ProjectRayOrigin(mouse), camera.ProjectRayNormal(mouse));
    }

    //Der Kopf eines Gegners liegt auf dem Bildschirm über seinen Füßen, der Bodenpunkt unter der Maus also hinter ihm
    public Unit3D FindHostileUnitOnRay(Vector3 rayOrigin, Vector3 rayNormal)
    {
        Unit3D nearestUnit  = null;
        var    nearestDepth = float.MaxValue;
        var    groundPoint  = Mathf.IsZeroApprox(rayNormal.Y) ? rayOrigin : rayOrigin - rayNormal * (rayOrigin.Y / rayNormal.Y);

        UnitRegistry3D.FindNear(groundPoint, PickSearchPx, UnitsNearPoint);

        foreach (var unit in UnitsNearPoint)
        {
            if (!IsHostileTo(unit) || !unit.IsTargetable)
                continue;

            var depth = unit.GetPickDepth(rayOrigin, rayNormal);

            if (depth < 0f || depth >= nearestDepth)
                continue;

            nearestUnit  = unit;
            nearestDepth = depth;
        }

        return nearestUnit;
    }

    private void UpdateHoveredUnit()
    {
        var unitUnderMouse = FindHostileUnitUnderMouse();

        if (unitUnderMouse == hoveredUnit)
            return;

        if (IsInstanceValid(hoveredUnit))
            hoveredUnit.SetHighlight(false);

        hoveredUnit = unitUnderMouse;
        hoveredUnit?.SetHighlight(true);
    }

    #endregion

    #region Skills

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDead)
            return;

        for (var slot = 0; slot < Math.Min(Skills.Count, InputActions.SkillSlots.Length); slot++)
        {
            if (!@event.IsActionPressed(InputActions.SkillSlots[slot]))
                continue;

            if (UseSlot(slot))
            {
                heldSlot = slot;

                GetViewport().SetInputAsHandled();
            }

            return;
        }
    }

    public bool UseSlot(int slot, bool isRepeat = false)
    {
        var skill = GetSkill(slot);

        return skill is not null && UseSkill(skill, new Aim3D(GetMouseGroundPoint(), FindHostileUnitUnderMouse()));
    }

    public bool UseSkill(SkillResource skill, Aim3D aim)
    {
        if (IsDead || skill is null)
            return false;

        return skill.Kind == SkillKind.Attack ? OrderAttack(skill, aim) : CastSpell(skill, aim);
    }

    private SkillResource GetSkill(int slot)
        => slot >= 0 && slot < Skills.Count ? Skills[slot] : null;

    private bool CastSpell(SkillResource skill, Aim3D aim)
    {
        if (TryPayFor(skill, CombatRules.MinSpellCooldownSec) != SkillUseCheck.Ready)
            return false;

        if (RollActionFailure())
        {
            CombatText3D.Show(this, "Failed", Colors.Yellow, 28);

            return true;
        }

        Face(aim.CurrentPoint - GlobalPosition);

        SkillExecutor3D.Execute(this, skill, aim);

        return true;
    }

    private bool OrderAttack(SkillResource skill, Aim3D aim)
    {
        if (!aim.HasTarget || !IsHostileTo(aim.Target))
            return false;

        if (SkillGate.Check(skill.Definition, SkillCooldowns, AvailableMana) != SkillUseCheck.Ready)
            return false;

        var previousTarget = attackTarget;

        orderedSkill     = skill;
        attackTarget     = aim.Target;
        attackAimPoint   = aim.Point;
        approachStuckSec = 0;

        if (attackTarget != previousTarget)
            approachPath.Reset();

        return true;
    }

    private void RepeatHeldSkill()
    {
        if (heldSlot == NoSlot)
            return;

        if (!Input.IsActionPressed(InputActions.SkillSlots[heldSlot]))
        {
            heldSlot = NoSlot;

            return;
        }

        if (orderedSkill is not null || !attackCycle.IsReady)
            return;

        UseSlot(heldSlot, true);
    }

    private void AdvanceAttack(double delta)
    {
        var wasSwinging = !attackCycle.IsReady;

        if (attackCycle.Advance(delta))
            Strike();

        if (wasSwinging && attackCycle.IsReady)
            FinishSwing();
    }

    private Vector3 GetWantedDirection(double delta)
    {
        var inputDirection = GetInputDirection();

        if (inputDirection != Vector3.Zero)
        {
            CancelAttack();

            return inputDirection;
        }

        if (orderedSkill is null || !attackCycle.IsReady)
            return Vector3.Zero;

        if (!IsValidTarget(attackTarget))
        {
            ClearOrder();

            return Vector3.Zero;
        }

        if (DistancePxTo(attackTarget) > GetEngageRange(orderedSkill.Definition))
            return approachPath.GetDirectionTo(attackTarget.GlobalPosition, delta);

        StartSwing(attackTarget.GlobalPosition - GlobalPosition);

        return Vector3.Zero;
    }

    //Oben auf dem Bildschirm ist die Blickrichtung der Kamera auf dem Boden
    private Vector3 GetInputDirection()
    {
        var input = Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown);

        if (input == Vector2.Zero)
            return Vector3.Zero;

        var basis   = GetViewport().GetCamera3D()?.GlobalBasis ?? Basis.Identity;
        var right   = WorldScale.OnGround(basis.X).Normalized();
        var forward = WorldScale.OnGround(-basis.Z).Normalized();

        return right * input.X - forward * input.Y;
    }

    private float GetEngageRange(SkillDefinition skill)
        => skill.Delivery switch
        {
            SkillDelivery.Weapon     => Weapon.Range,
            SkillDelivery.Projectile => skill.Projectile.Reach * EngageFraction,
            _                        => float.MaxValue
        };

    private void Move(Vector3 direction, double delta)
    {
        if (direction != Vector3.Zero)
            Face(direction);

        MoveOnGround(direction, MovementspeedPx);

        GiveUpTargetWhenStuck(direction, delta);
    }

    private void GiveUpTargetWhenStuck(Vector3 direction, double delta)
    {
        var isApproaching = attackTarget is not null && direction != Vector3.Zero;

        if (!isApproaching || GetRealVelocity().Length() > WorldScale.ToMeters(MovementspeedPx) * StuckSpeedFraction)
        {
            approachStuckSec = 0;

            return;
        }

        approachStuckSec += delta;

        if (approachStuckSec < StuckTimeoutSec)
            return;

        ClearOrder();

        heldSlot         = NoSlot;
        approachStuckSec = 0;
    }

    //Bezahlt wird erst hier, denn auf dem Weg zum Ziel kann das Mana ausgegangen sein
    private void StartSwing(Vector3 toTarget)
    {
        if (TryPayFor(orderedSkill) != SkillUseCheck.Ready)
        {
            ClearOrder();

            return;
        }

        var swingSec = 1.0 / Math.Max(CombatRules.MinAttacksPerSecond, Stats.GetFinal(CombatStat.Attackspeed));

        swingSkill  = orderedSkill;
        swingFailed = RollActionFailure();

        attackCycle.Start(swingSec * ImpactFraction, swingSec * (1 - ImpactFraction));

        Face(toTarget);
        PlaySwingLook(swingSec);
    }

    private void Strike()
    {
        if (swingFailed)
        {
            CombatText3D.Show(this, "Failed", Colors.Yellow, 28);

            return;
        }

        if (swingSkill is not null)
            SkillExecutor3D.Execute(this, swingSkill, new Aim3D(attackAimPoint, attackTarget));
    }

    private void FinishSwing()
    {
        EndSwingLook();

        var previousTarget = attackTarget;

        swingSkill = null;

        ClearOrder();

        if (heldSlot != NoSlot)
            ContinueHeldAttack(previousTarget);
    }

    private void ContinueHeldAttack(Unit3D previousTarget)
    {
        var skill = GetSkill(heldSlot);

        if (skill is null || skill.Kind != SkillKind.Attack)
            return;

        var target = FindHostileUnitUnderMouse();

        if (target is null && IsValidTarget(previousTarget))
            target = previousTarget;

        OrderAttack(skill, new Aim3D(GetMouseGroundPoint(), target));
    }

    private void ClearOrder()
    {
        orderedSkill = null;
        attackTarget = null;
    }

    private void CancelAttack()
    {
        ClearOrder();

        swingSkill = null;

        if (GetSkill(heldSlot)?.Kind == SkillKind.Attack)
            heldSlot = NoSlot;

        attackCycle.CancelWindup();

        EndSwingLook();
    }

    private static bool IsValidTarget(Unit3D unit)
        => IsInstanceValid(unit) && unit.IsTargetable;

    private void PlaySwingLook(double swingSec)
    {
        if (weaponPivot is null)
            return;

        swingLook?.Kill();

        weaponPivot.RotationDegrees = new Vector3(0, -SwingArcDegrees, 0);

        swingLook = CreateTween();

        swingLook.TweenProperty(weaponPivot, "rotation_degrees", new Vector3(0, SwingArcDegrees, 0), swingSec * ImpactFraction);
        swingLook.TweenProperty(weaponPivot, "rotation_degrees", Vector3.Zero, swingSec * (1 - ImpactFraction));
    }

    private void EndSwingLook()
    {
        swingLook?.Kill();

        swingLook = null;

        if (weaponPivot is not null)
            weaponPivot.RotationDegrees = Vector3.Zero;
    }

    #endregion

    #region Tod und Respawn

    public override void ReceiveDamage(HitResult hit, Unit3D attacker = null)
    {
        if (invulnerableTimeLeftSec > 0)
            return;

        base.ReceiveDamage(hit, attacker);
    }

    private void Die()
    {
        hasDied = true;

        CancelAttack();
        attackCycle.Reset();
        StatusEffects.Clear();

        heldSlot = NoSlot;
        Velocity = Vector3.Zero;

        if (Visual is not null)
            Visual.RotationDegrees = new Vector3(90, Visual.RotationDegrees.Y, 0);

        RaiseDied();
    }

    public void Respawn()
    {
        if (!hasDied)
            return;

        GlobalPosition          = spawnPosition;
        Velocity                = Vector3.Zero;
        hasDied                 = false;
        LifeCurrent             = LifeMaximum;
        ManaCurrent             = ManaMaximum;
        invulnerableTimeLeftSec = RespawnInvulnerabilitySec;

        if (Visual is not null)
            Visual.RotationDegrees = Vector3.Zero;
    }

    #endregion
}
