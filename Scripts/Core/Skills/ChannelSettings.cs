using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Wirbel, der läuft, solange die Taste gehalten wird. Er kostet Mana je Sekunde statt je Einsatz und trifft in Ticks:
//je Angriff des Angriffstempos TicksPerAttack Ticks, jeder mit dem Waffenschaden des Skills auf alle im Kreis.
//Der erste Tick kommt nach der Hälfte eines Intervalls wie der Treffer eines Schlags, danach einer je Intervall.
//Geht das Mana aus, endet der Wirbel. Danach erholt sich der Held wie nach einem Angriff.
//TurnsPerTick ist nur Darstellung: so oft dreht der Wirbelnde sich zwischen zwei Ticks. Mit GrantsPhasing hat er solange keine Kollision mit Gegnern
public sealed record ChannelSettings(float ManaPerSec, float TicksPerAttack = 1f, float TurnsPerTick = 1f, bool GrantsPhasing = false)
{
    public const float FullTurnDegrees = 360f;

    private const float MinTicksPerAttack = 0.1f;

    public static float GetAttacksPerSec(StatSheet attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return Math.Max(CombatRules.MinAttacksPerSecond, attacker.GetFinal(CombatStat.Attackspeed));
    }

    public double GetTicksPerSec(float attacksPerSec)
        => Math.Max(CombatRules.MinAttacksPerSecond, attacksPerSec) * Math.Max(MinTicksPerAttack, TicksPerAttack);

    public double GetIntervalSec(float attacksPerSec)
        => 1.0 / GetTicksPerSec(attacksPerSec);

    public double GetFirstTickSec(float attacksPerSec)
        => GetIntervalSec(attacksPerSec) * CombatRules.ActionImpactFraction;

    public float GetManaFor(double sec)
        => Math.Max(0f, ManaPerSec) * (float)Math.Max(0, sec);

    public float GetManaPerTick(float attacksPerSec)
        => GetManaFor(GetIntervalSec(attacksPerSec));

    //Zum Beginnen reicht das Mana bis zum ersten Tick
    public bool CanStart(float availableMana, float attacksPerSec)
        => ManaPerSec <= 0f || availableMana >= GetManaFor(GetFirstTickSec(attacksPerSec));

    //Weiter geht es nur, wenn das Mana den Verbrauch dieses Takts noch deckt. Sonst hielte die Regeneration den Wirbel bei null ewig am Leben
    public bool CanContinue(float availableMana, double deltaSec)
        => ManaPerSec <= 0f || availableMana >= GetManaFor(deltaSec);

    //TurnsPerTick Umdrehungen je Tick, mit schnellerem Angriffstempo also auch schneller. Unter 0 steht die Waffe still
    public double GetSpinDegreesPerSec(float attacksPerSec)
        => FullTurnDegrees * Math.Max(0f, TurnsPerTick) * GetTicksPerSec(attacksPerSec);
}
