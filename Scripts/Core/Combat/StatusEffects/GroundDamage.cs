using System;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

//Schaden über Zeit vom Boden, etwa aus einem Lavariss. Er braucht keinen Treffer: kein Trefferwurf, kein Krit, kein Ausweichen, kein Block.
//perSecond ist ein Treffer, wie ihn der Urheber je Sekunde machte. Sein ganzer mittlerer Schaden zählt als die Schadensart des Effekts,
//die Multiplikatoren des Urhebers für Schaden über Zeit heben ihn, die Resistenz des Stehenden mindert ihn
public static class GroundDamage
{
    //Vor der Verteidigung des Stehenden, so wie der Tooltip ihn zeigt
    public static float GetDamagePerSecond(HitRequest perSecond, DamageType damageType)
    {
        ArgumentNullException.ThrowIfNull(perSecond);

        var average = (Math.Max(0f, perSecond.MinDamage) + Math.Max(0f, perSecond.MaxDamage)) / 2f;

        foreach (var (_, range) in perSecond.AddedDamage.Entries)
        {
            if (!range.IsEmpty)
                average += (Math.Max(0f, range.Min) + Math.Max(0f, range.Max)) / 2f;
        }

        return average * damageType.GetDamageFactor() * perSecond.DamageOverTimeMultiplier * perSecond.DamageOverTimeByType.For(damageType);
    }

    //Der Effekt für eine Einheit, die gerade darauf steht. Er läuft GroundDamageLingerSec und wird nach der stärksten Instanz gezählt
    public static StatusEffectApplication Create(StatusEffectKind kind, HitRequest perSecond, StatSheet defender, object source = null)
    {
        ArgumentNullException.ThrowIfNull(defender);

        if (StatusEffectRules.GetDamageType(kind) is not { } damageType)
            throw new ArgumentException($"{kind} macht keinen Schaden.", nameof(kind));

        var damage = GetDamagePerSecond(perSecond, damageType);

        if (damageType.IsElemental())
            damage = CombatFormulas.MitigateByResistance(damage, Defences.GetEffectiveResistance(defender, damageType));

        return new StatusEffectApplication(kind, damage, CombatRules.GroundDamageLingerSec) { Source = source };
    }
}
