namespace Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

public enum StatusEffectKind
{
    Bleed,
    Burn,
    Shock,
    Chill,

    //Kein Ailment eines Treffers: Brittle Mist legt es. Die Einheit nimmt mehr physischen Schaden, Feuer löst es
    Brittle
}

public enum StackingRule
{
    Strongest,
    Sum
}
