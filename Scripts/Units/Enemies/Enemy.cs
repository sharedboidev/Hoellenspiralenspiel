using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class Enemy : BaseUnit
{
    public delegate void EngagedEventHandler(Enemy enemy);

    public delegate void ProvokedEventHandler(Enemy enemy);

    private const float  MinAttackspeedRate    = 0.1f;
    private const float  ArrivalDistancePx     = 24f;
    private const double HomeBlockedSec        = 0.6;
    private const float  BlockedSpeedFraction  = 0.1f;
    private const float  NearHomePx            = 300f;
    private const float  EngageFraction        = 0.9f;
    private const double SightCheckIntervalSec = 0.2;
    private const float  LungeMeters           = 0.3f;
    private const double DeathLookSec          = 0.4;
    private const double CorpseSec             = 0.5;
    private const float  NameTagLiftMeters     = 0.15f;
    private const float  EliteGlowEnergy       = 0.35f;
    private const float  AuraRadiusFactor      = 1.3f;

    private static readonly Vector3 WindupTint = new(1.6f, 0.7f, 0.7f);

    private static readonly StringName TintParameter           = "tint";
    private static readonly StringName EmissionParameter       = "emission";
    private static readonly StringName EmissionEnergyParameter = "emission_energy";

    private static readonly AttackSkillResource StandardAttack = new() { Id = "attack", DisplayName = AttackDefinition.Standard.Name };

    private bool                              attackFailed;
    private float                             closestToHomePx;
    private EliteAura                         aura;
    private Tween                             attackLook;
    private double                            attackRecoverySec;
    private SkillResource                     attackSkill;
    private MeshInstance3D                    blobShadow;
    private ShaderMaterial                    bodyMaterial;
    private EnemyBrain                        brain = new(new EnemyBehaviour());
    private bool                              hasSight;
    private HealthBar                         healthbar;
    private Vector3                           homePoint;
    private bool                              isAwake = true;
    private bool                              isSeen  = true;
    private float                             shownVisibility = 1f;
    private EnemyRarityLook                   look    = EnemyRarityLook.Normal;
    private MonsterModRuntime                 modRuntime;
    private IReadOnlyList<MonsterModResource> mods = [];
    private NameTag                           nameTag;
    private PathFollower                      pathFollower;
    private double                            secBlockedOnWayHome;
    private double                            secUntilSightCheck;
    private Vector3                           spawnPoint;
    private WeaponProfile                     weapon = WeaponProfile.Unarmed;
    private PackedScene                       weaponProjectileScene;
    private EnemyRarity                       rarity;

    [Export]
    public EnemyResource Definition { get; set; }

    [Export]
    public int Level { get; set; } = 1;

    [Export]
    public float HealthbarHeight { get; set; } = 1.3f;

    public string SpawnGroup { get; set; }

    //Der Platz in der Reihenfolge, in der die Marker einer Ebene spawnen. Beschworene Gegner haben keinen
    public int SpawnIndex { get; set; } = -1;

    public EnemyController Controller { get; set; }

    public BaseUnit Target { get; set; }

    public BaseUnit LastAttacker { get; private set; }

    public bool IsDying { get; private set; }

    public IReadOnlyList<MonsterModResource> Mods => mods;

    public EnemyRarity Rarity => rarity;

    public EnemyState State => brain.State;

    public bool IsInCombat => brain.IsInCombat;

    public bool IsResting => brain.IsResting;

    public int XpGranted => EnemyScaling.GetXp(Definition?.Xp ?? 0, look.XpFactor);

    public int LootRolls => look.LootRolls;

    public float GoldFactor => look.GoldFactor;

    public string LootTableId => Definition?.LootTableId;

    public string DisplayName => Definition?.NameOrId ?? Name;

    public NameTag NameTag => nameTag;

    public Color NameColor => look.NameColor;

    public EliteAura Aura => aura;

    public override Faction Faction      => Faction.Monster;
    public override bool    IsTargetable => !IsDead && !IsDying;
    public override bool    IsSeen       => isSeen;
    public override bool    IsSolid      => !IsDying;

    public override WeaponProfile Weapon => weapon;

    public override PackedScene WeaponProjectileScene => weaponProjectileScene;

    public override float CombatTextHeight => HealthbarHeight + 0.3f;

    public event EngagedEventHandler  Engaged;
    public event ProvokedEventHandler Provoked;

    //Vor dem Einhängen in den Szenenbaum aufrufen. Ohne Stufe folgt sie der Zahl der Mods
    public void Configure(EnemyResource definition, int level, IReadOnlyList<MonsterModResource> rolledMods = null, EnemyRarityLook rarityLook = null, EnemyRarity? rank = null)
    {
        Definition = definition;
        Level      = level;
        mods       = rolledMods ?? [];
        look       = rarityLook ?? EnemyRarityLook.Normal;
        rarity     = rank ?? EnemyRarityRules.FromModCount(mods.Count);
    }

    public override void _Ready()
    {
        base._Ready();

        ScaleBody(look.Scale);

        HealthbarHeight *= look.Scale;
        spawnPoint      =  GlobalPosition;
        homePoint       =  GlobalPosition;
        pathFollower    =  new PathFollower(this);
        modRuntime      =  new MonsterModRuntime(this, mods);

        blobShadow = GetNodeOrNull<MeshInstance3D>("BlobShadow");

        OwnMaterials();
        AddHealthbar();
        AddNameTag();
        AddAura();

        LifeChanged += OnLifeChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        UnitSight.Release(bodyMaterial);

        if (IsInstanceValid(nameTag))
            nameTag.QueueFree();
    }

    protected override void ApplyBaseValues(StatSheet sheet)
    {
        if (Definition is null)
        {
            GD.PushWarning($"{Name} hat keine Gegner-Definition.");

            return;
        }

        var core = Definition.Core;

        brain                 = new EnemyBrain(core.Behaviour);
        weapon                = EnemyStats.GetWeapon(core, Level);
        weaponProjectileScene = Definition.WieldedWeapon?.ProjectileScene;

        EnemyStats.Apply(sheet, core, Level, mods.Select(mod => mod.Definition));
    }

    //Alle Teile blenden am Rand der Sicht ein. Der Körper bekommt dazu sein eigenes Material, sonst färbte das Ausholen alle Gegner dieser Szene
    private void OwnMaterials()
    {
        if (Visual is null)
            return;

        var body = FindBody();

        foreach (var part in Visual.GetAllChildren<MeshInstance3D>())
        {
            for (var surface = 0; surface < part.GetSurfaceOverrideMaterialCount(); surface++)
            {
                if (part.GetActiveMaterial(surface) is not ShaderMaterial material || !UnitSight.CanAdopt(material))
                    continue;

                if (part != body || surface > 0)
                {
                    part.SetSurfaceOverrideMaterial(surface, UnitSight.GetShared(material));

                    continue;
                }

                bodyMaterial = UnitSight.CreateOwn(material);

                part.SetSurfaceOverrideMaterial(surface, bodyMaterial);
            }
        }
    }

    private void AddHealthbar()
    {
        healthbar = new HealthBar { Name = "Healthbar", Visible = false };

        AddChild(healthbar);

        healthbar.Position = Vector3.Up * HealthbarHeight;
    }

    private void AddNameTag()
    {
        if (Rarity == EnemyRarity.Normal)
            return;

        nameTag = NameTag.Create(this, HealthbarHeight + NameTagLiftMeters, DisplayName, look.NameColor, string.Join(" · ", mods.Select(mod => mod.Definition.Name)));

        CombatText.GetLayer(GetTree().CurrentScene ?? GetTree().Root).AddChild(nameTag);
    }

    private void AddAura()
    {
        if (Rarity == EnemyRarity.Normal)
            return;

        aura = EliteAura.Create(look.NameColor, PickRadius * AuraRadiusFactor);

        AddChild(aura);

        bodyMaterial?.SetShaderParameter(EmissionParameter, look.NameColor);
        bodyMaterial?.SetShaderParameter(EmissionEnergyParameter, EliteGlowEnergy);
    }

    private void OnLifeChanged(BaseUnit unit)
    {
        healthbar.SetRatio(LifeMaximum > 0 ? LifeCurrent / LifeMaximum : 0f);

        healthbar.Visible = LifeCurrent < LifeMaximum && !IsDying;

        if (IsDead)
            BeginDeath();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!IsDying)
            modRuntime.Advance(delta);
    }

    protected override void RegenerateLife(double delta)
    {
        if (!IsDying)
            base.RegenerateLife(delta);
    }

    public override void ReceiveDamage(HitResult hit, BaseUnit attacker = null)
    {
        if (IsTargetable)
        {
            if (IsInstanceValid(attacker))
                LastAttacker = attacker;

            Provoke();

            Provoked?.Invoke(this);
        }

        base.ReceiveDamage(hit, attacker);
    }

    //Auch ein abgewehrter Treffer und der Hilferuf der Gruppe wecken das Monster, egal wie weit das Ziel entfernt ist
    public void Provoke()
        => brain.Provoke();

    public void AddModifiers(IEnumerable<StatModifierResource> modifiers, double durationSec)
        => modRuntime.AddModifiers(modifiers, durationSec);

    public Vector3 SnapToNavigation(Vector3 point)
        => pathFollower.SnapToNavigation(point);

    public void TeleportTo(Vector3 point)
    {
        GlobalPosition = Controller?.FindFreeSpot(new SpawnArea(point), BodyRadius, this) ?? WorldScale.OnGround(point);
        Velocity       = Vector3.Zero;

        pathFollower.Reset();
    }

    //Ruhende Monster fern vom Helden sparen sich Denken und Bewegung. Leben, Statuseffekte und Abklingzeiten laufen weiter
    public void SetAwake(bool awake)
    {
        if (isAwake == awake || IsDying)
            return;

        isAwake = awake;

        if (!awake)
            Velocity = Vector3.Zero;

        ShowNameTag();
        Animations?.SetRunning(isAwake && isSeen);
    }

    //Wer hinter einer Mauer steht, bleibt verborgen, samt Aura, Lebensbalken und Namensschild
    public void SetSeen(bool seen)
    {
        if (isSeen == seen || IsDying)
            return;

        isSeen  = seen;
        Visible = seen;

        ShowNameTag();
        Animations?.SetRunning(isAwake && isSeen);
    }

    //Der Körper blendet über seinen Shader ein. Schatten, Aura, Lebensbalken und Namensschild folgen diesem Wert
    public void SetVisibility(float visibility)
    {
        if (Mathf.IsEqualApprox(visibility, shownVisibility) || IsDying)
            return;

        shownVisibility = visibility;

        healthbar.SetVisibility(visibility);

        if (blobShadow is not null)
            blobShadow.Transparency = 1f - visibility;

        if (aura is not null)
            aura.Visibility = visibility;

        if (nameTag is not null)
            nameTag.Modulate = new Color(1f, 1f, 1f, visibility);
    }

    private void ShowNameTag()
    {
        if (nameTag is not null)
            nameTag.IsShown = isAwake && isSeen;
    }

    //Der Tod wird genau einmal ausgelöst: XP und Loot sofort, entfernt wird der Gegner erst nach dem Zusammensinken
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

        if (nameTag is not null)
            nameTag.IsShown = false;

        aura?.QueueFree();

        GetNodeOrNull<CollisionShape3D>(nameof(CollisionShape3D))?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);

        LifeChanged -= OnLifeChanged;

        if (IsInstanceValid(LastAttacker))
            LastAttacker.NotifyKill(this);

        RaiseDied();

        modRuntime.Release();

        if (Visual is null)
        {
            QueueFree();

            return;
        }

        if (Animations?.CanDie == true)
        {
            var dying = CreateTween();

            dying.TweenInterval(Animations.Die() + CorpseSec);
            dying.TweenCallback(Callable.From(QueueFree));

            return;
        }

        var collapse = CreateTween();

        collapse.TweenProperty(Visual, "scale", Visual.Scale * new Vector3(1.4f, 0.05f, 1.4f), DeathLookSec);
        collapse.TweenCallback(Callable.From(QueueFree));
    }

    public void Think(double delta)
    {
        if (IsDying)
            return;

        var skill      = brain.IsInCombat ? ChooseSkill() : null;
        var perception = Perceive(skill, delta);
        var decision   = brain.Tick(delta, perception);

        if (decision.Engages)
            Engaged?.Invoke(this);

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
            SkillDelivery.Weapon           => Weapon.Range,
            SkillDelivery.Projectile       => definition.Projectile.Reach * EngageFraction,
            SkillDelivery.AreaAroundCaster => definition.Area.Radius * EngageFraction,
            _                              => float.MaxValue
        };

        return Math.Min(skillRange, Definition?.AttackRange ?? skillRange);
    }

    private bool NeedsSight(SkillResource skill)
        => skill.Delivery == SkillDelivery.Projectile || (skill.Delivery == SkillDelivery.Weapon && Weapon.IsRanged);

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

        homePoint           = pathFollower.SnapToNavigation(spawnPoint + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * reach);
        secBlockedOnWayHome = 0;
        closestToHomePx     = float.MaxValue;

        pathFollower.Reset();
    }

    //Steht ein anderer auf dem Platz oder im Weg, endet der Rückweg dort, wo es nicht mehr weitergeht.
    //Wer kurz vor dem Ziel nur noch um einen anderen herumrutscht, kommt dem Ziel nicht näher und gilt ebenfalls als angekommen
    private void SettleWhenBlocked(float speedPx, double delta)
    {
        var distancePx = WorldScale.GroundDistancePx(GlobalPosition, homePoint);
        var isCrawling = GetRealVelocity().Length() <= WorldScale.ToMeters(speedPx) * BlockedSpeedFraction;
        var isCircling = distancePx <= NearHomePx && distancePx >= closestToHomePx;

        closestToHomePx = Math.Min(closestToHomePx, distancePx);

        if (!isCrawling && !isCircling)
        {
            secBlockedOnWayHome = 0;

            return;
        }

        secBlockedOnWayHome += delta;

        if (secBlockedOnWayHome >= HomeBlockedSec)
            homePoint = GlobalPosition;
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

        Animations?.BeginRecovery(attackRecoverySec);

        if (attackFailed)
            CombatText.Show(this, "Failed", Colors.Yellow, 28);
        else if (attackSkill is not null && IsInstanceValid(Target))
            SkillExecutor.Execute(this, attackSkill, new SkillAim(Target.GlobalPosition, Target));
    }

    private void Move(EnemyMovement movement, double delta)
    {
        switch (movement)
        {
            case EnemyMovement.TowardTarget when IsInstanceValid(Target):
                MoveAlong(pathFollower.GetDirectionTo(Target.GlobalPosition, delta), MovementspeedPx);

                break;
            case EnemyMovement.TowardHome:
                var returnSpeedPx = MovementspeedPx * (Definition?.ReturnSpeedFactor ?? 1f);

                MoveAlong(pathFollower.GetDirectionTo(homePoint, delta), returnSpeedPx);
                SettleWhenBlocked(returnSpeedPx, delta);

                break;
            default:
                StandStill();

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

        if (Animations?.CanAttack == true)
        {
            attackRecoverySec = recoverySec;

            Animations.BeginWindup(windupSec);

            return;
        }

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

        Animations?.EndAttack();
        attackLook?.Kill();

        attackLook = null;

        if (Visual is not null)
            Visual.Position = Vector3.Zero;
    }

    private void SetTint(Vector3 tint)
        => bodyMaterial?.SetShaderParameter(TintParameter, tint);
}
