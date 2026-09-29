using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Progression;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Hero
        : BaseUnit,
          IHero
{
    public const int InventoryWidth  = 14;
    public const int InventoryHeight = 5;

    //Auf dieser Ebene liegt der Körper des Helden samt Ausrüstung. Sein eigenes Licht wirft davon keinen Schatten,
    //dafür steht er in jedem Look auf seiner Scheibe. Deshalb gehört sein BlobShadow nicht zur Gruppe blob_shadows
    public const uint BodyLayer = 1u << 18;

    private const float  ImpactFraction     = 0.5f;
    private const float  EngageFraction     = 0.9f;
    private const int    NoSlot             = -1;
    private const double StuckTimeoutSec    = 0.4;
    private const float  StuckSpeedFraction = 0.1f;
    private const float  PickSearchPx       = 600f;
    private const float  SwingArcDegrees    = 70f;
    private const float  SwingRaiseDegrees  = 40f;
    private const float  AimRaiseDegrees    = 80f;

    private static readonly List<BaseUnit> UnitsNearPoint = new();

    private readonly AttackCycle   attackCycle = new();
    private          PathFollower  approachPath;
    private          double        approachStuckSec;
    private          Vector3       attackAimPoint;
    private          BaseUnit      attackTarget;
    private          ItemInstance  equippedWeapon;
    private          bool          hasDied;
    private          int           heldSlot = NoSlot;
    private          BaseUnit      hoveredEnemy;
    private          IUsable       hoveredUsable;
    private          double        invulnerableTimeLeftSec;
    private          LevelUpEffect levelUpEffect;
    private          OmniLight3D   light;
    private          float         lightBaseRange;
    private          float         manaCurrent;
    private          SkillResource orderedSkill;
    private readonly HeroProgress  progress = new();
    private          Vector3       spawnPosition;
    private          bool          swingFailed;
    private          Tween         swingLook;
    private          SkillResource swingSkill;
    private          IUsable       useTarget;
    private          WeaponProfile weapon = WeaponProfile.Unarmed;
    private          Node3D        weaponPivot;
    private          WornItems     wornItems;

    public Hero()
    {
        weapon.ApplyTo(Stats);

        Items = new CharacterItems(InventoryWidth, InventoryHeight, GetRequiredValue);

        Items.Equipment.Equipped   += OnItemEquipped;
        Items.Equipment.Unequipped += OnItemUnequipped;
        Items.Dropped              += OnItemDropped;

        LifeChanged        += _ => ResourcesChanged?.Invoke();
        progress.Changed   += () => XpChanged?.Invoke();
        progress.LeveledUp += OnLeveledUp;
    }

    public CharacterItems Items { get; }

    public string CharacterName { get; set; } = string.Empty;

    public SkillLoadout Loadout { get; } = new(InputActions.SkillSlots.Length);

    //Bis entschieden ist, wie der Held Skills bekommt, kennt er alle
    public IReadOnlyList<SkillResource> KnownSkills => SkillLibrary.PlayerSkills;

    public int  Level               => progress.Level;
    public long XpTotal             => progress.XpTotal;
    public long XpFloorCurrentLevel => progress.XpFloor;
    public long XpForNextLevel      => progress.XpForNextLevel;
    public int  AttributePoints     => progress.AttributePoints;

    public long LastXpLoss { get; private set; }

    public WornItems WornItems => wornItems;

    //Ein neuer Charakter trägt diese Items am Körper, ohne Affixe
    [Export]
    public Array<ItemBaseResource> StartingEquipment { get; set; } = new();

    //Ein neuer Charakter hat diese Items im Inventar
    [Export]
    public Array<ItemBaseResource> StartingItems { get; set; } = new();

    [Export]
    public AudioStreamPlayer NoManaSound { get; set; }

    [Export]
    public int LifeBonus { get; set; } = 50;

    [Export]
    public float Movementspeed { get; set; } = 1000f;

    [Export]
    public float RespawnInvulnerabilitySec { get; set; } = 2f;

    [ExportGroup("Attributes")]
    [Export]
    public int Strength { get; set; } = 1;

    [Export]
    public int Dexterity { get; set; } = 1;

    [Export]
    public int Intelligence { get; set; } = 1;

    [Export]
    public int Constitution { get; set; } = 1;

    [Export]
    public int Awareness { get; set; } = 1;

    public override Faction Faction => Faction.Player;

    public override WeaponProfile Weapon => weapon;

    public override PackedScene WeaponProjectileScene => ItemLibrary.GetProjectileScene(equippedWeapon);

    public override float AvailableMana => ManaCurrent;

    public override float CombatTextHeight => 2.1f;

    public float LightRadiusMeters => light?.OmniRange ?? 0f;

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

            ResourcesChanged?.Invoke();
        }
    }

    public event Action SheetChanged;
    public event Action ResourcesChanged;
    public event Action XpChanged;
    public event Action LeveledUp;
    public event Action Respawned;

    public override void _Ready()
    {
        base._Ready();

        ManaCurrent   = ManaMaximum;
        spawnPosition = GlobalPosition;
        approachPath  = new PathFollower(this);
        weaponPivot   = Visual?.GetNodeOrNull<Node3D>("WeaponPivot");
        light         = GetNodeOrNull<OmniLight3D>("Light");
        levelUpEffect = GetNodeOrNull<LevelUpEffect>("LevelUpEffect");

        if (light is not null)
        {
            lightBaseRange          =  light.OmniRange;
            light.ShadowCasterMask &= ~BodyLayer;
        }

        ApplyLightRadius();
        AssignStartingSkills();

        if (Visual is not null)
        {
            wornItems = new WornItems(Visual);

            wornItems.ShowAll(Items.Equipment);

            MarkBody(Visual);

            GetTree().NodeAdded += OnNodeAdded;
        }

        LifeChanged += OnLifeChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (Visual is not null)
            GetTree().NodeAdded -= OnNodeAdded;
    }

    //Angelegte Ausrüstung kommt später dazu und gehört ebenso zum Körper
    private void OnNodeAdded(Node node)
    {
        if (Visual.IsAncestorOf(node))
            MarkBody(node);
    }

    //Nur auf dieser Ebene: Ein Licht wirft den Schatten eines Meshs, sobald eine seiner Ebenen in der Maske steht
    private static void MarkBody(Node node)
    {
        if (node is GeometryInstance3D geometry)
            geometry.Layers = BodyLayer;

        foreach (var child in node.GetChildren())
            MarkBody(child);
    }

    protected override void ApplyBaseValues(StatSheet sheet)
    {
        sheet.SetBase(CombatStat.Strength, Strength);
        sheet.SetBase(CombatStat.Dexterity, Dexterity);
        sheet.SetBase(CombatStat.Intelligence, Intelligence);
        sheet.SetBase(CombatStat.Constitution, Constitution);
        sheet.SetBase(CombatStat.Awareness, Awareness);
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

        SheetChanged?.Invoke();
    }

    private void ApplyLightRadius()
    {
        if (light is not null)
            light.OmniRange = lightBaseRange * Stats.GetFinal(CombatStat.LightRadius) / 100f;
    }

    public override void _Process(double delta)
        => WallFade.Update(GlobalPosition, PickHeight, LightRadiusMeters, GetViewport().GetCamera3D()?.GlobalPosition ?? GlobalPosition);

    //Hier steht der Held nach dem Betreten einer Ebene und nach dem Tod
    public void MoveToLevelStart(Vector3 position)
    {
        Teleport(position);

        spawnPosition = position;
    }

    public void Teleport(Vector3 position)
    {
        CancelAttack();

        useTarget      = null;
        heldSlot       = NoSlot;
        Velocity       = Vector3.Zero;
        GlobalPosition = position;

        approachPath.Reset();
    }

    private void OnLifeChanged(BaseUnit unit)
    {
        if (IsDead && !hasDied)
            Die();
    }

    private void AssignStartingSkills()
    {
        var startingSlots = SkillLibrary.LoadStartingLoadout()?.Slots;

        if (startingSlots is null)
            return;

        for (var slot = 0; slot < Math.Min(startingSlots.Count, Loadout.SlotCount); slot++)
            Loadout.Assign(slot, startingSlots[slot]?.Id);
    }

    public void GainExperience(int experienceGained)
        => progress.Gain(experienceGained);

    private void OnLeveledUp()
    {
        levelUpEffect?.Emit();

        SheetChanged?.Invoke();
        LeveledUp?.Invoke();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDead)
            return;

        invulnerableTimeLeftSec = Math.Max(0, invulnerableTimeLeftSec - delta);

        RegenerateMana(delta);
        UpdateHoveredUsable();
        UpdateHoveredEnemy();
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

    public BaseUnit FindHostileUnitUnderMouse()
    {
        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return null;

        var mouse = GetViewport().GetMousePosition();

        return FindHostileUnitOnRay(camera.ProjectRayOrigin(mouse), camera.ProjectRayNormal(mouse));
    }

    //Der Kopf eines Gegners liegt auf dem Bildschirm über seinen Füßen, der Bodenpunkt unter der Maus also hinter ihm
    public BaseUnit FindHostileUnitOnRay(Vector3 rayOrigin, Vector3 rayNormal)
    {
        BaseUnit nearestUnit  = null;
        var    nearestDepth = float.MaxValue;
        var    groundPoint  = Mathf.IsZeroApprox(rayNormal.Y) ? rayOrigin : rayOrigin - rayNormal * (rayOrigin.Y / rayNormal.Y);

        UnitRegistry.FindNear(groundPoint, PickSearchPx, UnitsNearPoint);

        foreach (var unit in UnitsNearPoint)
        {
            if (!IsHostileTo(unit) || !unit.IsTargetable || !unit.IsSeen)
                continue;

            var depth = unit.GetPickDepth(rayOrigin, rayNormal);

            if (depth < 0f || depth >= nearestDepth || WallFade.IsHidden(this, rayOrigin, rayOrigin + rayNormal * depth))
                continue;

            nearestUnit  = unit;
            nearestDepth = depth;
        }

        return nearestUnit;
    }

    #endregion

    #region Skills

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDead)
            return;

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && OrderUse(FindUsableUnderMouse()))
        {
            GetViewport().SetInputAsHandled();

            return;
        }

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
        var skill = GetSkill(slot);

        return skill is not null && UseSkill(skill, new SkillAim(GetMouseGroundPoint(), FindHostileUnitUnderMouse()), isRepeat);
    }

    public bool UseSkill(SkillResource skill, SkillAim aim, bool isRepeat = false)
    {
        if (IsDead || skill is null)
            return false;

        return skill.Kind == SkillKind.Attack ? OrderAttack(skill, aim, isRepeat) : CastSpell(skill, aim, isRepeat);
    }

    private SkillResource GetSkill(int slot)
        => SkillLibrary.Find(Loadout.GetSkillId(slot));

    private bool CastSpell(SkillResource skill, SkillAim aim, bool isRepeat)
    {
        if (!Report(TryPayFor(skill, CombatRules.MinSpellCooldownSec), isRepeat))
            return false;

        if (RollActionFailure())
        {
            CombatText.Show(this, "Failed", Colors.Yellow, 28);

            return true;
        }

        Face(aim.CurrentPoint - GlobalPosition);

        SkillExecutor.Execute(this, skill, aim);

        return true;
    }

    private bool OrderAttack(SkillResource skill, SkillAim aim, bool isRepeat)
    {
        if (!aim.HasTarget && IsMelee(skill))
            return false;

        if (aim.HasTarget && !IsHostileTo(aim.Target))
            return false;

        if (!Report(SkillGate.Check(skill.Definition, SkillCooldowns, AvailableMana), isRepeat))
            return false;

        var previousTarget = attackTarget;

        useTarget        = null;
        orderedSkill     = skill;
        attackTarget     = aim.HasTarget ? aim.Target : null;
        attackAimPoint   = aim.Point;
        approachStuckSec = 0;

        if (attackTarget != previousTarget)
            approachPath.Reset();

        return true;
    }

    private bool IsMelee(SkillResource skill)
        => skill.Delivery == SkillDelivery.Weapon && !Weapon.IsRanged;

    //Bei gehaltener Taste bleibt der Ton für fehlendes Mana aus, sonst liefe er in Dauerschleife
    private bool Report(SkillUseCheck check, bool isQuiet)
    {
        if (check == SkillUseCheck.NotEnoughMana && !isQuiet && NoManaSound is { Playing: false })
            NoManaSound.Play();

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

        if (useTarget is not null)
            return ApproachUsable(delta);

        if (orderedSkill is null || !attackCycle.IsReady)
            return Vector3.Zero;

        if (attackTarget is null)
        {
            StartSwing(attackAimPoint - GlobalPosition);

            return Vector3.Zero;
        }

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
            SkillDelivery.Weapon           => Weapon.Range,
            SkillDelivery.Projectile       => skill.Projectile.Reach * EngageFraction,
            SkillDelivery.AreaAroundCaster => skill.Area.Radius * EngageFraction,
            _                              => float.MaxValue
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
        var isApproaching = (attackTarget is not null || useTarget is not null) && direction != Vector3.Zero;

        if (!isApproaching || GetRealVelocity().Length() > WorldScale.ToMeters(MovementspeedPx) * StuckSpeedFraction)
        {
            approachStuckSec = 0;

            return;
        }

        approachStuckSec += delta;

        if (approachStuckSec < StuckTimeoutSec)
            return;

        ClearOrder();

        useTarget        = null;
        heldSlot         = NoSlot;
        approachStuckSec = 0;
    }

    //Bezahlt wird erst hier, denn auf dem Weg zum Ziel kann das Mana ausgegangen sein
    private void StartSwing(Vector3 toTarget)
    {
        if (!Report(TryPayFor(orderedSkill), true))
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
            CombatText.Show(this, "Failed", Colors.Yellow, 28);

            return;
        }

        if (swingSkill is not null)
            SkillExecutor.Execute(this, swingSkill, new SkillAim(attackAimPoint, attackTarget));
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

    private void ContinueHeldAttack(BaseUnit previousTarget)
    {
        var skill = GetSkill(heldSlot);

        if (skill is null || skill.Kind != SkillKind.Attack)
            return;

        var target = FindHostileUnitUnderMouse();

        if (target is null && IsMelee(skill) && IsValidTarget(previousTarget))
            target = previousTarget;

        OrderAttack(skill, new SkillAim(GetMouseGroundPoint(), target), true);
    }

    private void ClearOrder()
    {
        orderedSkill = null;
        attackTarget = null;
    }

    private void CancelAttack()
    {
        ClearOrder();

        useTarget = null;
        swingSkill = null;

        if (GetSkill(heldSlot)?.Kind == SkillKind.Attack)
            heldSlot = NoSlot;

        attackCycle.CancelWindup();

        EndSwingLook();
    }

    private static bool IsValidTarget(BaseUnit unit)
        => IsInstanceValid(unit) && unit.IsTargetable;

    private void PlaySwingLook(double swingSec)
    {
        if (weaponPivot is null)
            return;

        swingLook?.Kill();

        swingLook = CreateTween();

        if (Weapon.IsRanged)
        {
            swingLook.TweenProperty(weaponPivot, "rotation_degrees", new Vector3(AimRaiseDegrees, 0, 0), swingSec * ImpactFraction);
        }
        else
        {
            weaponPivot.RotationDegrees = new Vector3(SwingRaiseDegrees, -SwingArcDegrees, 0);

            swingLook.TweenProperty(weaponPivot, "rotation_degrees", new Vector3(SwingRaiseDegrees, SwingArcDegrees, 0), swingSec * ImpactFraction);
        }

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

    #region Items

    public int GetRequiredValue(Requirement requirement)
        => requirement switch
        {
            Requirement.Strength       => Stats.GetFinalWhole(CombatStat.Strength),
            Requirement.Dexterity      => Stats.GetFinalWhole(CombatStat.Dexterity),
            Requirement.Intelligence   => Stats.GetFinalWhole(CombatStat.Intelligence),
            Requirement.Constitution   => Stats.GetFinalWhole(CombatStat.Constitution),
            Requirement.Awareness      => Stats.GetFinalWhole(CombatStat.Awareness),
            Requirement.CharacterLevel => Level,
            _                          => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null)
        };

    public void Consume(ItemInstance item)
    {
        if (IsDead)
            return;

        var effect = Items.Consume(item);

        switch (effect?.Kind)
        {
            case ConsumableEffectKind.RestoreLife:
                var healedAmount = (int)(LifeMaximum * effect.Percent / 100f);

                LifeCurrent += healedAmount;

                CombatText.Show(this, healedAmount.ToString("N0"), Colors.LimeGreen, 36);

                break;
            case ConsumableEffectKind.RestoreMana:
                ManaCurrent += ManaMaximum * effect.Percent / 100f;

                break;
        }
    }

    private void OnItemEquipped(ItemInstance item)
    {
        Stats.Update(sheet =>
        {
            sheet.AddModifiers(item.GetEquipModifiers());

            if (item.Definition.Kind == ItemKind.Weapon)
                WieldWeapon(item);
        });

        wornItems?.Show(item);
    }

    private void OnItemUnequipped(ItemInstance item)
    {
        Stats.Update(sheet =>
        {
            sheet.RemoveModifiersOf(item.InstanceId);

            if (item == equippedWeapon)
                WieldWeapon(null);
        });

        wornItems?.Hide(item);
    }

    private void WieldWeapon(ItemInstance newWeapon)
    {
        equippedWeapon = newWeapon;
        weapon         = newWeapon?.ToWeaponProfile() ?? WeaponProfile.Unarmed;

        weapon.ApplyTo(Stats);
    }

    public void GiveStartingItems()
    {
        foreach (var item in StartingEquipment)
        {
            if (item is not null)
                Wear(new ItemInstance(item.Definition));
        }

        foreach (var item in StartingItems)
        {
            if (item is not null)
                Items.PickUp(new ItemInstance(item.Definition));
        }
    }

    //Was der Held nicht tragen kann, bleibt im Inventar liegen
    private void Wear(ItemInstance item)
    {
        if (Items.PickUp(item) && !Items.EquipFromInventory(item))
            GD.PushWarning($"Der Held erfüllt die Anforderungen von {item.Definition.Name} nicht, das Item liegt im Inventar.");
    }

    public bool OrderPickUp(Lootbag lootbag)
        => OrderUse(lootbag);

    //Was in Reichweite liegt, benutzt der Held sofort, zu allem anderen läuft er erst hin
    public bool OrderUse(IUsable usable)
    {
        if (IsDead || !IsStillThere(usable))
            return false;

        CancelAttack();

        if (usable.IsInReachOf(this))
        {
            usable.Use();

            return true;
        }

        useTarget        = usable;
        approachStuckSec = 0;

        approachPath.Reset();

        return true;
    }

    private static bool IsStillThere(IUsable usable)
        => usable is Node node && IsInstanceValid(node) && !node.IsQueuedForDeletion();

    //Sind die Schilder zu sehen, hebt man Beutel nur über ihr Schild auf, und das Schild hellt den Beutel selbst auf
    private IUsable FindUsableUnderMouse()
        => Usables.FindUnderMouse(this, LootLabels.AreShown);

    private void UpdateHoveredUsable()
    {
        var usableUnderMouse = FindUsableUnderMouse();

        if (usableUnderMouse == hoveredUsable)
            return;

        if (IsStillThere(hoveredUsable))
            hoveredUsable.SetHighlight(false);

        hoveredUsable = usableUnderMouse;
        hoveredUsable?.SetHighlight(true);
    }

    private void UpdateHoveredEnemy()
    {
        var enemyUnderMouse = FindHostileUnitUnderMouse();

        if (enemyUnderMouse == hoveredEnemy)
            return;

        if (IsInstanceValid(hoveredEnemy))
            hoveredEnemy.SetHighlighted(false);

        hoveredEnemy = enemyUnderMouse;
        hoveredEnemy?.SetHighlighted(true);
    }

    private Vector3 ApproachUsable(double delta)
    {
        if (!IsStillThere(useTarget))
        {
            useTarget = null;

            return Vector3.Zero;
        }

        if (!useTarget.IsInReachOf(this))
            return approachPath.GetDirectionTo(useTarget.GlobalPosition, delta);

        var reached = useTarget;

        useTarget = null;

        reached.Use();

        return Vector3.Zero;
    }

    private void OnItemDropped(ItemInstance item)
    {
        if (!IsInsideTree())
            return;

        Lootbag.DropAround(this, item, Items);
    }

    #endregion

    #region Tod und Respawn

    public override void ReceiveDamage(HitResult hit, BaseUnit attacker = null)
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

        LastXpLoss = progress.LoseForDeath();

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

        Respawned?.Invoke();
    }

    #endregion

    #region Fortschritt

    public bool RaiseAttribute(Attributes attribute)
    {
        if (!progress.SpendAttributePoint())
            return false;

        var stat = GetStatOf(attribute);

        Stats.SetBase(stat, Stats.GetBase(stat) + 1);

        return true;
    }

    private static CombatStat GetStatOf(Attributes attribute)
        => attribute switch
        {
            Attributes.Strength     => CombatStat.Strength,
            Attributes.Dexterity    => CombatStat.Dexterity,
            Attributes.Intelligence => CombatStat.Intelligence,
            Attributes.Constitution => CombatStat.Constitution,
            Attributes.Awareness    => CombatStat.Awareness,
            _                       => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null)
        };

    public CharacterSave CaptureProgress()
        => new()
        {
            Name            = CharacterName,
            Level           = progress.Level,
            XpTotal         = progress.XpTotal,
            AttributePoints = progress.AttributePoints,
            Strength        = (int)Stats.GetBase(CombatStat.Strength),
            Dexterity       = (int)Stats.GetBase(CombatStat.Dexterity),
            Intelligence    = (int)Stats.GetBase(CombatStat.Intelligence),
            Constitution    = (int)Stats.GetBase(CombatStat.Constitution),
            Awareness       = (int)Stats.GetBase(CombatStat.Awareness)
        };

    public void RestoreProgress(CharacterSave save)
    {
        CharacterName = CharacterNames.Clean(save.Name);

        progress.Restore(save.Level, save.XpTotal, save.AttributePoints);

        Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Strength, Math.Max(1, save.Strength));
            sheet.SetBase(CombatStat.Dexterity, Math.Max(1, save.Dexterity));
            sheet.SetBase(CombatStat.Intelligence, Math.Max(1, save.Intelligence));
            sheet.SetBase(CombatStat.Constitution, Math.Max(1, save.Constitution));
            sheet.SetBase(CombatStat.Awareness, Math.Max(1, save.Awareness));
        });

        SheetChanged?.Invoke();
    }

    public void RefillResources()
    {
        LifeCurrent = LifeMaximum;
        ManaCurrent = ManaMaximum;
    }

    #endregion
}
