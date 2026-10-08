using System;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

//Stirbt eine Einheit mit Brittle, zerspringt sie für einen Anteil ihres Lebens. Der Zauber gehört dem, der Brittle gelegt hat:
//Sein Zauber- und Kälteschaden erhöhen ihn, er kann mit seiner Chance kritisch treffen, aber nicht verfehlen
public static class BrittleShatter
{
    public static HitRequest CreateRequest(StatSheet source, SpellDefinition shatter, float victimLifeMaximum)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(shatter);

        var damage  = Math.Max(0f, victimLifeMaximum) * CombatRules.BrittleShatterLifeFraction;
        var request = HitRequests.ForSpell(source, shatter with { MinDamage = damage, MaxDamage = damage });

        return request with { HitChance = Math.Max(request.HitChance, CombatRules.BaseHitChance) };
    }
}
