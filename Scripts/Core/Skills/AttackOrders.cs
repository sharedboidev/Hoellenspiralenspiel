namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public enum AttackOrder
{
    None,
    InPlace,
    Approach
}

//Wie der Held einen Angriff ausführt. Ein Gegner unter der Maus ist das Ziel, zu dem er hinläuft.
//Hält der Spieler die Taste zum Stehenbleiben, schlägt oder schießt er sofort aus dem Stand Richtung Maus
public static class AttackOrders
{
    public static AttackOrder Choose(bool isMelee, bool hasTarget, bool standsStill)
    {
        if (standsStill)
            return AttackOrder.InPlace;

        if (hasTarget)
            return AttackOrder.Approach;

        return isMelee ? AttackOrder.None : AttackOrder.InPlace;
    }
}
