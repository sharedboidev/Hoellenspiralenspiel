using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models;

namespace Hoellenspiralenspiel.Scripts.Units;

public abstract partial class BaseUnit : CharacterBody3D,
                                         INotifyPropertyChanged
{
    private int   lifeCurrent;
    private float LifeAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Life);
    private float LifePercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Life);
    private float LifeMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Life);
    public  int   LifeMaximum              => (int)((LifeBase + LifeAddedFlat) * LifePercentageMultiplier * LifeMoreMultiplierTotal);

    [Export]
    public int LifeBase { get; set; }

    public int LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, value);
    }

    [Export]
    public float                             Movementspeed       { get; set; }
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

    public override void _Ready() { }

    public override void _Process(double delta)
    {
        if (IsDead)
            DieProperly();
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