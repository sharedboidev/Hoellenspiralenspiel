namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public enum StatusEffectKind
{
    //Schaden über Zeit, ausgelöst durch Slash
    Bleed,

    //Stapelnder Schaden über Zeit, ausgelöst durch Fire
    Burn,

    //Aktionen schlagen fehl, ausgelöst durch Lightning
    Shock,

    //Bewegung und Angriffe werden langsamer, ausgelöst durch Frost
    Chill
}

public enum StackingRule
{
    //Mehrere Instanzen laufen nebeneinander, es wirkt nur die stärkste
    Strongest,

    //Alle Instanzen wirken zusammen
    Sum
}
