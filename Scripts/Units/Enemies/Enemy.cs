using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class Enemy : BaseUnit
{
    public delegate void EngagedEventHandler(Enemy enemy);

    public delegate void ProvokedEventHandler(Enemy enemy);

    private const float  MinAttackspeedRate     = 0.1f;
    private const float  ArrivalDistancePx      = 24f;
    private const float  EngageFraction         = 0.9f;
    private const double SightCheckIntervalSec  = 0.2;
    private const float  MinFacingChangeSquared = 0.0004f;
    private const int    NameFontSize           = 20;
    private const int    ModsFontSize           = 15;
    private const float  NameLabelLiftPx        = 14f;
    private const float  NameLabelWidthPx       = 360f;

    private static readonly Color WindupTint    = new(1.6f, 0.7f, 0.7f);
    private static readonly Color ModsNameColor = new(0.8f, 0.8f, 0.8f);

    private static readonly StringName RunBlendPosition   = "parameters/StateMachine/MoveState/RunState/blend_position";
    private static readonly StringName IdleBlendPosition  = "parameters/StateMachine/MoveState/IdleState/blend_position";
    private static readonly StringName DeathBlendPosition = "parameters/StateMachine/MoveState/DeathState/blend_position";

    private static readonly AttackSkillResource StandardAttack = new() { Id = "attack", DisplayName = AttackDefinition.Standard.Name };

    private AnimationPlayer                   animationPlayer;
    private bool                              attackFailed;
    private SkillResource                     attackSkill;
    private EnemyBrain                        brain = new(new EnemyBehaviour());
    private Vector2                           facedDirection;
    private bool                              hasSight;
    private ProgressBar                       healthbar;
    private Vector2                           homePoint;
    private bool                              isAttackAnimationRunning;
    private bool                              isAwake = true;
    private EnemyRarityLook                   look    = EnemyRarityLook.Normal;
    private MonsterModRuntime                 modRuntime;
    private IReadOnlyList<MonsterModResource> mods = [];
    private Control                           nameLabel;
    private PathFollower                      pathFollower;
    private double                            secUntilSightCheck;
    private Vector2                           spawnPoint;
    private bool                              usesAnimationTree;
    private WeaponProfile                     weapon = WeaponProfile.Unarmed;
    private PackedScene                       weaponProjectileScene;

    [Export]
    public EnemyResource Definition { get; set; }

    [Export]
    public int Level { get; set; } = 1;

    //Verschiebt den Start der Animation, damit eine Gruppe nicht im Gleichschritt wackelt
    [Export]
    public bool RandomizeAnimationStart { get; set; }

    public string SpawnGroup { get; set; }

    public EnemyController Controller { get; set; }

    public BaseUnit Target { get; set; }

    public BaseUnit LastAttacker { get; private set; }

    public bool IsDying { get; private set; }

    public IReadOnlyList<MonsterModResource> Mods => mods;

    public EnemyRarity Rarity => EnemyRarityRules.FromModCount(mods.Count);

    public EnemyState State => brain.State;

    public bool IsInCombat => brain.IsInCombat;

    public bool IsResting => brain.IsResting;

    public int XpGranted => EnemyScaling.GetXp(Definition?.Xp ?? 0, look.XpFactor);

    public int LootRolls => look.LootRolls;

    public string LootTableId => Definition?.LootTableId;

    public string DisplayName => Definition?.NameOrId ?? Name;

    public override Faction Faction      => Faction.Monster;
    public override bool    IsTargetable => !IsDead && !IsDying;

    public override WeaponProfile Weapon => weapon;

    public override PackedScene WeaponProjectileScene => weaponProjectileScene;

    private AnimationTree AnimationTree  { get; set; }
    private Sprite2D      MovementSprite { get; set; }

    public event EngagedEventHandler  Engaged;
    public event ProvokedEventHandler Provoked;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Configure(EnemyResource definition, int level, IReadOnlyList<MonsterModResource> rolledMods, EnemyRarityLook rarityLook)
    {
        Definition = definition;
        Level      = level;
        mods       = rolledMods ?? [];
        look       = rarityLook ?? EnemyRarityLook.Normal;
    }

    public override void _Ready()
    {
        ApplyDefinition();

        base._Ready();

        Scale *= look.Scale;

        AnimationTree      = GetNode<AnimationTree>(nameof(AnimationTree));
        animationPlayer    = GetNodeOrNull<AnimationPlayer>(nameof(AnimationPlayer));
        MovementSprite     = RunSprite ?? GetNodeOrNull<Sprite2D>(nameof(Sprite2D));
        healthbar          = GetNode<ProgressBar>("%Healthbar");
        healthbar.MaxValue = LifeMaximum;
        healthbar.Value    = LifeCurrent;
        spawnPoint         = GlobalPosition;
        homePoint          = GlobalPosition;
        pathFollower       = new PathFollower(this);
        modRuntime         = new MonsterModRuntime(this, mods);

        if (RandomizeAnimationStart)
            RandomizeAnimation();

        usesAnimationTree = AnimationTree.Active;

        ShowNameLabel();

        PropertyChanged                += OnPropertyChanged;
        StatsChanged                   += OnStatsChanged;
        AnimationTree.AnimationStarted += AnimationTreeOnAnimationStarted;
    }

    private void ApplyDefinition()
    {
        if (Definition is null)
        {
            GD.PushWarning($"{Name} hat keine Gegner-Definition und behält die Werte aus der Szene.");

            return;
        }

        brain = new EnemyBrain(Definition.ToBehaviour());

        Stats.Update(sheet =>
        {
            StrengthBase      = EnemyScaling.GetAttribute(Definition.Strength, Definition.StrengthPerLevel, Level);
            DexterityBase     = EnemyScaling.GetAttribute(Definition.Dexterity, Definition.DexterityPerLevel, Level);
            IntelligenceBase  = EnemyScaling.GetAttribute(Definition.Intelligence, Definition.IntelligencePerLevel, Level);
            ConstitutionBase  = EnemyScaling.GetAttribute(Definition.Constitution, Definition.ConstitutionPerLevel, Level);
            AwarenessBase     = EnemyScaling.GetAttribute(Definition.Awareness, Definition.AwarenessPerLevel, Level);
            LifeBaseBonus     = Definition.LifeBonus;
            Movementspeed     = Definition.Movementspeed;
            ArmorBase         = Definition.Armor;
            DodgeBase         = Definition.Dodge;
            FireResiBase      = Definition.FireResistance;
            FrostResiBase     = Definition.FrostResistance;
            LightningResiBase = Definition.LightningResistance;

            foreach (var item in Definition.Equipment.Where(item => item is not null))
                sheet.AddModifiers(new ItemInstance(item.Definition, Level).GetEquipModifiers());

            foreach (var mod in mods)
                sheet.AddModifiers(mod.Definition.GetStampedModifiers());
        });

        WieldWeapon(Definition.WieldedWeapon);
    }

    private void WieldWeapon(WeaponBaseResource wieldedWeapon)
    {
        weapon                = wieldedWeapon is null ? Definition.NaturalWeapon : new ItemInstance(wieldedWeapon.Definition, Level).ToWeaponProfile();
        weaponProjectileScene = wieldedWeapon?.ProjectileScene;
    }

    private void RandomizeAnimation()
    {
        if (animationPlayer is null || !animationPlayer.HasAnimation(Animation.RunDown))
            return;

        AnimationTree.Active = false;

        animationPlayer.Play(Animation.RunDown);
        animationPlayer.Seek(GD.Randf() * animationPlayer.CurrentAnimationLength, true);

        AnimationTree.Active = true;
    }

    //Die Schrift gleicht die Skalierung des Monsters aus, sonst wäre sie bei großen Monstern unscharf
    private void ShowNameLabel()
    {
        if (Rarity == EnemyRarity.Normal)
            return;

        var title = new Label { Text = DisplayName, HorizontalAlignment = HorizontalAlignment.Center };
        var names = new Label { Text = string.Join(" · ", mods.Select(mod => mod.Definition.Name)), HorizontalAlignment = HorizontalAlignment.Center };

        title.AddThemeFontSizeOverride("font_size", NameFontSize);
        title.AddThemeColorOverride("font_color", look.NameColor);
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        title.AddThemeConstantOverride("outline_size", 4);

        names.AddThemeFontSizeOverride("font_size", ModsFontSize);
        names.AddThemeColorOverride("font_color", ModsNameColor);
        names.AddThemeColorOverride("font_outline_color", Colors.Black);
        names.AddThemeConstantOverride("outline_size", 4);

        var box = new VBoxContainer
        {
            Name              = "NameLabel",
            ZIndex            = 2,
            MouseFilter       = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(NameLabelWidthPx, 0),
            GrowVertical      = Control.GrowDirection.Begin,
            Scale             = Vector2.One / Scale
        };

        box.AddThemeConstantOverride("separation", -4);
        box.AddChild(title);
        box.AddChild(names);

        AddChild(box);

        box.Position = new Vector2(-NameLabelWidthPx / 2f / Scale.X, healthbar.Position.Y - (NameLabelLiftPx + box.GetCombinedMinimumSize().Y) / Scale.Y);

        nameLabel = box;
    }

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
    {
        if (MovementSprite is not null)
            MovementSprite.SelfModulate = active ? new Color(3f, 1f, 2.0f) : new Color(1, 1, 1);
    }

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

        modRuntime.Advance(delta);

        if (MovementDirection != Vector2.Zero)
            FaceTowards(MovementDirection);
    }

    private void FaceTowards(Vector2 direction)
    {
        if (direction.DistanceSquaredTo(facedDirection) < MinFacingChangeSquared)
            return;

        facedDirection = direction;

        AnimationTree.Set(RunBlendPosition, direction);
        AnimationTree.Set(IdleBlendPosition, direction);
        AnimationTree.Set(DeathBlendPosition, direction);
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
            healthbar.Value   = LifeCurrent;
            healthbar.Visible = LifeCurrent < LifeMaximum;

            if (IsDead)
                BeginDeath();
        }
        else
        {
            if (e.PropertyName == nameof(MovementDirection) && MovementDirection.Length() > 0.0f && !isAttackAnimationRunning)
                SetAsOnlyVisibleSprite(RunSprite); //Hack, die Statemachine im Animationtree Startet die Animation nicht mehr
        }
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

    public Vector2 SnapToNavigation(Vector2 point)
        => pathFollower.SnapToNavigation(point);

    public void TeleportTo(Vector2 point)
    {
        GlobalPosition = point;
        Velocity       = Vector2.Zero;

        pathFollower.Reset();
    }

    //Ruhende Monster fern vom Helden sparen sich Denken, Bewegung und Animation. Leben, Statuseffekte und Abklingzeiten laufen weiter
    public void SetAwake(bool awake)
    {
        if (isAwake == awake || IsDying)
            return;

        isAwake = awake;

        if (!awake)
            Stop();

        if (!isAttackAnimationRunning)
            AnimationTree.Active = awake && usesAnimationTree;

        if (nameLabel is not null)
            nameLabel.Visible = awake;
    }

    //Der Tod wird genau einmal ausgelöst: XP und Loot sofort, entfernt wird der Gegner erst nach der Todesanimation
    private void BeginDeath()
    {
        if (IsDying)
            return;

        IsDying           = true;
        healthbar.Visible = false;

        brain.Die();
        Stop();
        EndAttackLook();
        StatusEffects.Clear();

        if (nameLabel is not null)
            nameLabel.Visible = false;

        AnimationTree.Active = usesAnimationTree;

        GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D))?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        PropertyChanged -= OnPropertyChanged;

        RaiseDied();

        modRuntime.Release();

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

    public void Think(double delta)
    {
        if (IsDying)
            return;

        var skill    = brain.IsInCombat ? ChooseSkill() : null;
        var decision = brain.Tick(delta, Perceive(skill, delta));

        if (decision.Engages)
            Engaged?.Invoke(this);

        if (decision.GivesUp)
            BeginReturn();

        if (decision.StartsAttack)
            StartAttack(skill);

        if (decision.Strikes)
            Strike();

        if (decision.EndsAttack)
            EndAttackLook();

        Move(decision.Movement, delta);
    }

    private EnemyPerception Perceive(SkillResource skill, double delta)
    {
        var hasTarget   = IsInstanceValid(Target) && Target.IsTargetable;
        var distance    = hasTarget ? DistanceTo(Target) : float.MaxValue;
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
        var hasArrivedHome  = BodyCenter.DistanceTo(homePoint) <= ArrivalDistancePx;

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

        var ray = PhysicsRayQueryParameters2D.Create(BodyCenter, Target.BodyCenter, CollisionLayers.Walls);

        hasSight = GetWorld2D().DirectSpaceState.IntersectRay(ray).Count == 0;

        return hasSight;
    }

    private void BeginReturn()
    {
        var radius = Definition?.HomeRadius ?? 0f;
        var angle  = GameRandom.Shared.NextFloat() * MathF.Tau;
        var reach  = MathF.Sqrt(GameRandom.Shared.NextFloat()) * radius;

        homePoint = pathFollower.SnapToNavigation(spawnPoint + Vector2.FromAngle(angle) * reach);

        pathFollower.Reset();
    }

    private void StartAttack(SkillResource skill)
    {
        var toTarget = Target.BodyCenter - BodyCenter;

        attackSkill  = skill;
        attackFailed = RollActionFailure();

        TryPayFor(skill);

        BeginAttackLook(toTarget, brain.AttackSec);
    }

    private void Strike()
    {
        Modulate = Colors.White;

        if (attackFailed)
            this.ShowCombatText("Failed", Colors.Yellow, 28);
        else if (attackSkill is not null && IsInstanceValid(Target))
            SkillExecutor.Execute(this, attackSkill, new SkillAim(Target.BodyCenter, Target));
    }

    private void Move(EnemyMovement movement, double delta)
    {
        switch (movement)
        {
            case EnemyMovement.TowardTarget when IsInstanceValid(Target):
                MoveAlong(pathFollower.GetDirectionTo(Target.BodyCenter, delta), MovementspeedFinal);

                break;
            case EnemyMovement.TowardHome:
                MoveAlong(pathFollower.GetDirectionTo(homePoint, delta), MovementspeedFinal * (Definition?.ReturnSpeedFactor ?? 1f));

                break;
            default:
                Stop();

                break;
        }
    }

    private void MoveAlong(Vector2 direction, float speed)
    {
        MovementDirection = direction;
        Velocity          = speed * direction;

        MoveAndSlide();
    }

    private void Stop()
    {
        MovementDirection = Vector2.Zero;
        Velocity          = Vector2.Zero;
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

        AnimationTree.Active = usesAnimationTree && isAwake;
    }

    private static string GetAttackAnimationName(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            return direction.X > 0 ? Animation.AttackRight : Animation.AttackLeft;

        return direction.Y > 0 ? Animation.AttackDown : Animation.AttackTop;
    }
}
