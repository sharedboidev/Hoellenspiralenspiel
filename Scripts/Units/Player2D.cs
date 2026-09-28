using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Progression;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;
using ResourceOrb = Hoellenspiralenspiel.Scripts.UI.Character.ResourceOrb;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : BaseUnit
{
    public delegate void EquipmentChangedEventHandler();

    public delegate void LeveledUpEventHandler(Player2D player);

    public delegate void RespawnedEventHandler();

    //Wer auf den nächsten Klick der linken Maustaste wartet, trägt sich hier ein. Solange greift der Spieler nicht an
    public const string PrimaryClickReservedGroup = "awaits_primary_click";

    //Anteil des Schwungs, nach dem der Treffer fällt
    private const float ImpactFraction = 0.5f;

    //Der Treffer landet noch, wenn das Ziel während des Ausholens ein Stück aus der Reichweite gerückt ist
    private const float  RangeTolerance      = 1.25f;
    private const float  MinAttacksPerSecond = 0.1f;
    private const double StuckTimeoutSec     = 0.4;
    private const float  StuckSpeedFraction  = 0.1f;

    private static readonly string[] CompassSuffixes = ["e", "se", "s", "sw", "w", "nw", "n", "ne"];
    private static readonly Color    DeathTint       = new(0.6f, 0.1f, 0.1f, 0.7f);

    private readonly AttackCycle attackCycle = new();

    //Lichter des Spielers mit der Größe, die in der Szene eingestellt ist
    private readonly Dictionary<PointLight2D, float> lightBaseScales = new();
    private readonly PackedScene                     skillBarIcon    = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
    private readonly List<BaseSkill>                 skills          = new();
    private          AnimationPlayer                 animationPlayer;
    private          double                          approachStuckSec;
    private          BaseUnit                        attackTarget;
    private          BaseWeapon                      equippedWeapon;
    private          bool                            hasDied;
    private          BaseUnit                        hoveredUnit;
    private          double                          invulnerableTimeLeftSec;
    private          bool                            isAttackHeld;
    private          bool                            isSwingAnimationRunning;
    private          LevelUpEffect                   levelUpEffect;
    [Export] private ResourceOrb                     lifeOrb;
    private          int                             lightRadiusBase = 100;
    private          float                           manaCurrent;
    [Export] private ResourceOrb                     manaOrb;
    private          float                           manaregenerationBase = .5f;
    [Export] public  HBoxContainer                   SkillBar;
    private          Vector2                         spawnPosition;
    private          bool                            swingFailed;
    private          long                            xpTotal;

    public Player2D()
    {
        Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Manaregeneration, manaregenerationBase);
            sheet.SetBase(CombatStat.LightRadius, lightRadiusBase);
        });

        Weapon.ApplyTo(Stats);
    }

    public override Faction Faction => Faction.Player;

    //Der Spieler ist größer als die Gegner, seine Zahlen erscheinen über dem Kopf
    public override Vector2 CombatTextOffset => new(0, -170);

    //Die Werte, mit denen der Spieler angreift. Ohne angelegte Waffe kämpft er mit bloßen Händen
    public WeaponProfile Weapon { get; private set; } = WeaponProfile.Unarmed;

    //XP, die der letzte Tod gekostet hat
    public long LastXpLoss { get; private set; }

    private AnimationTree AnimationTree { get; set; }

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    //Zeit nach dem Respawn, in der der Spieler keinen Schaden nimmt
    [Export]
    public float RespawnInvulnerabilitySec { get; set; } = 2f;

    //Mana pro Sekunde
    [Export]
    public float ManaregenerationBase
    {
        get => manaregenerationBase;
        set => SetBaseStat(ref manaregenerationBase, value, CombatStat.Manaregeneration);
    }

    //Lichtradius in Prozent. 100 entspricht der Größe, die in der Szene eingestellt ist
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

    //Der Lichtradius skaliert die Lichter relativ zu ihrer Größe aus der Szene
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
        AddSkillsToBar();
        SetSkillbarposition();
    }

    private void SetSkillbarposition()
    {
        var viewportSize     = GetViewportRect().Size;
        var skillbarSize     = SkillBar.Size;
        var skillbarPosition = new Vector2((viewportSize.X - skillbarSize.X) / 2, viewportSize.Y - 2 * skillbarSize.Y);

        SkillBar.Position = skillbarPosition;
    }

    private void AddSkillsToBar()
    {
        skills.Add(new FireballSkill(this));
        skills.Add(new FrostNovaSkill(this));
        skills.Add(new LightningStrikeSkill(this));

        var fireballActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        fireballActionBarItem.Init(skills.ElementAt(0), "res://Scenes/Spells/fireball.tscn", Key.F);

        var frostNovaActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        frostNovaActionBarItem.Init(skills.ElementAt(1), "res://Scenes/Spells/frost_nova.tscn", Key.E);

        var lightningStrikeActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        lightningStrikeActionBarItem.Init(skills.ElementAt(2), "res://Scenes/Spells/very_cool_circle.tscn", Key.R);

        SkillBar.AddChild(fireballActionBarItem);
        SkillBar.AddChild(frostNovaActionBarItem);
        SkillBar.AddChild(lightningStrikeActionBarItem);
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
        AdvanceAttack(delta);
        Move(GetWantedDirection(), delta);
    }

    //Die Orbs hören auf die Änderung von ManaCurrent und LifeCurrent und müssen nicht eigens angestoßen werden
    private void ResolveManareg(double delta)
    {
        if (ManaCurrent < ManaMaximum)
        {
            ManaCurrent += ManaregenerationFinal * (float)delta;
            ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
        }
    }

    public bool CanUseAbility(float manaCost)
        => ManaCurrent >= manaCost;

    public void PlayOutOfMana()
    {
        if (!NoManaSound.IsPlaying())
            NoManaSound.Play();
    }

    public void ReduceMana(float mana)
        => ManaCurrent -= mana;

    #region Angriff

    //Der Standardangriff liegt vorerst fest auf der linken Maustaste und löst nur bei einem Klick auf einen Gegner aus
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputActions.PrimaryAction) || IsDead || IsPrimaryClickReserved())
            return;

        var clickedUnit = FindHostileUnitAt(GetGlobalMousePosition());

        if (clickedUnit is null)
            return;

        OrderAttack(clickedUnit);

        GetViewport().SetInputAsHandled();
    }

    //Der Spieler läuft zum Ziel und greift an, sobald es in Reichweite ist. Ein neues Ziel ersetzt das alte
    public void OrderAttack(BaseUnit target)
    {
        if (IsDead || !IsValidTarget(target) || !IsHostileTo(target))
            return;

        attackTarget     = target;
        isAttackHeld     = true;
        approachStuckSec = 0;
    }

    private bool IsPrimaryClickReserved()
        => GetTree().GetNodesInGroup(PrimaryClickReservedGroup).Count > 0;

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

    //Liefert die Richtung, in die sich der Spieler bewegen will, und beginnt den Schwung, sobald das Ziel in Reichweite ist
    private Vector2 GetWantedDirection()
    {
        if (!Input.IsActionPressed(InputActions.PrimaryAction))
            isAttackHeld = false;

        var inputDirection = Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown);

        //Bewegung per Tastatur übersteuert Hinlaufen und Ausholen
        if (inputDirection != Vector2.Zero)
        {
            CancelAttack();

            return inputDirection;
        }

        if (!IsValidTarget(attackTarget))
            attackTarget = null;

        if (!attackCycle.IsReady)
            return Vector2.Zero;

        //Bei gehaltener Maustaste geht es mit dem Gegner unter dem Mauszeiger weiter
        if (attackTarget is null && isAttackHeld)
            attackTarget = FindHostileUnitAt(GetGlobalMousePosition());

        if (attackTarget is null)
            return Vector2.Zero;

        var toTarget = attackTarget.BodyCenter - BodyCenter;

        if (toTarget.Length() > Weapon.Range)
            return toTarget.Normalized();

        StartSwing(toTarget);

        return Vector2.Zero;
    }

    private void Move(Vector2 direction, double delta)
    {
        MovementDirection = direction;
        Velocity          = direction * MovementspeedFinal;

        if (direction != Vector2.Zero)
            Face(direction);

        MoveAndSlide();

        GiveUpTargetWhenStuck(direction, delta);
    }

    //Ohne Wegfindung bleibt der Spieler an Wänden hängen. Kommt er nicht voran, gibt er das Ziel auf
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

        attackTarget     = null;
        isAttackHeld     = false;
        approachStuckSec = 0;
    }

    private void Face(Vector2 direction)
    {
        var blendPosition = direction * new Vector2(1, -1);

        AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", blendPosition);
        AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", blendPosition);
    }

    private void StartSwing(Vector2 toTarget)
    {
        var direction = toTarget == Vector2.Zero ? Vector2.Down : toTarget.Normalized();
        var swingSec  = 1.0 / Math.Max(MinAttacksPerSecond, AttacksPerSecondFinal);

        swingFailed = RollActionFailure();

        attackCycle.Start(swingSec * ImpactFraction, swingSec * (1 - ImpactFraction));

        Face(direction);
        PlaySwingAnimation(direction, swingSec);
    }

    //Der Standardangriff ist eine ATTACK mit 100 % Waffenschaden
    private void Strike()
    {
        if (swingFailed)
        {
            this.ShowCombatText("Failed", Colors.Yellow, 28);

            return;
        }

        if (!IsValidTarget(attackTarget) || DistanceTo(attackTarget) > Weapon.Range * RangeTolerance)
            return;

        var request = HitRequests.ForAttack(Stats, Weapon, AttackDefinition.Standard);

        attackTarget.ReceiveDamage(HitResolver.Resolve(request, attackTarget.Stats, GameRandom.Shared));
    }

    private void FinishSwing()
    {
        EndSwingAnimation();

        if (!isAttackHeld)
        {
            attackTarget = null;

            return;
        }

        var unitUnderMouse = FindHostileUnitAt(GetGlobalMousePosition());

        if (unitUnderMouse is not null)
            attackTarget = unitUnderMouse;
    }

    private void CancelAttack()
    {
        attackTarget = null;
        isAttackHeld = false;

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

        Velocity          = Vector2.Zero;
        MovementDirection = Vector2.Zero;

        LastXpLoss = DeathPenalty.GetXpLoss(XpTotal, XpFloorCurrentLevel, XpForNextLevel);
        LoseExperience(LastXpLoss);

        RunSprite.SelfModulate = DeathTint;

        RaiseDied();
    }

    //Zurück an den Startpunkt des Levels, mit vollem Leben und Mana
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

    //Alle Modifier eines Items tragen dessen Herkunft, damit sie beim Ablegen gemeinsam entfernt werden
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

    //Angriffstempo und Krit-Chance der Waffe werden zu Grundwerten im Stat-Blatt
    private void WieldWeapon(BaseWeapon weapon)
    {
        equippedWeapon = weapon;
        Weapon         = weapon?.ToProfile() ?? WeaponProfile.Unarmed;

        Weapon.ApplyTo(Stats);
    }

    #endregion
}
