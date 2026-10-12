using System;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Schlag auf den Boden, aus dessen Einschlag Lavarisse in Blickrichtung laufen. Der Schlag trifft jeden im ImpactRadius um den Einschlag
//mit dem Waffenschaden des Skills. Jeder Riss wächst in GrowSec auf seine Länge, liegt DurationSec lang und brennt jeden, der darauf steht,
//mit WeaponDamagePercentPerSec des Waffenschadens als Feuer je Sekunde. Wen der wachsende Riss erreicht und wen der Schlag trifft,
//den trifft der Riss einmal je Schlag mit HitWeaponDamagePercent als Feuer. Von einem Wirkenden liegen höchstens MaxMainCracks Hauptrisse
//zugleich, ein weiterer löst den ältesten vorzeitig auf. Alle Längen in Pixeln auf dem Boden
public sealed record FissureSettings(float ImpactRadius,
                                     float Length,
                                     float Width,
                                     float GrowSec,
                                     float DurationSec,
                                     float WeaponDamagePercentPerSec,
                                     float SpreadDegrees,
                                     float HitWeaponDamagePercent,
                                     int   MaxMainCracks)
{
    //Mehr als ein Halbkreis nach vorn wird der Fächer nicht
    public const float MaxTotalDegrees = 180f;

    //Lava ist Feuer, egal was die Waffe macht
    public const DamageType CrackDamageType = DamageType.Fire;

    //Der Riss rechnet wie ein Schlag mit seinem Anteil am Waffenschaden, ganz in Feuer umgewandelt. Das Ergebnis gilt je Sekunde
    public AttackDefinition GetAttack(AttackDefinition slam)
    {
        ArgumentNullException.ThrowIfNull(slam);

        return slam with { WeaponDamagePercent = WeaponDamagePercentPerSec, DealtAs = CrackDamageType };
    }

    //Der Treffer, wenn der Riss frisch durch einen Gegner läuft, ebenso ganz in Feuer
    public AttackDefinition GetHitAttack(AttackDefinition slam)
    {
        ArgumentNullException.ThrowIfNull(slam);

        return slam with { WeaponDamagePercent = HitWeaponDamagePercent, DealtAs = CrackDamageType };
    }

    //Jedes zusätzliche Projektil gibt einen weiteren Riss aus demselben Einschlag
    public int GetCount(int bonusProjectiles)
        => 1 + Math.Max(0, bonusProjectiles);

    //Der Fächer liegt mittig um die Blickrichtung, ein einzelner Riss läuft geradeaus
    public float GetOffsetDegrees(int index, int count)
    {
        if (count <= 1)
            return 0f;

        var totalDegrees = Math.Min(MaxTotalDegrees, Math.Max(0f, SpreadDegrees) * (count - 1));
        var step         = totalDegrees / (count - 1);

        return -totalDegrees / 2f + step * Math.Clamp(index, 0, count - 1);
    }

    //So viele der ältesten Hauptrisse lösen sich auf, wenn so viele liegen. Ohne Grenze (0 oder weniger) keiner
    public int GetExcess(int activeMainCracks)
        => MaxMainCracks > 0 ? Math.Max(0, activeMainCracks - MaxMainCracks) : 0;

    //Der Anteil der Länge, den der Riss nach so langer Zeit erreicht hat
    public float GetGrownShare(double activeSec)
    {
        if (GrowSec <= 0f)
            return 1f;

        return (float)Math.Clamp(activeSec / GrowSec, 0.0, 1.0);
    }

    public bool IsOver(double activeSec)
        => activeSec >= DurationSec;

    public float HalfWidth => Math.Max(0f, Width) / 2f;
}
