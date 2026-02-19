using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Units;

public abstract partial class BaseUnit : CharacterBody2D,
                                         INotifyPropertyChanged
{
    private int     lifeCurrent;
    private Vector2 movementDirection = Vector2.Zero;
    private float   LifeAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Life);
    private float   LifePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Life);
    private float   LifeMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Life);
    public  int     LifeMaximum              => (int)((LifeBase + LifeAddedFlat) * LifePercentageMultiplier * LifeMoreMultiplierTotal);

    [Export]
    public int LifeBase { get; set; } = 1;

    [Export]
    public int LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, Math.Min(value, LifeMaximum));
    }

    [Export]
    public Vector2 MovementDirection
    {
        get => movementDirection;
        set => SetField(ref movementDirection, value);
    }

    [Export]
    public float Movementspeed { get; set; }

    public bool                              IsDead              => LifeCurrent <= 0;
    public List<CombatStatModifier>          CombatStatModifiers { get; } = new();
    public event PropertyChangedEventHandler PropertyChanged;

    private float GetTotalMoreMultiplierOf(CombatStat combatStat)
    {
        var totalMoreMultiplier = 1f;

        foreach (var modifier in GetModifierOf(ModificationType.More, combatStat))
            totalMoreMultiplier *= 1 + modifier.Value;

        return totalMoreMultiplier;
    }

    private float GetModifierSumOf(ModificationType modificationType, CombatStat combatStat)
        => GetModifierOf(modificationType, combatStat).Sum(mod => mod.Value);

    private IEnumerable<CombatStatModifier> GetModifierOf(ModificationType modificationType, CombatStat combatStat)
        => CombatStatModifiers.Where(mod => mod.AffectedStat == combatStat &&
                                            mod.ModificationType == modificationType);

    public override void _Ready() => LifeCurrent = LifeMaximum;

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

    protected virtual void DieProperly() => QueueFree();

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }
}