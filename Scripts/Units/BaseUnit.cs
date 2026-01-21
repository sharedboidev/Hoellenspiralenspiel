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
    private int  lifeCurrent;
    public  bool IsDead      => LifeCurrent <= 0;
    public  int  LifeMaximum => (int)((LifeBase + ModifierSumOf(ModificationType.Flat, CombatStat.Life)) * ModifierSumOf(ModificationType.Percentage, CombatStat.Life));
    public  int  LifeBase    { get; set; }

    public int LifeCurrent
    {
        get => lifeCurrent;
        set => SetField(ref lifeCurrent, value);
    }

    public List<CombatStatModifier>          CombatStatModifiers { get; } = new();
    public event PropertyChangedEventHandler PropertyChanged;

    private float ModifierSumOf(ModificationType modificationType, CombatStat combatStat)
        => CombatStatModifiers.Where(mod => mod.AffectedStat == combatStat &&
                                            mod.ModificationType == modificationType)
                              .Sum(mod => mod.Value);

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