using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class Enemy3D : Unit3D
{
    public delegate void ProvokedEventHandler(Enemy3D enemy);

    private const float  MinAttackspeedRate    = 0.1f;
    private const float  ArrivalDistancePx     = 24f;
    private const float  EngageFraction        = 0.9f;
    private const double SightCheckIntervalSec = 0.2;
    private const float  LungeMeters           = 0.3f;
    private const double DeathLookSec          = 0.4;

    private const float HighlightEnergy = 0.6f;

    private static readonly Vector3 WindupTint    = new(1.6f, 0.7f, 0.7f);
    private static readonly Color   HighlightGlow = new(0.9f, 0.3f, 0.6f);

    private static readonly StringName TintParameter           = "tint";
    private static readonly StringName EmissionParameter       = "emission";
    private static readonly StringName EmissionEnergyParameter = "emission_energy";

    private static readonly AttackSkillResource StandardAttack = new() { Id = "attack", DisplayName = AttackDefinition.Standard.Name };

    private bool               attackFailed;
    private Tween              attackLook;
    private SkillResource      attackSkill;
    private ShaderMaterial     bodyMaterial;
    private EnemyBrain         brain = new(new EnemyBehaviour());
    private bool               hasSight;
    private HealthBar3D        healthbar;
    private Vector3            homePoint;
    private bool               isAwake = true;
    private PathFollower3D     pathFollower;
    private double             secUntilSightCheck;
    private Vector3            spawnPoint;
    private WeaponProfile      weapon = WeaponProfile.Unarmed;

    [Export]
    public EnemyResource Definition { get; set; }

    [Export]
    public int Level { get; set; } = 1;

    [Export]
    public float HealthbarHeight { get; set; } = 1.3f;

    public string SpawnGroup { get; set; }

    public Unit3D Target { get; set; }

    public bool IsDying { get; private set; }

    public EnemyState State => brain.State;

    public bool IsInCombat => brain.IsInCombat;

    public bool IsResting => brain.IsResting;

    public override Faction Faction      => Faction.Monster;
    public override bool    IsTargetable => !IsDead && !IsDying;

    public override WeaponProfile Weapon => weapon;

    public override float CombatTextHeight => HealthbarHeight + 0.3f;

    public event ProvokedEventHandler Provoked;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Configure(EnemyResource definition, int level)
    {
        Definition = definition;
        Level      = level;
    }

    public override void _Ready()
    {
        base._Ready();

        spawnPoint   = GlobalPosition;
        homePoint    = GlobalPosition;
        pathFollower = new PathFollower3D(this);

        OwnBodyMaterial();
        AddHealthbar();

        LifeChanged += OnLifeChanged;
    }

    protected override void ApplyBaseValues(StatSheet sheet)
    {
        if (Definition is null)
        {
            GD.PushWarning($"{Name} hat keine Gegner-Definition.");

            return;
        }

        brain  = new EnemyBrain(Definition.ToBehaviour());
        weapon = Definition.WieldedWeapon is null ? Definition.NaturalWeapon : new ItemInstance(Definition.WieldedWeapon.Definition, Level).ToWeaponProfile();

        sheet.SetBase(CombatStat.Strength, EnemyScaling.GetAttribute(Definition.Strength, Definition.StrengthPerLevel, Level));
        sheet.SetBase(CombatStat.Dexterity, EnemyScaling.GetAttribute(Definition.Dexterity, Definition.DexterityPerLevel, Level));
        sheet.SetBase(CombatStat.Intelligence, EnemyScaling.GetAttribute(Definition.Intelligence, Definition.IntelligencePerLevel, Level));
        sheet.SetBase(CombatStat.Constitution, EnemyScaling.GetAttribute(Definition.Constitution, Definition.ConstitutionPerLevel, Level));
        sheet.SetBase(CombatStat.Awareness, EnemyScaling.GetAttribute(Definition.Awareness, Definition.AwarenessPerLevel, Level));
        sheet.SetBase(CombatStat.Life, Definition.LifeBonus);
        sheet.SetBase(CombatStat.Movementspeed, Definition.Movementspeed);
        sheet.SetBase(CombatStat.Armor, Definition.Armor);
        sheet.SetBase(CombatStat.Dodge, Definition.Dodge);
        sheet.SetBase(CombatStat.FireResistance, Definition.FireResistance);
        sheet.SetBase(CombatStat.FrostResistance, Definition.FrostResistance);
        sheet.SetBase(CombatStat.LightningResistance, Definition.LightningResistance);

        foreach (var item in Definition.Equipment.Where(item => item is not null))
            sheet.AddModifiers(new ItemInstance(item.Definition, Level).GetEquipModifiers());
    }

    //Jede Instanz bekommt ihr eigenes Material, sonst färbte das Ausholen alle Gegner dieser Szene
    private void OwnBodyMaterial()
    {
        var body = Visual?.GetNodeOrNull<MeshInstance3D>("Body");

        if (body?.GetActiveMaterial(0) is not ShaderMaterial material)
            return;

        bodyMaterial = (ShaderMaterial)material.Duplicate();

        bodyMaterial.SetShaderParameter(EmissionParameter, HighlightGlow);

        body.SetSurfaceOverrideMaterial(0, bodyMaterial);
    }

    private void AddHealthbar()
    {
        healthbar = new HealthBar3D { Name = "Healthbar", Visible = false };

        AddChild(healthbar);

        healthbar.Position = Vector3.Up * HealthbarHeight;
    }

    private void OnLifeChanged(Unit3D unit)
    {
        healthbar.SetRatio(LifeMaximum > 0 ? LifeCurrent / LifeMaximum : 0f);

        healthbar.Visible = LifeCurrent < LifeMaximum && !IsDying;

        if (IsDead)
            BeginDeath();
    }

    public override void SetHighlight(bool active)
        => bodyMaterial?.SetShaderParameter(EmissionEnergyParameter, active ? HighlightEnergy : 0f);

    protected override void RegenerateLife(double delta)
    {
        if (!IsDying)
            base.RegenerateLife(delta);
    }

    public override void ReceiveDamage(HitResult hit, Unit3D attacker = null)
    {
        if (IsTargetable)
        {
            Provoke();

            Provoked?.Invoke(this);
        }

        base.ReceiveDamage(hit, attacker);
    }

    public void Provoke()
        => brain.Provoke();

    public void SetAwake(bool awake)
    {
        if (isAwake == awake || IsDying)
            return;

        isAwake = awake;

        if (!awake)
            Velocity = Vector3.Zero;
    }

    private void BeginDeath()
    {
        if (IsDying)
            return;

        IsDying           = true;
        healthbar.Visible = false;

        brain.Die();
        EndAttackLook();
        StatusEffects.Clear();

        Velocity = Vector3.Zero;

        GetNodeOrNull<CollisionShape3D>(nameof(CollisionShape3D))?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);

        LifeChanged -= OnLifeChanged;

        RaiseDied();

        if (Visual is null)
        {
            QueueFree();

            return;
        }

        var collapse = CreateTween();

        collapse.TweenProperty(Visual, "scale", new Vector3(1.4f, 0.05f, 1.4f), DeathLookSec);
        collapse.TweenCallback(Callable.From(QueueFree));
    }

    public void Think(double delta)
    {
        if (IsDying)
            return;

        var skill      = brain.IsInCombat ? ChooseSkill() : null;
        var perception = Perceive(skill, delta);
        var decision   = brain.Tick(delta, perception);

        if (decision.GivesUp)
            BeginReturn();

        if (decision.StartsAttack)
            StartAttack(skill, perception);

        if (decision.Strikes)
            Strike();

        if (decision.EndsAttack)
            EndAttackLook();

        Move(decision.Movement, delta);
    }

    private EnemyPerception Perceive(SkillResource skill, double delta)
    {
        var hasTarget   = IsInstanceValid(Target) && Target.IsTargetable;
        var distance    = hasTarget ? DistancePxTo(Target) : float.MaxValue;
        var engageRange = 0f;
        var canAttack   = false;

        if (hasTarget && skill is not null)
        {
            engageRange = GetEngageRange(skill);
            canAttack   = SkillGate.Check(skill.Definition, SkillCooldowns, AvailableMana) == SkillUseCheck.Ready;

            if (distance <= engageRange && NeedsSight(skill) && !HasSightOfTarget(delta))
                engageRange = 0f;
        }

        var attackspeedRate = Math.Max(MinAttackspeedRate, Stats.GetTotalMultiplier(CombatStat.Attackspeed));
        var windupSec       = (Definition?.AttackWindupSec ?? 0.3f) / attackspeedRate;
        var recoverySec     = (Definition?.AttackRecoverySec ?? 0.2f) / attackspeedRate;
        var hasArrivedHome  = WorldScale.GroundDistancePx(GlobalPosition, homePoint) <= ArrivalDistancePx;

        return new EnemyPerception(hasTarget, distance, engageRange, canAttack, hasArrivedHome, windupSec, recoverySec);
    }

    private SkillResource ChooseSkill()
    {
        var skills = Definition?.Skills;

        if (skills is null || skills.Count == 0)
            return StandardAttack;

        SkillResource firstSkill = null;

        foreach (var skill in skills)
        {
            if (skill is null)
                continue;

            firstSkill ??= skill;

            if (SkillCooldowns.IsReady(skill.Id))
                return skill;
        }

        return firstSkill ?? StandardAttack;
    }

    private float GetEngageRange(SkillResource skill)
    {
        var definition = skill.Definition;

        var skillRange = definition.Delivery switch
        {
            SkillDelivery.Weapon     => Weapon.Range,
            SkillDelivery.Projectile => definition.Projectile.Reach * EngageFraction,
            _                        => float.MaxValue
        };

        return Math.Min(skillRange, Definition?.AttackRange ?? skillRange);
    }

    private static bool NeedsSight(SkillResource skill)
        => skill.Delivery == SkillDelivery.Projectile;

    private bool HasSightOfTarget(double delta)
    {
        secUntilSightCheck -= delta;

        if (secUntilSightCheck > 0)
            return hasSight;

        secUntilSightCheck = SightCheckIntervalSec;

        var ray = PhysicsRayQueryParameters3D.Create(BodyCenter, Target.BodyCenter, CollisionLayers.Walls);

        hasSight = GetWorld3D().DirectSpaceState.IntersectRay(ray).Count == 0;

        return hasSight;
    }

    private void BeginReturn()
    {
        var radius = WorldScale.ToMeters(Definition?.HomeRadius ?? 0f);
        var angle  = GameRandom.Shared.NextFloat() * MathF.Tau;
        var reach  = MathF.Sqrt(GameRandom.Shared.NextFloat()) * radius;

        homePoint = pathFollower.SnapToNavigation(spawnPoint + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * reach);

        pathFollower.Reset();
    }

    private void StartAttack(SkillResource skill, EnemyPerception perception)
    {
        attackSkill  = skill;
        attackFailed = RollActionFailure();

        TryPayFor(skill);

        BeginAttackLook(Target.GlobalPosition - GlobalPosition, perception.WindupSec, perception.RecoverySec);
    }

    private void Strike()
    {
        SetTint(Vector3.One);

        if (attackFailed)
            CombatText3D.Show(this, "Failed", Colors.Yellow, 28);
        else if (attackSkill is not null && IsInstanceValid(Target))
            SkillExecutor3D.Execute(this, attackSkill, new Aim3D(Target.GlobalPosition, Target));
    }

    private void Move(EnemyMovement movement, double delta)
    {
        switch (movement)
        {
            case EnemyMovement.TowardTarget when IsInstanceValid(Target):
                MoveAlong(pathFollower.GetDirectionTo(Target.GlobalPosition, delta), MovementspeedPx);

                break;
            case EnemyMovement.TowardHome:
                MoveAlong(pathFollower.GetDirectionTo(homePoint, delta), MovementspeedPx * (Definition?.ReturnSpeedFactor ?? 1f));

                break;
            default:
                Velocity = Vector3.Zero;

                break;
        }
    }

    private void MoveAlong(Vector3 direction, float speedPx)
    {
        Face(direction);
        MoveOnGround(direction, speedPx);
    }

    private void BeginAttackLook(Vector3 toTarget, double windupSec, double recoverySec)
    {
        Face(toTarget);
        SetTint(WindupTint);

        if (Visual is null || windupSec <= 0)
            return;

        var lunge = WorldScale.OnGround(toTarget).Normalized() * LungeMeters;

        attackLook?.Kill();

        attackLook = CreateTween();

        attackLook.TweenProperty(Visual, "position", lunge, windupSec).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Back);
        attackLook.TweenProperty(Visual, "position", Vector3.Zero, Math.Max(0.05, recoverySec));
    }

    private void EndAttackLook()
    {
        SetTint(Vector3.One);

        attackLook?.Kill();

        attackLook = null;

        if (Visual is not null)
            Visual.Position = Vector3.Zero;
    }

    private void SetTint(Vector3 tint)
        => bodyMaterial?.SetShaderParameter(TintParameter, tint);
}
