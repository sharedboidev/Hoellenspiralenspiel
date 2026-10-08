using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public enum HitAvoidance
{
    None,
    Missed,
    Dodged,
    Parried
}

public sealed record HitResult
{
    public DamageType   DamageType { get; init; }
    public SkillKind    SkillKind  { get; init; }
    public HitAvoidance Avoidance  { get; init; }
    public bool         WasBlocked { get; init; }
    public bool         IsCritical { get; init; }

    public float RolledDamage { get; init; }

    public float UnmitigatedDamage { get; init; }

    //Alles zusammen, mit dem Zusatzschaden der Elemente
    public int FinalDamage { get; init; }

    //Der Effekt des Hauptteils
    public StatusEffectApplication InflictedEffect { get; init; }

    //Gemindert, je Element. FinalDamage enthält ihn schon
    public PerElement<int> AddedDamage { get; init; }

    public PerElement<StatusEffectApplication> AddedEffects { get; init; }

    //Was der Treffer löst, bevor sein Schaden zählt, etwa Brittle durch Feuer. Nur bei einem gelandeten Treffer gesetzt
    public IReadOnlyList<StatusEffectKind> RemovedEffects { get; init; } = [];

    public bool HasLanded => Avoidance == HitAvoidance.None;

    //Der Zusatzschaden ist immer elementar, physisch kann nur der Hauptteil sein. Aus ihm saugt Leech
    public int PhysicalDamage => DamageType.IsPhysical() ? FinalDamage - AddedDamage.Fire - AddedDamage.Frost - AddedDamage.Lightning : 0;

    public IEnumerable<StatusEffectApplication> InflictedEffects
    {
        get
        {
            if (InflictedEffect is not null)
                yield return InflictedEffect;

            foreach (var (_, effect) in AddedEffects.Entries)
            {
                if (effect is not null)
                    yield return effect;
            }
        }
    }
}
