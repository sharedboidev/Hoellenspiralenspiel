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

    //Der Hauptteil oder ein Zusatzschaden hat diese Art
    public bool Deals(DamageType damageType)
        => DamageType == damageType || damageType.IsElemental() && !AddedDamage[damageType].IsEmpty;

    //Derselbe Treffer mit weniger Schaden, etwa nach dem Sprung eines Blitzes. Treffer- und Kritchance bleiben
    public HitRequest Times(float factor)
        => this with
        {
            MinDamage = MinDamage * factor,
            MaxDamage = MaxDamage * factor,
            AddedDamage = AddedDamage.Select((_, range) => range.Times(factor))
        };
}
