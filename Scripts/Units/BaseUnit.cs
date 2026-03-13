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

namespace Hoellenspiralenspiel.Scripts.Units;

public abstract partial class BaseUnit
        : CharacterBody2D,
          INotifyPropertyChanged
{
    public delegate void DiedEventHandler(BaseUnit unit);

    private float   lifeCurrent;
    private Vector2 movementDirection = Vector2.Zero;
    private float   AwarenessAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Awareness);
    private float   AwarenessPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Awareness);
    private float   AwarenessMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Awareness);
    public  int     AwarenessFinal                => (int)((AwarenessBase + AwarenessAddedFlat) * AwarenessPercentageMultiplier * AwarenessMoreMultiplierTotal);

    [Export]
    public int AwarenessBase { get; set; } = 1;

    private float ConstitutionAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Constitution);
    private float ConstitutionPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Constitution);
    private float ConstitutionMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Constitution);
    public  int   ConstitutionFinal                => (int)((ConstitutionBase + ConstitutionAddedFlat) * ConstitutionPercentageMultiplier * ConstitutionMoreMultiplierTotal);

    [Export]
    public int ConstitutionBase { get; set; } = 1;

    private float IntelligenceAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Intelligence);
    private float IntelligencePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Intelligence);
    private float IntelligenceMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Intelligence);
    public  int   IntelligenceFinal                => (int)((IntelligenceBase + IntelligenceAddedFlat) * IntelligencePercentageMultiplier * IntelligenceMoreMultiplierTotal);

    [Export]
    public int IntelligenceBase { get; set; } = 1;

    private float DexterityAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Dexterity);
    private float DexterityPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Dexterity);
    private float DexterityMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Dexterity);
    public  int   DexterityFinal                => (int)((DexterityBase + DexterityAddedFlat) * DexterityPercentageMultiplier * DexterityMoreMultiplierTotal);

    [Export]
    public int DexterityBase { get; set; } = 1;

    private float StrengthAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Strength);
    private float StrengthPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Strength);
    private float StrengthMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Strength);
    public  int   StrengthFinal                => (int)((StrengthBase + StrengthAddedFlat) * StrengthPercentageMultiplier * StrengthMoreMultiplierTotal);

    [Export]
    public int StrengthBase { get; set; } = 1;

    private float LifeAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Life);
    private float LifePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Life);
    private float LifeMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Life);
    public  float LifeMaximum              => (int)((LifeBase + LifeAddedFlat) * LifePercentageMultiplier * LifeMoreMultiplierTotal);

    public int LifeBase  => 5 + StrengthFinal + 3*ConstitutionFinal;

    [Export]
    public float LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, Math.Min(value, LifeMaximum));
    }

    [Export]
    public int LiferegenerationBase { get; set; }

    private float LiferegenerationAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Liferegeneration);
    private float LiferegenerationPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Liferegeneration);
    private float LiferegenerationMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Liferegeneration);
    public  int   LiferegenerationFinal                => (int)((LiferegenerationBase + LiferegenerationAddedFlat) * LiferegenerationPercentageMultiplier * LiferegenerationMoreMultiplierTotal);

    [Export]
    public int ArmorBase { get; set; }

    private float ArmorAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Armor);
    private float ArmorPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Armor);
    private float ArmorMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Armor);
    public  int   ArmorFinal                => (int)((ArmorBase + ArmorAddedFlat) * ArmorPercentageMultiplier * ArmorMoreMultiplierTotal);

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

    [Export]
    public Vector2 MovementDirection
    {
        get => movementDirection;
        set => SetField(ref movementDirection, value);
    }

    [Export]
    public float Movementspeed { get; set; }

    public bool                              IsDead              => LifeCurrent <= 0;
    public List<CombatStatModifier>          CombatStatModifiers { get; protected set; } = new();
    public event PropertyChangedEventHandler PropertyChanged;

    public event DiedEventHandler Died;

    public override void _PhysicsProcess(double delta)
        => ResolveLifeReg(delta);

    public virtual void ReceiveDamage(HitResult hit)
    {
        var mainScene = GetTree().CurrentScene;
        this.InstatiateFloatingCombatText(hit, mainScene, new Vector2(0, -75));

        LifeCurrent -= hit.MitigatedDamage;
    }

    protected virtual void ResolveLifeReg(double delta)
    {
        if (LifeCurrent < LifeMaximum)
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
        => LifeCurrent = LifeMaximum;

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
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }
}