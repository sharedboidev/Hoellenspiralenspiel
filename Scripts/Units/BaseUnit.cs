using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Units;

public abstract partial class BaseUnit
        : CharacterBody2D,
          INotifyPropertyChanged
{
    public delegate void AttributeChangedEventHandler(CombatStat attribute, int value);

    public delegate void DiedEventHandler(BaseUnit unit);

    private Vector2 movementDirection = Vector2.Zero;

    [Export]
    public Vector2 MovementDirection
    {
        get => movementDirection;
        set => SetField(ref movementDirection, value);
    }

    [Export]
    public float Movementspeed { get; set; }

    public bool                               IsDead              => LifeCurrent <= 0;
    public List<CombatStatModifier>           CombatStatModifiers { get; protected set; } = new();
    public event PropertyChangedEventHandler  PropertyChanged;
    public event AttributeChangedEventHandler AttributeChanged;
    public event DiedEventHandler             Died;

    public override void _PhysicsProcess(double delta)
        => ResolveLifeReg(delta);

    public virtual void ReceiveDamage(HitResult hit)
    {
        var mainScene = GetTree().CurrentScene;

        LifeCurrent -= hit.MitigatedDamage;
        
        this.InstatiateFloatingCombatText(hit, mainScene, new Vector2(0, -75));
    }

    protected virtual void ResolveLifeReg(double delta)
    {
        if (LiferegenerationFinal > 0 && LifeCurrent < LifeMaximum)
        {
            LifeCurrent += LiferegenerationFinal * (float)delta;
            LifeCurrent =  Mathf.Clamp(LifeCurrent, 0, LifeMaximum);
        }
    }

    public float GetTotalMoreMultiplierOf(CombatStat combatStat)
    {
        var totalMoreMultiplier = 1f;

        foreach (var modifier in GetModifierOf(ModificationType.More, combatStat))
            totalMoreMultiplier *= 1 + modifier.Value;

        return totalMoreMultiplier;
    }

    public float GetModifierSumOf(ModificationType modificationType, CombatStat combatStat)
        => GetModifierOf(modificationType, combatStat).Sum(mod => mod.Value);

    private IEnumerable<CombatStatModifier> GetModifierOf(ModificationType modificationType, CombatStat combatStat)
        => CombatStatModifiers.Where(mod => mod.AffectedStat == combatStat &&
                                            mod.ModificationType == modificationType);

    public override void _Ready()
    {
        LifeCurrent = LifeMaximum;

        SubscribeAndInitAttributeDerivedStats();
    }

    private void SubscribeAndInitAttributeDerivedStats()
    {
        AttributeChanged += OnAttributeChanged;

        AttributeChanged?.Invoke(CombatStat.Strength, StrengthFinal);
        AttributeChanged?.Invoke(CombatStat.Dexterity, DexterityFinal);
        AttributeChanged?.Invoke(CombatStat.Intelligence, IntelligenceFinal);
        AttributeChanged?.Invoke(CombatStat.Constitution, ConstitutionFinal);
        AttributeChanged?.Invoke(CombatStat.Awareness, AwarenessFinal);
    }

    private void OnAttributeChanged(CombatStat attribute, int value)
    {
        var derivedStats = DerivedStatProvider.GetModifiersFor(attribute, value);
        var modIds       = derivedStats.Select(stat => stat.OriginId).ToArray();

        foreach (var modId in modIds)
            RemoveModifiers(modId);

        CombatStatModifiers.AddRange(derivedStats);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LifeMaximum))); //Hack, weil Racecondition zwischen ResourceOrb und Der Zeile hier drüber, obwohl beide Das selbe Event subscriben
    }

    protected void RemoveModifiers(string modId)
    {
        var modifierToRemove = CombatStatModifiers.Where(mod => mod.OriginId == modId).ToList();
        CombatStatModifiers = CombatStatModifiers.Except(modifierToRemove).ToList();
    }

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

    protected virtual void DieProperly()
    {
        Died?.Invoke(this);

        QueueFree();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        switch (propertyName)
        {
            case nameof(StrengthBase):
                AttributeChanged?.Invoke(CombatStat.Strength, StrengthFinal);

                break;

            case nameof(DexterityBase):
                AttributeChanged?.Invoke(CombatStat.Dexterity, DexterityFinal);

                break;

            case nameof(IntelligenceBase):
                AttributeChanged?.Invoke(CombatStat.Intelligence, IntelligenceFinal);

                break;

            case nameof(ConstitutionBase):
                AttributeChanged?.Invoke(CombatStat.Constitution, ConstitutionFinal);

                break;

            case nameof(AwarenessBase):
                AttributeChanged?.Invoke(CombatStat.Awareness, AwarenessFinal);

                break;
        }
    }

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    #region Attributes

    private int   awarenessBase    = 1;
    private int   constitutionBase = 1;
    private int   dexterityBase    = 1;
    private int   intelligenceBase = 1;
    private int   strengthBase     = 1;
    private float AwarenessAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Awareness);
    private float AwarenessPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Awareness);
    private float AwarenessMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Awareness);
    public  int   AwarenessFinal                => (int)((AwarenessBase + AwarenessAddedFlat) * AwarenessPercentageMultiplier * AwarenessMoreMultiplierTotal);

    [Export]
    public int AwarenessBase
    {
        get => awarenessBase;
        set => SetField(ref awarenessBase, value);
    }

    private float ConstitutionAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Constitution);
    private float ConstitutionPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Constitution);
    private float ConstitutionMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Constitution);
    public  int   ConstitutionFinal                => (int)((ConstitutionBase + ConstitutionAddedFlat) * ConstitutionPercentageMultiplier * ConstitutionMoreMultiplierTotal);

    [Export]
    public int ConstitutionBase
    {
        get => constitutionBase;
        set => SetField(ref constitutionBase, value);
    }

    private float IntelligenceAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Intelligence);
    private float IntelligencePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Intelligence);
    private float IntelligenceMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Intelligence);
    public  int   IntelligenceFinal                => (int)((IntelligenceBase + IntelligenceAddedFlat) * IntelligencePercentageMultiplier * IntelligenceMoreMultiplierTotal);

    [Export]
    public int IntelligenceBase
    {
        get => intelligenceBase;
        set => SetField(ref intelligenceBase, value);
    }

    private float DexterityAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Dexterity);
    private float DexterityPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Dexterity);
    private float DexterityMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Dexterity);
    public  int   DexterityFinal                => (int)((DexterityBase + DexterityAddedFlat) * DexterityPercentageMultiplier * DexterityMoreMultiplierTotal);

    [Export]
    public int DexterityBase
    {
        get => dexterityBase;
        set => SetField(ref dexterityBase, value);
    }

    private float StrengthAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Strength);
    private float StrengthPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Strength);
    public  float StrengthMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Strength);
    public  int   StrengthFinal                => (int)((StrengthBase + StrengthAddedFlat) * StrengthPercentageMultiplier * StrengthMoreMultiplierTotal);

    [Export]
    public int StrengthBase
    {
        get => strengthBase;
        set => SetField(ref strengthBase, value);
    }

    #endregion

    #region Offences

    [Export]
    public int AttackspeedBase { get; set; }

    public float AttackspeedAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Attackspeed);
    public float AttackspeedPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Attackspeed);
    public float AttackspeedMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Attackspeed);
    public int   AttackspeedFinal                => (int)((AttackspeedBase + AttackspeedAddedFlat) * AttackspeedPercentageMultiplier * AttackspeedMoreMultiplierTotal);

    [Export]
    public int SpellDamageBase { get; set; }

    public float SpellDamageAddedFlat                 => GetModifierSumOf(ModificationType.Flat, CombatStat.SpellDamage);
    public float SpellDamagePercentageMultiplier      => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.SpellDamage);
    public float SpellDamageMoreMultiplierTotal       => GetTotalMoreMultiplierOf(CombatStat.SpellDamage);
    public int   SpellDamageFinal                     => (int)((SpellDamageBase + SpellDamageAddedFlat) * SpellDamagePercentageMultiplier * SpellDamageMoreMultiplierTotal);
    public float CriticalHitChanceMoreMultiplierTotal => GetTotalMoreMultiplierOf(CombatStat.CriticalHitChance);
    public float PhysicalDamageMoreMultiplierTotal    => GetTotalMoreMultiplierOf(CombatStat.PhysicalDamage);

    #endregion

    #region Defences

    public  float LifeAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Life);
    public  float LifePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Life);
    public  float LifeMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Life);
    public  float LifeMaximum              => (int)((LifeBase + LifeAddedFlat) * LifePercentageMultiplier * LifeMoreMultiplierTotal);
    public  int   LifeBase                 => 5 + StrengthFinal + 3 * ConstitutionFinal;
    private float lifeCurrent;

    [Export]
    public float LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, Math.Min(value, LifeMaximum));
    }

    public int LiferegenerationBase => (int)(StrengthFinal / 5f + ConstitutionFinal / 3f);

    public float LiferegenerationAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Liferegeneration);
    public float LiferegenerationPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Liferegeneration);
    public float LiferegenerationMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Liferegeneration);
    public int   LiferegenerationFinal                => (int)((LiferegenerationBase + LiferegenerationAddedFlat) * LiferegenerationPercentageMultiplier * LiferegenerationMoreMultiplierTotal);

    [Export]
    public int ArmorBase { get; set; }

    public float ArmorAddedFlat                => GetModifierSumOf(ModificationType.Flat, CombatStat.Armor);
    public float ArmorPercentageMultiplier     => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Armor);
    public float ArmorMoreMultiplierTotal      => GetTotalMoreMultiplierOf(CombatStat.Armor);
    public int   ArmorFinal                    => (int)((ArmorBase + ArmorAddedFlat) * ArmorPercentageMultiplier * ArmorMoreMultiplierTotal);
    
    
    public float MeleeParryMoreMultiplierTotal => GetTotalMoreMultiplierOf(CombatStat.MeleeParry);
    public float MeleeBlockMoreMultiplierTotal => GetTotalMoreMultiplierOf(CombatStat.MeleeBlock);

    [Export]
    public int DodgeBase { get; set; } = 6;
    public float DodgeAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Dodge);
    public float DodgePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Dodge);
    public float DodgeMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Dodge);
    public int   DodgeFinal                => (int)((DodgeBase + DodgeAddedFlat) * DodgePercentageMultiplier * DodgeMoreMultiplierTotal);

    [Export]
    public int FireResiBase { get; set; }

    private float FireResiAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.FireResistance);
    private float FireResiPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.FireResistance);
    private float FireResiMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.FireResistance);
    public  int   FireResiFinal                => (int)((FireResiBase + FireResiAddedFlat) * FireResiPercentageMultiplier * FireResiMoreMultiplierTotal);

    [Export]
    public int FrostResiBase { get; set; }

    private float FrostResiAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.FrostResistance);
    private float FrostResiPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.FrostResistance);
    private float FrostResiMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.FrostResistance);
    public  int   FrostResiFinal                => (int)((FrostResiBase + FrostResiAddedFlat) * FrostResiPercentageMultiplier * FrostResiMoreMultiplierTotal);

    [Export]
    public int LightningResiBase { get; set; }

    private float LightningResiAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.LightningResistance);
    private float LightningResiPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.LightningResistance);
    private float LightningResiMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.LightningResistance);
    public  int   LightningResiFinal                => (int)((LightningResiBase + LightningResiAddedFlat) * LightningResiPercentageMultiplier * LightningResiMoreMultiplierTotal);

    #endregion
}