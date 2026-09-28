using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Units;

public abstract partial class BaseUnit
        : CharacterBody2D,
          INotifyPropertyChanged
{
    public delegate void DiedEventHandler(BaseUnit unit);

    public delegate void StatsChangedEventHandler();

    private const float MinPickRadiusPx      = 40f;
    private const float PickRadiusPerScalePx = 22f;

    private readonly List<StatusTick> statusTicks = new();
    protected        Sprite2D         AttackSprite;
    private          CollisionShape2D bodyShape;
    protected        Sprite2D         DeathSprite;
    protected        Sprite2D         IdleSprite;
    private          Vector2          movementDirection = Vector2.Zero;
    protected        Sprite2D         RunSprite;

    protected BaseUnit()
    {
        PushBaseValuesToStats();

        StatusEffects = new StatusEffectTracker(Stats);

        Stats.Changed         += OnStatsChanged;
        StatusEffects.Started += OnStatusEffectStarted;
    }

    //Alle Stats der Einheit. Gerechnet wird im Kern, diese Klasse hält nur die Grundwerte für den Inspector
    public StatSheet Stats { get; } = new();

    public StatusEffectTracker StatusEffects { get; }

    public abstract Faction Faction { get; }

    [Export]
    public Vector2 MovementDirection
    {
        get => movementDirection;
        set => SetField(ref movementDirection, value);
    }

    public bool IsDead => LifeCurrent <= 0;

    //Tote und sterbende Einheiten können weder angeklickt noch getroffen werden
    public virtual bool IsTargetable => !IsDead;

    //Die Mitte des Körpers. Abstände im Kampf werden zwischen diesen Punkten gemessen
    public Vector2 BodyCenter => bodyShape?.GlobalPosition ?? GlobalPosition;

    //Radius um die Körpermitte, in dem ein Mausklick die Einheit trifft
    public float PickRadius => Math.Max(MinPickRadiusPx, PickRadiusPerScalePx * Scale.X);

    //Versatz von der Position der Einheit, an dem Schadenszahlen erscheinen
    public virtual Vector2 CombatTextOffset => new(0, -75);

    public event PropertyChangedEventHandler PropertyChanged;
    public event DiedEventHandler            Died;

    //Feuert, nachdem alle Stats neu berechnet sind. Anzeigen hören hierauf
    public event StatsChangedEventHandler StatsChanged;

    public override void _PhysicsProcess(double delta)
    {
        ResolveLifeReg(delta);
        AdvanceStatusEffects(delta);
    }

    public bool IsHostileTo(BaseUnit other)
        => other is not null && other.Faction != Faction;

    public float DistanceTo(BaseUnit other)
        => BodyCenter.DistanceTo(other.BodyCenter);

    //Wendet einen gewürfelten Treffer an: Leben abziehen, Statuseffekt auflegen, Ergebnis anzeigen
    public virtual void ReceiveDamage(HitResult hit)
    {
        if (!IsTargetable)
            return;

        if (hit.HasLanded)
        {
            LifeCurrent -= hit.FinalDamage;

            if (hit.InflictedEffect is not null && !IsDead)
                StatusEffects.Apply(hit.InflictedEffect);
        }

        this.ShowHit(hit);
    }

    //Würfelt, ob die nächste Aktion der Einheit fehlschlägt, z.B. unter Shock
    public bool RollActionFailure()
    {
        var chance = StatusEffects.ActionFailureChance;

        return chance > 0 && GameRandom.Shared.NextFloat() < chance;
    }

    //Die feindliche Einheit, deren Klickfläche den Punkt enthält. Bei mehreren gewinnt die nächste
    public BaseUnit FindHostileUnitAt(Vector2 globalPoint)
    {
        BaseUnit nearestUnit     = null;
        var      nearestDistance = float.MaxValue;

        foreach (var unit in UnitRegistry.Units)
        {
            if (!IsHostileTo(unit) || !unit.IsTargetable)
                continue;

            var distance = unit.BodyCenter.DistanceTo(globalPoint);

            if (distance > unit.PickRadius || distance >= nearestDistance)
                continue;

            nearestUnit     = unit;
            nearestDistance = distance;
        }

        return nearestUnit;
    }

    protected virtual void ResolveLifeReg(double delta)
    {
        if (IsDead)
            return;

        if (LiferegenerationFinal > 0 && LifeCurrent < LifeMaximum)
        {
            LifeCurrent += LiferegenerationFinal * (float)delta;
            LifeCurrent =  Mathf.Clamp(LifeCurrent, 0, LifeMaximum);
        }
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

            this.ShowStatusTick(tick);
        }
    }

    private void OnStatusEffectStarted(StatusEffectKind kind)
    {
        if (IsInsideTree())
            this.ShowStatusStarted(kind);
    }

    public override void _EnterTree()
        => UnitRegistry.Register(this);

    public override void _ExitTree()
        => UnitRegistry.Unregister(this);

    public override void _Ready()
    {
        LoadSpriteNodes();

        bodyShape = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));

        LifeCurrent = LifeMaximum;
    }

    //Hebt die Einheit hervor, solange der Mauszeiger auf ihr liegt
    public virtual void SetHighlight(bool active) { }

    protected void LoadSpriteNodes()
    {
        IdleSprite   = GetNodeOrNull<Sprite2D>("IdleSprite");
        RunSprite    = GetNodeOrNull<Sprite2D>("RunSprite");
        AttackSprite = GetNodeOrNull<Sprite2D>("AttackSprite");
        DeathSprite  = GetNodeOrNull<Sprite2D>("DeathSprite");
    }

    private void OnStatsChanged()
    {
        if (LifeCurrent > LifeMaximum)
            LifeCurrent = LifeMaximum;

        OnStatsRecalculated();

        OnPropertyChanged(nameof(LifeMaximum));

        StatsChanged?.Invoke();
    }

    //Für abgeleitete Klassen, die auf neue Stats reagieren müssen, bevor die Anzeigen informiert werden
    protected virtual void OnStatsRecalculated() { }

    public BaseEnemy[] FindClosestEnemyFrom(List<BaseEnemy> existingEnemies, int amountReturned = 1)
    {
        var enemyDistanceDict = new Godot.Collections.Dictionary<BaseEnemy, float>();

        foreach (var existingEnemy in existingEnemies)
        {
            var distance = GlobalPosition.DistanceSquaredTo(existingEnemy.GlobalPosition);

            enemyDistanceDict.Add(existingEnemy, distance);
        }

        var nearestBois = enemyDistanceDict.OrderBy(dd => dd.Value)
                                           .Take(amountReturned)
                                           .Select(dd => dd.Key)
                                           .ToArray();

        return nearestBois;
    }

    protected void RaiseDied()
        => Died?.Invoke(this);

    protected virtual void DieProperly()
    {
        RaiseDied();

        QueueFree();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    //Setzt einen Grundwert und gibt ihn an den Kern weiter
    protected void SetBaseStat(ref int field, int value, CombatStat stat, [CallerMemberName] string propertyName = null)
    {
        if (field == value)
            return;

        field = value;

        Stats.SetBase(stat, value);
        OnPropertyChanged(propertyName);
    }

    protected void SetBaseStat(ref float field, float value, CombatStat stat, [CallerMemberName] string propertyName = null)
    {
        if (field.Equals(value))
            return;

        field = value;

        Stats.SetBase(stat, value);
        OnPropertyChanged(propertyName);
    }

    //Die Felder tragen die Standardwerte für den Inspector, der Kern kennt sie erst nach dieser Übergabe
    private void PushBaseValuesToStats()
        => Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Strength, strengthBase);
            sheet.SetBase(CombatStat.Dexterity, dexterityBase);
            sheet.SetBase(CombatStat.Intelligence, intelligenceBase);
            sheet.SetBase(CombatStat.Constitution, constitutionBase);
            sheet.SetBase(CombatStat.Awareness, awarenessBase);
            sheet.SetBase(CombatStat.Attackspeed, attackspeedBase);
            sheet.SetBase(CombatStat.SpellDamage, spellDamageBase);
            sheet.SetBase(CombatStat.Life, lifeBaseBonus);
            sheet.SetBase(CombatStat.Armor, armorBase);
            sheet.SetBase(CombatStat.Dodge, dodgeBase);
            sheet.SetBase(CombatStat.FireResistance, fireResiBase);
            sheet.SetBase(CombatStat.FrostResistance, frostResiBase);
            sheet.SetBase(CombatStat.LightningResistance, lightningResiBase);
            sheet.SetBase(CombatStat.Movementspeed, movementspeed);
            sheet.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            sheet.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
            sheet.SetBase(CombatStat.BlockReduction, CombatRules.BaseBlockReduction);
        });

    #region Attributes

    private int awarenessBase    = 1;
    private int constitutionBase = 1;
    private int dexterityBase    = 1;
    private int intelligenceBase = 1;
    private int strengthBase     = 1;

    public int StrengthFinal     => Stats.GetFinalWhole(CombatStat.Strength);
    public int DexterityFinal    => Stats.GetFinalWhole(CombatStat.Dexterity);
    public int IntelligenceFinal => Stats.GetFinalWhole(CombatStat.Intelligence);
    public int ConstitutionFinal => Stats.GetFinalWhole(CombatStat.Constitution);
    public int AwarenessFinal    => Stats.GetFinalWhole(CombatStat.Awareness);

    [Export]
    public int StrengthBase
    {
        get => strengthBase;
        set => SetBaseStat(ref strengthBase, value, CombatStat.Strength);
    }

    [Export]
    public int DexterityBase
    {
        get => dexterityBase;
        set => SetBaseStat(ref dexterityBase, value, CombatStat.Dexterity);
    }

    [Export]
    public int IntelligenceBase
    {
        get => intelligenceBase;
        set => SetBaseStat(ref intelligenceBase, value, CombatStat.Intelligence);
    }

    [Export]
    public int ConstitutionBase
    {
        get => constitutionBase;
        set => SetBaseStat(ref constitutionBase, value, CombatStat.Constitution);
    }

    [Export]
    public int AwarenessBase
    {
        get => awarenessBase;
        set => SetBaseStat(ref awarenessBase, value, CombatStat.Awareness);
    }

    #endregion

    #region Offences

    private int attackspeedBase;
    private int spellDamageBase;

    //Angriffe pro Sekunde. Beim Spieler kommt der Grundwert von der Waffe
    public float AttacksPerSecondFinal => Stats.GetFinal(CombatStat.Attackspeed);
    public int   SpellDamageFinal      => Stats.GetFinalWhole(CombatStat.SpellDamage);

    [Export]
    public int AttackspeedBase
    {
        get => attackspeedBase;
        set => SetBaseStat(ref attackspeedBase, value, CombatStat.Attackspeed);
    }

    [Export]
    public int SpellDamageBase
    {
        get => spellDamageBase;
        set => SetBaseStat(ref spellDamageBase, value, CombatStat.SpellDamage);
    }

    #endregion

    #region Defences

    private int   armorBase;
    private int   dodgeBase = 6;
    private int   fireResiBase;
    private int   frostResiBase;
    private int   lifeBaseBonus;
    private float lifeCurrent;
    private int   lightningResiBase;

    public float LifeMaximum           => Stats.GetFinalWhole(CombatStat.Life);
    public int   LifeBase              => (int)Stats.GetEffectiveBase(CombatStat.Life);
    public int   LiferegenerationFinal => Stats.GetFinalWhole(CombatStat.Liferegeneration);
    public int   ArmorFinal            => Stats.GetFinalWhole(CombatStat.Armor);
    public int   DodgeFinal            => Stats.GetFinalWhole(CombatStat.Dodge);
    public int   FireResiFinal         => Stats.GetFinalWhole(CombatStat.FireResistance);
    public int   FrostResiFinal        => Stats.GetFinalWhole(CombatStat.FrostResistance);
    public int   LightningResiFinal    => Stats.GetFinalWhole(CombatStat.LightningResistance);

    //Fester Zuschlag auf das Basisleben, z.B. für Gegner, deren Leben nicht nur aus Attributen kommen soll
    [Export]
    public int LifeBaseBonus
    {
        get => lifeBaseBonus;
        set => SetBaseStat(ref lifeBaseBonus, value, CombatStat.Life);
    }

    //Das Leben bleibt zwischen 0 und dem Maximum
    [Export]
    public float LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, Math.Max(0, Math.Min(value, LifeMaximum)));
    }

    [Export]
    public int ArmorBase
    {
        get => armorBase;
        set => SetBaseStat(ref armorBase, value, CombatStat.Armor);
    }

    [Export]
    public int DodgeBase
    {
        get => dodgeBase;
        set => SetBaseStat(ref dodgeBase, value, CombatStat.Dodge);
    }

    [Export]
    public int FireResiBase
    {
        get => fireResiBase;
        set => SetBaseStat(ref fireResiBase, value, CombatStat.FireResistance);
    }

    [Export]
    public int FrostResiBase
    {
        get => frostResiBase;
        set => SetBaseStat(ref frostResiBase, value, CombatStat.FrostResistance);
    }

    [Export]
    public int LightningResiBase
    {
        get => lightningResiBase;
        set => SetBaseStat(ref lightningResiBase, value, CombatStat.LightningResistance);
    }

    #endregion

    #region Utilities

    private float movementspeed;

    public float MovementspeedFinal => Stats.GetFinal(CombatStat.Movementspeed);

    //Grundwert der Bewegungsgeschwindigkeit. Für die Bewegung zählt MovementspeedFinal
    [Export]
    public float Movementspeed
    {
        get => movementspeed;
        set => SetBaseStat(ref movementspeed, value, CombatStat.Movementspeed);
    }

    #endregion
}
