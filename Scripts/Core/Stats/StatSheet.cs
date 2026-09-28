using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

//Hält alle Stats einer Einheit. Gerechnet wird nur, wenn sich etwas ändert, gelesen wird aus einem Zwischenspeicher.
//Die Klasse ist reines C# ohne Godot, damit sie ohne Engine testbar ist und für 2D und 3D gleich funktioniert.
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

    //Modifier von außen, z.B. von Ausrüstung
    public IReadOnlyList<CombatStatModifier> Modifiers => modifiers;

    //Modifier, die das Blatt selbst aus den Attributen ableitet
    public IReadOnlyList<CombatStatModifier> DerivedModifiers => derivedModifiers;

    //Zählt die Neuberechnungen, für Tests und zur Diagnose
    public int RecalculationCount { get; private set; }

    //Feuert nach jeder Neuberechnung, also erst wenn alle Werte aktuell sind
    public event Action Changed;

    public static bool IsAttribute(CombatStat stat)
        => stat is CombatStat.Strength or CombatStat.Dexterity or CombatStat.Intelligence or CombatStat.Constitution or CombatStat.Awareness;

    //Der gesetzte Grundwert. Bei Leben, Mana und Lebensregeneration ist das der Zuschlag auf die Formel aus den Attributen
    public float GetBase(CombatStat stat)
        => baseValues[(int)stat];

    //Der Grundwert, mit dem gerechnet wird, inklusive der Formel aus den Attributen
    public float GetEffectiveBase(CombatStat stat)
        => effectiveBase[(int)stat];

    public float GetAddedFlat(CombatStat stat)
        => addedFlat[(int)stat];

    public float GetIncreasedMultiplier(CombatStat stat)
        => 1 + increased[(int)stat];

    public float GetMoreMultiplier(CombatStat stat)
        => more[(int)stat];

    //Increased und More zusammen, z.B. 1,4 für "+40 %"
    public float GetTotalMultiplier(CombatStat stat)
        => GetIncreasedMultiplier(stat) * GetMoreMultiplier(stat);

    public float GetFinal(CombatStat stat)
        => finalValues[(int)stat];

    //Endwert ohne Nachkommastellen, so wie ihn Kampf und Anzeige bisher benutzen
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

    //Entfernt alle Modifier einer Herkunft, z.B. eines abgelegten Items. Liefert die Anzahl der entfernten Modifier
    public int RemoveModifiersOf(string originId)
    {
        var removed = modifiers.RemoveAll(modifier => modifier.OriginId == originId);

        if (removed > 0)
            NotifyChanged();

        return removed;
    }

    //Fasst mehrere Änderungen zusammen. Gerechnet und gemeldet wird einmal am Ende
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

        //Schritt 1: Attribute. Sie hängen nur von Grundwert und äußeren Modifiern ab
        foreach (var attribute in Attributes)
        {
            effectiveBase[(int)attribute] = baseValues[(int)attribute];
            finalValues[(int)attribute]   = StatFormulas.TruncateAttribute(CombineFor(attribute));
        }

        //Schritt 2: Aus den Endwerten der Attribute leiten sich weitere Modifier ab
        derivedModifiers.Clear();

        foreach (var attribute in Attributes)
            derivedModifiers.AddRange(DerivedStatProvider.GetModifiersFor(attribute, GetFinalWhole(attribute)));

        foreach (var modifier in derivedModifiers)
        {
            //Ein Attribut darf kein Attribut ableiten, sonst hinge das Ergebnis von der Reihenfolge ab
            if (!IsAttribute(modifier.AffectedStat))
                Accumulate(modifier);
        }

        //Schritt 3: Alle übrigen Stats
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
