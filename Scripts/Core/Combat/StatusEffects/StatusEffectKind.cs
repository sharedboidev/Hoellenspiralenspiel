namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public enum StatusEffectKind
{
    Bleed,
    Burn,
    Shock,
    Chill,

    //Kein Ailment eines Treffers: Brittle Mist legt es. Die Einheit nimmt mehr physischen Schaden, Feuer löst es
    Brittle,

    //Kein Ailment: Wer auf einem Lavariss steht, brennt, solange er darauf steht. Mehrere Risse zählen nicht doppelt
    Lava
}

public enum StackingRule
{
    Strongest,
    Sum
}
