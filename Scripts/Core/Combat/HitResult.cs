using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public enum HitAvoidance
{
    None,
    Missed,
    Dodged,
    Parried
}

//Das Ergebnis eines Treffers. Es wird einmal gewürfelt und danach nicht mehr verändert
public sealed record HitResult
{
    public DamageType   DamageType { get; init; }
    public SkillKind    SkillKind  { get; init; }
    public HitAvoidance Avoidance  { get; init; }
    public bool         WasBlocked { get; init; }
    public bool         IsCritical { get; init; }

    //Gewürfelter Schaden vor Krit, Bonus der Schadensart und Block
    public float RolledDamage { get; init; }

    //Schaden nach Krit, Bonus der Schadensart und Block, aber vor Rüstung oder Resistenz
    public float UnmitigatedDamage { get; init; }

    //Schaden, der vom Leben abgezogen wird
    public int FinalDamage { get; init; }

    //Der Statuseffekt, den der Treffer auslöst. Ohne Effekt null
    public StatusEffectApplication InflictedEffect { get; init; }

    public bool HasLanded => Avoidance == HitAvoidance.None;
}
