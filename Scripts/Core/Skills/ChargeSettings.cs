using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Schuss, der stärker wird, je länger der Spieler die Taste hält. Die Ladung steht in Prozent: Bei 100 ist der Schuss voll,
//darüber durchstößt er jedes Ziel auf seiner Bahn. Der Waffenschaden des Skills gilt bei 100 % und wächst linear mit der Ladung.
//Unter der Mindestladung verpufft der Schuss. Wer das Maximum zu lange hält, verschießt nichts und bekommt eine Abklingzeit.
//RatePerSec gilt bei unverändertem Angriffstempo: Erhöhtes Angriffstempo lädt im selben Verhältnis schneller.
//Beim Laden läuft der Held langsamer wie bei jedem Skill, siehe AttackOrders.GetSkillWalkFactor
public sealed record ChargeSettings(float  RatePerSec,
                                    float  MinPercent,
                                    float  MaxPercent,
                                    float  OverholdSec,
                                    double OverholdCooldownSec)
{
    public const float FullPercent = 100f;

    private const float MinRateFactor = 0.1f;

    //Wie das Zaubertempo die Wirkzeit teilt, teilt das Angriffstempo die Ladezeit: 50 % mehr Tempo lädt anderthalbmal so schnell
    public static float GetRateFactor(StatSheet attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);

        return Math.Max(MinRateFactor, attacker.GetTotalMultiplier(CombatStat.Attackspeed));
    }

    public float Advance(float percent, double deltaSec, float rateFactor = 1f)
        => Math.Clamp(percent + Math.Max(0f, RatePerSec) * Math.Max(0f, rateFactor) * (float)Math.Max(0, deltaSec), 0f, Math.Max(0f, MaxPercent));

    public bool CanFire(float percent)
        => percent >= MinPercent;

    public bool IsAtMax(float percent)
        => percent >= MaxPercent;

    public bool Pierces(float percent)
        => percent > FullPercent;

    //So lange darf das Maximum gehalten werden, danach verpufft der Schuss
    public bool IsOverheld(float percent, double secAtMax)
        => IsAtMax(percent) && secAtMax >= OverholdSec;

    //Ein Drittel der Ladung ergibt ein Drittel des Waffenschadens, 150 % das Anderthalbfache
    public float GetDamageFactor(float percent)
        => Math.Max(0f, percent) / FullPercent;

    public AttackDefinition GetAttack(AttackDefinition attack, float percent)
    {
        ArgumentNullException.ThrowIfNull(attack);

        return attack with { WeaponDamagePercent = attack.WeaponDamagePercent * GetDamageFactor(percent) };
    }

    public double GetSecToReach(float percent, float rateFactor = 1f)
    {
        var rate = RatePerSec * rateFactor;

        return rate <= 0f ? double.PositiveInfinity : Math.Max(0f, percent) / rate;
    }

    //Für die Darstellung: 0 unterhalb der Mindestladung, dann wachsend bis 1 am Maximum
    public float GetShownShare(float percent)
    {
        if (percent < MinPercent)
            return 0f;

        var span = MaxPercent - MinPercent;

        return span <= 0f ? 1f : Math.Clamp((percent - MinPercent) / span, 0f, 1f);
    }
}
