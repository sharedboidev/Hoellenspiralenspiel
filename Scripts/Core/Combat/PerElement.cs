using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Ein Wert je Element. Als Struct vergleicht er sich nach Inhalt, eine Liste täte das in einem Record nicht
public readonly record struct PerElement<T>(T Fire, T Frost, T Lightning)
{
    public T this[DamageType element]
        => element switch
        {
            DamageType.Fire      => Fire,
            DamageType.Frost     => Frost,
            DamageType.Lightning => Lightning,
            _                    => throw new ArgumentOutOfRangeException(nameof(element), element, "Kein Element")
        };

    public IEnumerable<(DamageType Element, T Value)> Entries
    {
        get
        {
            yield return (DamageType.Fire, Fire);
            yield return (DamageType.Frost, Frost);
            yield return (DamageType.Lightning, Lightning);
        }
    }

    public static PerElement<T> From(Func<DamageType, T> valueOf)
    {
        ArgumentNullException.ThrowIfNull(valueOf);

        return new PerElement<T>(valueOf(DamageType.Fire), valueOf(DamageType.Frost), valueOf(DamageType.Lightning));
    }

    public PerElement<TResult> Select<TResult>(Func<DamageType, T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new PerElement<TResult>(selector(DamageType.Fire, Fire), selector(DamageType.Frost, Frost), selector(DamageType.Lightning, Lightning));
    }

    public PerElement<T> With(DamageType element, T value)
        => element switch
        {
            DamageType.Fire      => this with { Fire = value },
            DamageType.Frost     => this with { Frost = value },
            DamageType.Lightning => this with { Lightning = value },
            _                    => throw new ArgumentOutOfRangeException(nameof(element), element, "Kein Element")
        };
}
