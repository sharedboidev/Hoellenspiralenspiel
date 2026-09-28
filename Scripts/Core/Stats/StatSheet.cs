using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

public sealed class StatSheet
{
    public static readonly IReadOnlyList<CombatStat> Attributes =
    [
        CombatStat.Strength,
        CombatStat.Dexterity,
        CombatStat.Intelligence,
        CombatStat.Constitution,
        CombatStat.Awareness
    ];

    private static readonly CombatStat[] AllStats  = Enum.GetValues<CombatStat>();
    private static readonly int          StatCount = AllStats.Max(stat => (int)stat) + 1;

    private readonly float[]                  addedFlat        = new float[StatCount];
    private readonly float[]                  baseValues       = new float[StatCount];
    private readonly List<CombatStatModifier> derivedModifiers = new();
    private readonly float[]                  effectiveBase    = new float[StatCount];
    private readonly float[]                  finalValues      = new float[StatCount];
    private readonly float[]                  increased        = new float[StatCount];
    private readonly List<CombatStatModifier> modifiers        = new();
    private readonly float[]                  more             = new float[StatCount];
    private          bool                     hasPendingChanges;
    private          int                      updateDepth;

    public StatSheet()
        => Recalculate();

    public IReadOnlyList<CombatStatModifier> Modifiers => modifiers;

    public IReadOnlyList<CombatStatModifier> DerivedModifiers => derivedModifiers;

    public int RecalculationCount { get; private set; }

    public event Action Changed;

    public static bool IsAttribute(CombatStat stat)
        => stat is CombatStat.Strength or CombatStat.Dexterity or CombatStat.Intelligence or CombatStat.Constitution or CombatStat.Awareness;

    //Bei Leben, Mana und Lebensregeneration ist der Grundwert ein Zuschlag auf die Formel aus den Attributen
    public float GetBase(CombatStat stat)
        => baseValues[(int)stat];

    public float GetEffectiveBase(CombatStat stat)
        => effectiveBase[(int)stat];

    public float GetAddedFlat(CombatStat stat)
        => addedFlat[(int)stat];

    public float GetIncreasedMultiplier(CombatStat stat)
        => 1 + increased[(int)stat];

    public float GetMoreMultiplier(CombatStat stat)
        => more[(int)stat];

    public float GetTotalMultiplier(CombatStat stat)
        => GetIncreasedMultiplier(stat) * GetMoreMultiplier(stat);

    public float GetFinal(CombatStat stat)
        => finalValues[(int)stat];

    public int GetFinalWhole(CombatStat stat)
        => (int)finalValues[(int)stat];

    public void SetBase(CombatStat stat, float value)
    {
        if (baseValues[(int)stat].Equals(value))
            return;

        baseValues[(int)stat] = value;

        NotifyChanged();
    }

    public void AddModifier(CombatStatModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        modifiers.Add(modifier);

        NotifyChanged();
    }

    public void AddModifiers(IEnumerable<CombatStatModifier> newModifiers)
    {
        ArgumentNullException.ThrowIfNull(newModifiers);

        var countBefore = modifiers.Count;

        foreach (var modifier in newModifiers)
        {
            ArgumentNullException.ThrowIfNull(modifier);

            modifiers.Add(modifier);
        }

        if (modifiers.Count != countBefore)
            NotifyChanged();
    }

    public int RemoveModifiersOf(string originId)
    {
        var removed = modifiers.RemoveAll(modifier => modifier.OriginId == originId);

        if (removed > 0)
            NotifyChanged();

        return removed;
    }

    public void Update(Action<StatSheet> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        updateDepth++;

        try
        {
            changes(this);
        }
        finally
        {
            updateDepth--;
        }

        if (updateDepth > 0 || !hasPendingChanges)
            return;

        hasPendingChanges = false;

        RecalculateAndNotify();
    }

    private void NotifyChanged()
    {
        if (updateDepth > 0)
        {
            hasPendingChanges = true;

            return;
        }

        RecalculateAndNotify();
    }

    private void RecalculateAndNotify()
    {
        Recalculate();

        Changed?.Invoke();
    }

    private void Recalculate()
    {
        RecalculationCount++;

        Array.Clear(addedFlat);
        Array.Clear(increased);
        Array.Fill(more, 1f);

        foreach (var modifier in modifiers)
            Accumulate(modifier);

        //Die Reihenfolge zählt: erst Attribute, dann die daraus abgeleiteten Modifier, dann alle übrigen Stats
        foreach (var attribute in Attributes)
        {
            effectiveBase[(int)attribute] = baseValues[(int)attribute];
            finalValues[(int)attribute]   = StatFormulas.TruncateAttribute(CombineFor(attribute));
        }

        derivedModifiers.Clear();

        foreach (var attribute in Attributes)
            derivedModifiers.AddRange(DerivedStatProvider.GetModifiersFor(attribute, GetFinalWhole(attribute)));

        foreach (var modifier in derivedModifiers)
        {
            //Ein Attribut darf kein Attribut ableiten, sonst hinge das Ergebnis von der Reihenfolge ab
            if (!IsAttribute(modifier.AffectedStat))
                Accumulate(modifier);
        }

        foreach (var stat in AllStats)
        {
            if (IsAttribute(stat))
                continue;

            effectiveBase[(int)stat] = baseValues[(int)stat] + GetFormulaBase(stat);
            finalValues[(int)stat]   = CombineFor(stat);
        }
    }

    private float GetFormulaBase(CombatStat stat)
        => stat switch
        {
            CombatStat.Life => StatFormulas.GetLifeBase(GetFinalWhole(CombatStat.Strength), GetFinalWhole(CombatStat.Constitution)),
            CombatStat.Mana => StatFormulas.GetManaBase(GetFinalWhole(CombatStat.Awareness), GetFinalWhole(CombatStat.Intelligence)),
            CombatStat.Liferegeneration => StatFormulas.GetLiferegenerationBase(GetFinalWhole(CombatStat.Strength), GetFinalWhole(CombatStat.Constitution)),
            _ => 0
        };

    private float CombineFor(CombatStat stat)
        => StatFormulas.Combine(effectiveBase[(int)stat], addedFlat[(int)stat], increased[(int)stat], more[(int)stat]);

    private void Accumulate(CombatStatModifier modifier)
    {
        var index = (int)modifier.AffectedStat;

        switch (modifier.ModificationType)
        {
            case ModificationType.Flat:
                addedFlat[index] += modifier.Value;

                break;
            case ModificationType.Percentage:
                increased[index] += modifier.Value;

                break;
            case ModificationType.More:
                more[index] *= 1 + modifier.Value;

                break;
            default: throw new ArgumentOutOfRangeException(nameof(modifier), modifier.ModificationType, "Unbekannte Art von Modifier");
        }
    }
}
