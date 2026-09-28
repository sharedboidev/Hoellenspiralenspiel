using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Progression;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.UI.Skills;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;
using ResourceOrb = Hoellenspiralenspiel.Scripts.UI.Character.ResourceOrb;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : BaseUnit
{
    public delegate void EquipmentChangedEventHandler();

    public delegate void LeveledUpEventHandler(Player2D player);

    public delegate void RespawnedEventHandler();

    private const float ImpactFraction = 0.5f;

    //Skills ohne eigene Reichweite setzt der Held ein, sobald das Ziel in diesem Anteil ihrer Reichweite steht
    private const float EngageFraction = 0.9f;

    private const int    NoSlot             = -1;
    private const float  SkillBarMarginPx   = 60f;
    private const double StuckTimeoutSec    = 0.4;
    private const float  StuckSpeedFraction = 0.1f;

    private static readonly string[] CompassSuffixes = ["e", "se", "s", "sw", "w", "nw", "n", "ne"];
    private static readonly Color    DeathTint       = new(0.6f, 0.1f, 0.1f, 0.7f);

    private readonly AttackCycle attackCycle = new();

    private readonly Dictionary<PointLight2D, float> lightBaseScales = new();
    private          AnimationPlayer                 animationPlayer;
    private          double                          approachStuckSec;
    private          Vector2                         attackAimPoint;
    private          BaseUnit                        attackTarget;
    private          BaseWeapon                      equippedWeapon;
    private          bool                            hasDied;
    private          int                             heldSlot = NoSlot;
    private          BaseUnit                        hoveredUnit;
    private          double                          invulnerableTimeLeftSec;
    private          bool                            isSwingAnimationRunning;
    private          LevelUpEffect                   levelUpEffect;
    [Export] private ResourceOrb                     lifeOrb;
    private          int                             lightRadiusBase = 100;
    private          float                           manaCurrent;
    [Export] private ResourceOrb                     manaOrb;
    private          float                           manaregenerationBase = .5f;
    private          SkillResource                   orderedSkill;
    [Export] public  HBoxContainer                   SkillBar;
    private          Vector2                         spawnPosition;
    private          bool                            swingFailed;
    private          SkillResource                   swingSkill;
    private          WeaponProfile                   weapon = WeaponProfile.Unarmed;
    private          long                            xpTotal;

    public Player2D()
    {
        Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Manaregeneration, manaregenerationBase);
            sheet.SetBase(CombatStat.LightRadius, lightRadiusBase);
        });

        weapon.ApplyTo(Stats);
    }

    public override Faction Faction => Faction.Player;

    public override Vector2 CombatTextOffset => new(0, -170);

    public override WeaponProfile Weapon => weapon;

    public override PackedScene WeaponProjectileScene => equippedWeapon?.ProjectileScene;

    public override float AvailableMana => ManaCurrent;

    public SkillLoadout Loadout { get; } = new(InputActions.SkillSlots.Length);

    //Bis entschieden ist, wie der Held Skills bekommt, kennt er alle
    public IReadOnlyList<SkillResource> KnownSkills => SkillLibrary.PlayerSkills;

    public long LastXpLoss { get; private set; }

    private AnimationTree AnimationTree { get; set; }

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    [Export]
    public float RespawnInvulnerabilitySec { get; set; } = 2f;

    [Export]
    public float ManaregenerationBase
    {
        get => manaregenerationBase;
        set => SetBaseStat(ref manaregenerationBase, value, CombatStat.Manaregeneration);
    }

    [Export]
    public int LightRadiusBase
    {
        get => lightRadiusBase;
        set => SetBaseStat(ref lightRadiusBase, value, CombatStat.LightRadius);
    }

    public int   ManaBase              => (int)Stats.GetEffectiveBase(CombatStat.Mana);
    public float ManaMaximum           => Stats.GetFinalWhole(CombatStat.Mana);
    public float ManaregenerationFinal => Stats.GetFinal(CombatStat.Manaregeneration);
    public float LightRadiusFinal      => Stats.GetFinal(CombatStat.LightRadius);

    public long XpTotal
    {
        get => xpTotal;
        private set => SetField(ref xpTotal, value);
    }

    public long XpForNextLevel                { get; private set; }
    public int  Level                         { get; private set; } = 1;
    public long XpDelta                       => XpTotal - XpFloorCurrentLevel;
    public long XpFloorCurrentLevel           => XpTable.GetTotalXpNeededForLevel(Level);
    public int  AttributePointsAllowedToSpend { get; set; }

    [Export]
    public float ManaCurrent
    {
        get => manaCurrent;
        set => SetField(ref manaCurrent, Math.Min(value, ManaMaximum));
    }

    public event EquipmentChangedEventHandler EquipmentChanged;
    public event LeveledUpEventHandler        LeveledUp;
    public event RespawnedEventHandler        Respawned;

    public override void _Ready()
    {
        lifeOrb.Init(this, ResourceType.Life);
        manaOrb.Init(this, ResourceType.Mana);

        XpForNextLevel = XpTable.GetTotalXpNeededForLevel(Level + 1);

        base._Ready();

        ManaCurrent   = ManaMaximum;
        spawnPosition = GlobalPosition;

        LoadLights();
        ConfigureSkillbar();

        AnimationTree   = GetNode<AnimationTree>(nameof(AnimationTree));
        animationPlayer = GetNode<AnimationPlayer>(nameof(AnimationPlayer));
        levelUpEffect   = GetNode<LevelUpEffect>(nameof(LevelUpEffect));
        //AnimationTree = GetNode<AnimationTree>("AnimationTreeNEW");

        //Erst jetzt ist das Leben gefüllt, vorher stünde es auf 0 und der Spieler gälte als tot
        PropertyChanged += OnPropertyChanged;
    }

    protected override void OnStatsRecalculated()
    {
        if (ManaCurrent > ManaMaximum)
            ManaCurrent = ManaMaximum;

        ApplyLightRadius();

        OnPropertyChanged(nameof(ManaMaximum));
    }

    private void LoadLights()
    {
        foreach (var light in this.GetAllChildren<PointLight2D>())
            lightBaseScales[light] = light.TextureScale;

        ApplyLightRadius();
    }

    private void ApplyLightRadius()
    {
        var factor = LightRadiusFinal / 100f;

        foreach (var (light, baseScale) in lightBaseScales)
            light.TextureScale = baseScale * factor;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(XpTotal) when Level < XpTable.MaxLevel && XpTotal >= XpForNextLevel:
                LevelUp();

                break;
            case nameof(LifeCurrent) when IsDead && !hasDied:
                Die();

                break;
        }
    }

    public void GainExperience(int experienceGained)
        => XpTotal += experienceGained;

    public void LoseExperience(long experienceLost)
    {
        var xpNeededForCurrentLevel = XpTable.GetTotalXpNeededForLevel(Level);
        var totalXpDelta            = XpTotal - experienceLost;

        XpTotal = Math.Max(xpNeededForCurrentLevel, totalXpDelta);
    }

    public void LevelUp()
    {
        Level++;
        AttributePointsAllowedToSpend++;

        XpForNextLevel = XpTable.GetTotalXpNeededForLevel(Level + 1);

        levelUpEffect.Emit();

        LeveledUp?.Invoke(this);
    }

    public int GetRequiredAttributevalue(Requirement requirement)
        => requirement switch
        {
            Requirement.Strength       => StrengthFinal,
            Requirement.Dexterity      => DexterityFinal,
            Requirement.Intelligence   => IntelligenceFinal,
            Requirement.Constitution   => ConstitutionFinal,
            Requirement.Awareness      => AwarenessFinal,
            Requirement.CharacterLevel => Level,
            _                          => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null)
        };

    private void ConfigureSkillbar()
    {
        AssignStartingSkills();

        var skillBarView = new SkillBarView();

        SkillBar.AddChild(skillBarView);
        skillBarView.Bind(this);

        AnchorSkillbar();
    }

    private void AssignStartingSkills()
    {
        var startingSlots = SkillLibrary.LoadStartingLoadout()?.Slots;

        if (startingSlots is null)
            return;

        for (var slot = 0; slot < Math.Min(startingSlots.Count, Loadout.SlotCount); slot++)
            Loadout.Assign(slot, startingSlots[slot]?.Id);
    }

    private void AnchorSkillbar()
    {
        SkillBar.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);

        SkillBar.GrowHorizontal = Control.GrowDirection.Both;
        SkillBar.GrowVertical   = Control.GrowDirection.Begin;
        SkillBar.OffsetLeft     = 0;
        SkillBar.OffsetRight    = 0;
        SkillBar.OffsetTop      = -SkillBarMarginPx;
        SkillBar.OffsetBottom   = -SkillBarMarginPx;
    }

    public bool IsInAggroRangeOf(BaseEnemy enemy)
    {
        var distanceToEnemy = Math.Sqrt(GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition));

        return distanceToEnemy <= enemy.AggroRange;
    }

    public override void _Process(double delta)
        => UpdateHoveredUnit();

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDead)
            return;

        invulnerableTimeLeftSec = Math.Max(0, invulnerableTimeLeftSec - delta);

        ResolveManareg(delta);
        RepeatHeldSkill();
        AdvanceAttack(delta);
        Move(GetWantedDirection(), delta);
    }

    private void ResolveManareg(double delta)
    {
        if (ManaCurrent < ManaMaximum)
        {
            ManaCurrent += ManaregenerationFinal * (float)delta;
            ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
        }
    }

    public override void SpendMana(float amount)
        => ManaCurrent -= amount;

    public void PlayOutOfMana()
    {
        if (!NoManaSound.IsPlaying())
            NoManaSound.Play();
    }

    #region Skills

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDead)
            return;

        for (var slot = 0; slot < InputActions.SkillSlots.Length; slot++)
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
        var skill = SkillLibrary.Find(Loadout.GetSkillId(slot));

        if (skill is null)
            return false;

        var aimPoint = GetGlobalMousePosition();

        return UseSkill(skill, new SkillAim(aimPoint, FindHostileUnitAt(aimPoint)), isRepeat);
    }

    public bool UseSkill(SkillResource skill, SkillAim aim, bool isRepeat = false)
    {
        if (IsDead || skill is null)
            return false;

        return skill.Kind == SkillKind.Attack ? OrderAttack(skill, aim, isRepeat) : CastSpell(skill, aim, isRepeat);
    }

    private bool CastSpell(SkillResource skill, SkillAim aim, bool isRepeat)
    {
        if (!Report(TryPayFor(skill, CombatRules.MinSpellCooldownSec), isRepeat))
            return false;

        if (RollActionFailure())
        {
            this.ShowCombatText("Failed", Colors.Yellow, 28);

            return true;
        }

        SkillExecutor.Execute(this, skill, aim);

        return true;
    }

    private bool OrderAttack(SkillResource skill, SkillAim aim, bool isRepeat)
    {
        if (!aim.HasTarget && IsMelee(skill))
            return false;

        if (aim.HasTarget && !IsHostileTo(aim.Target))
            return false;

        if (!CanPayFor(skill, isRepeat))
            return false;

        orderedSkill     = skill;
        attackTarget     = aim.HasTarget ? aim.Target : null;
        attackAimPoint   = aim.Point;
        approachStuckSec = 0;

        return true;
    }

    private bool IsMelee(SkillResource skill)
        => skill.Delivery == SkillDelivery.Weapon && !Weapon.IsRanged;

    private bool CanPayFor(SkillResource skill, bool isQuiet)
        => Report(SkillGate.Check(skill.Definition, SkillCooldowns, AvailableMana), isQuiet);

    //Bei gehaltener Taste bleibt der Ton für fehlendes Mana aus, sonst liefe er in Dauerschleife
    private bool Report(SkillUseCheck check, bool isQuiet)
    {
        if (check == SkillUseCheck.NotEnoughMana && !isQuiet)
            PlayOutOfMana();

        return check == SkillUseCheck.Ready;
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

    private void UpdateHoveredUnit()
    {
        var unitUnderMouse = IsDead ? null : FindHostileUnitAt(GetGlobalMousePosition());

        if (unitUnderMouse == hoveredUnit)
            return;

        if (IsInstanceValid(hoveredUnit))
            hoveredUnit.SetHighlight(false);

        hoveredUnit = unitUnderMouse;
        hoveredUnit?.SetHighlight(true);
    }

    private void AdvanceAttack(double delta)
    {
        var wasSwinging = !attackCycle.IsReady;

        if (attackCycle.Advance(delta))
            Strike();

        if (wasSwinging && attackCycle.IsReady)
            FinishSwing();
    }

    private Vector2 GetWantedDirection()
    {
        var inputDirection = Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown);

        if (inputDirection != Vector2.Zero)
        {
            CancelAttack();

            return inputDirection;
        }

        if (orderedSkill is null || !attackCycle.IsReady)
            return Vector2.Zero;

        if (attackTarget is null)
        {
            StartSwing(attackAimPoint - BodyCenter);

            return Vector2.Zero;
        }

        if (!IsValidTarget(attackTarget))
        {
            ClearOrder();

            return Vector2.Zero;
        }

        var toTarget = attackTarget.BodyCenter - BodyCenter;

        if (toTarget.Length() > GetEngageRange(orderedSkill.Definition))
            return toTarget.Normalized();

        StartSwing(toTarget);

        return Vector2.Zero;
    }

    private float GetEngageRange(SkillDefinition skill)
        => skill.Delivery switch
        {
            SkillDelivery.Weapon           => Weapon.Range,
            SkillDelivery.Projectile       => skill.Projectile.Reach * EngageFraction,
            SkillDelivery.AreaAroundCaster => skill.Area.Radius * EngageFraction,
            _                              => float.MaxValue
        };

    private void Move(Vector2 direction, double delta)
    {
        MovementDirection = direction;
        Velocity          = direction * MovementspeedFinal;

        if (direction != Vector2.Zero)
            Face(direction);

        MoveAndSlide();

        GiveUpTargetWhenStuck(direction, delta);
    }

    private void GiveUpTargetWhenStuck(Vector2 direction, double delta)
    {
        var isApproaching = attackTarget is not null && direction != Vector2.Zero;

        if (!isApproaching || GetRealVelocity().Length() > MovementspeedFinal * StuckSpeedFraction)
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

    private void Face(Vector2 direction)
    {
        var blendPosition = direction * new Vector2(1, -1);

        AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", blendPosition);
        AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", blendPosition);
    }

    //Bezahlt wird erst hier, denn auf dem Weg zum Ziel kann das Mana ausgegangen sein
    private void StartSwing(Vector2 toTarget)
    {
        if (!Report(TryPayFor(orderedSkill), true))
        {
            ClearOrder();

            return;
        }

        var direction = toTarget == Vector2.Zero ? Vector2.Down : toTarget.Normalized();
        var swingSec  = 1.0 / Math.Max(CombatRules.MinAttacksPerSecond, AttacksPerSecondFinal);

        swingSkill  = orderedSkill;
        swingFailed = RollActionFailure();

        attackCycle.Start(swingSec * ImpactFraction, swingSec * (1 - ImpactFraction));

        Face(direction);
        PlaySwingAnimation(direction, swingSec);
    }

    private void Strike()
    {
        if (swingFailed)
        {
            this.ShowCombatText("Failed", Colors.Yellow, 28);

            return;
        }

        if (swingSkill is not null)
            SkillExecutor.Execute(this, swingSkill, new SkillAim(attackAimPoint, attackTarget));
    }

    private void FinishSwing()
    {
        EndSwingAnimation();

        var previousTarget = attackTarget;

        swingSkill = null;

        ClearOrder();

        if (heldSlot != NoSlot)
            ContinueHeldAttack(previousTarget);
    }

    private void ContinueHeldAttack(BaseUnit previousTarget)
    {
        var skill = SkillLibrary.Find(Loadout.GetSkillId(heldSlot));

        if (skill is null || skill.Kind != SkillKind.Attack)
            return;

        var aimPoint = GetGlobalMousePosition();
        var target   = FindHostileUnitAt(aimPoint);

        if (target is null && IsMelee(skill) && IsValidTarget(previousTarget))
            target = previousTarget;

        OrderAttack(skill, new SkillAim(aimPoint, target), true);
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

        if (SkillLibrary.Find(Loadout.GetSkillId(heldSlot))?.Kind == SkillKind.Attack)
            heldSlot = NoSlot;

        attackCycle.CancelWindup();

        EndSwingAnimation();
    }

    private static bool IsValidTarget(BaseUnit unit)
        => IsInstanceValid(unit) && unit.IsTargetable;

    //Die Angriffsanimation läuft direkt über den AnimationPlayer, damit ihr Tempo zum Schwung passt
    private void PlaySwingAnimation(Vector2 direction, double swingSec)
    {
        var octant        = Mathf.PosMod(Mathf.RoundToInt(direction.Angle() / (Mathf.Pi / 4)), CompassSuffixes.Length);
        var animationName = $"attack/1h_{CompassSuffixes[octant]}";

        if (AttackSprite is null || !animationPlayer.HasAnimation(animationName))
            return;

        isSwingAnimationRunning = true;
        AnimationTree.Active    = false;
        AttackSprite.Visible    = true;

        //Die Lichter hängen am RunSprite, deshalb wird er durchsichtig statt unsichtbar
        RunSprite.SelfModulate = Colors.Transparent;

        var animationLength = animationPlayer.GetAnimation(animationName).Length;

        animationPlayer.Play(animationName, customSpeed: (float)(animationLength / swingSec));
    }

    private void EndSwingAnimation()
    {
        if (!isSwingAnimationRunning)
            return;

        isSwingAnimationRunning = false;

        animationPlayer.Stop();

        AttackSprite.Visible   = false;
        RunSprite.SelfModulate = Colors.White;
        AnimationTree.Active   = true;
    }

    #endregion

    #region Tod und Respawn

    public override void ReceiveDamage(HitResult hit)
    {
        if (invulnerableTimeLeftSec > 0)
            return;

        base.ReceiveDamage(hit);
    }

    private void Die()
    {
        hasDied = true;

        CancelAttack();
        attackCycle.Reset();
        StatusEffects.Clear();

        heldSlot = NoSlot;

        Velocity          = Vector2.Zero;
        MovementDirection = Vector2.Zero;

        LastXpLoss = DeathPenalty.GetXpLoss(XpTotal, XpFloorCurrentLevel, XpForNextLevel);
        LoseExperience(LastXpLoss);

        RunSprite.SelfModulate = DeathTint;

        RaiseDied();
    }

    public void Respawn()
    {
        if (!hasDied)
            return;

        GlobalPosition          = spawnPosition;
        Velocity                = Vector2.Zero;
        hasDied                 = false;
        LifeCurrent             = LifeMaximum;
        ManaCurrent             = ManaMaximum;
        invulnerableTimeLeftSec = RespawnInvulnerabilitySec;
        RunSprite.SelfModulate  = Colors.White;

        Respawned?.Invoke();
    }

    #endregion

    #region Ausrüstung

    public void EquipItem(BaseItem item)
    {
        Stats.Update(sheet =>
        {
            if (item is BaseArmor armor)
                sheet.AddModifier(item.CreateCombatStatModifier(CombatStat.Armor, ModificationType.Flat, armor.ArmorvalueFinal));

            foreach (var modifier in item.GetExtrinsicModifiers())
                sheet.AddModifier(item.CreateCombatStatModifier(modifier));

            if (item is BaseWeapon weapon)
                WieldWeapon(weapon);
        });

        EquipmentChanged?.Invoke();
    }

    public void UnequipItem(BaseItem item)
    {
        Stats.Update(sheet =>
        {
            sheet.RemoveModifiersOf(item.ToString());

            if (item == equippedWeapon)
                WieldWeapon(null);
        });

        EquipmentChanged?.Invoke();
    }

    private void WieldWeapon(BaseWeapon newWeapon)
    {
        equippedWeapon = newWeapon;
        weapon         = newWeapon?.ToProfile() ?? WeaponProfile.Unarmed;

        weapon.ApplyTo(Stats);
    }

    #endregion
}
