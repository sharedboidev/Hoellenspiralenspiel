namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public static class WallOpeningRule
{
    public const int NoRoom = -1;

    public const float Closed = 0f;
    public const float Half   = 0.5f;
    public const float Open   = 1f;

    //Der Held steht hinter der Mauer, sie liegt also zwischen ihm und der Kamera. Vorn ist die Seite der Kamera.
    //Ein Raum, in dem der Held nicht steht, bleibt verschlossen: Seine vorderen Mauern bleiben zu,
    //seine hinteren öffnen sich nur halb, damit der Held dahinter zu sehen ist und der Raum davor nicht frei liegt
    public static float Decide(int roomInFront, int roomBehind, int roomOfHero)
    {
        if (roomBehind != NoRoom && roomBehind != roomOfHero)
            return Closed;

        if (roomInFront != NoRoom && roomInFront != roomOfHero)
            return Half;

        return Open;
    }

    //Steht der Held auf der Seite der Normale, liegt hinter der Mauer, was auf dieser Seite liegt
    public static WallOpening DecideForBothSides(int roomAtNormal, int roomAtBack, int roomOfHero)
        => new(Decide(roomAtBack, roomAtNormal, roomOfHero), Decide(roomAtNormal, roomAtBack, roomOfHero));
}
