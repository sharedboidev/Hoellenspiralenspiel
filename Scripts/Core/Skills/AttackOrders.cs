using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Wie der Held einen Angriff ausführt: sofort aus dem Stand, Richtung Gegner unter der Maus oder Richtung Maus. Hinlaufen tut er nicht,
//er läuft selbst heran und kann dabei schlagen. Ein Nahkampfangriff braucht einen Gegner unter der Maus,
//mit der Taste zum Stehenbleiben schlägt er auch ins Leere Richtung Maus
public static class AttackOrders
{
    public static bool IsAllowed(bool isMelee, bool hasTarget, bool standsStill)
        => !isMelee || hasTarget || standsStill;

    //Ein Skill unterbricht das Laufen. Eine schon gehaltene Richtung wartet, bis der Angriff ausgeführt ist,
    //erst eine neu gedrückte Richtung bricht ihn ab und lässt den Helden wieder laufen
    public static bool MovementCancels(bool hasMovementInput, bool hasPendingAttack, bool isMovementJustPressed)
        => hasMovementInput && (!hasPendingAttack || isMovementJustPressed);

    //Solange ein Skill läuft, vom Ausholen, Wirken oder Laden bis zum Ende der Erholung, läuft der Held mit diesem Anteil seines Tempos.
    //Schneller als sonst wird er dabei nie
    public static float GetSkillWalkFactor(float speedPercent)
        => Math.Clamp(speedPercent / 100f, 0f, 1f);
}
