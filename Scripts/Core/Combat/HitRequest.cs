namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record HitRequest(float      MinDamage,
                                float      MaxDamage,
                                DamageType DamageType,
                                SkillKind  SkillKind,
                                float      HitChance           = CombatRules.BaseHitChance,
                                float      CriticalHitChance   = 0f,
                                float      CriticalDamageBonus = CombatRules.BaseCriticalDamage)
{
    //Zusatzschaden der Elemente neben dem Hauptteil. Er trifft mit denselben Würfen und jedes Element wird für sich gemindert
    public PerElement<DamageRange> AddedDamage { get; init; }

    //Faktor des Angreifers auf jeden Effekt, der Schaden über Zeit macht
    public float DamageOverTimeMultiplier { get; init; } = 1f;

    //Dazu der Faktor der Schadensart des Effekts
    public DamageOverTimeByType DamageOverTimeByType { get; init; } = DamageOverTimeByType.None;

    //Pierce trifft nur halb so oft, außer der Angriff ist davon ausgenommen
    public bool IgnoresPierceHitPenalty { get; init; }
}
