using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//In Metern, Y zeigt nach oben
public readonly record struct WorldPoint(float X, float Y, float Z);

//Hero ist der Ort der Füße des Helden
public readonly record struct WallFadeView(WorldPoint Hero, float HeroHeight, WorldPoint Camera, float Radius);

public readonly record struct WallPlane(float X, float Z, float NormalX, float NormalZ);

//Wie weit sich das Mauerwerk öffnet, wenn der Held hinter ihm steht: auf der Seite der Normale oder auf der anderen.
//0 bleibt zu, 1 gibt die Sicht ganz frei
public readonly record struct WallOpening(float HeroAtNormal, float HeroAtBack)
{
    public static WallOpening Open { get; } = new(WallOpeningRule.Open, WallOpeningRule.Open);
}

//Dieselbe Rechnung steht im Shader ps1_wall. Wer hier etwas ändert, ändert es auch dort
public static class WallFadeRule
{
    public const float EdgeMeters      = 1.5f;
    public const float SideRampMeters  = 0.5f;
    public const float CoverMeters     = 0.9f;
    public const float CoverEdgeMeters = 0.3f;
    public const float SeeThroughBelow = 0.55f;

    //1 ist Mauerwerk, 0 ist freie Sicht. Mauerwerk öffnet sich, wenn der Held hinter ihm steht, und nur im Umkreis des Helden.
    //Wo es den Helden selbst verdeckt, bleibt es nie ganz zu
    public static float GetOpacity(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view)
        => 1f - Math.Max(GetOpening(point, wall, opening, view), WallOpeningRule.Half * GetCover(point, view));

    public static bool IsSeeThrough(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view)
        => GetOpacity(point, wall, opening, view) < SeeThroughBelow;

    private static float GetOpening(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view)
    {
        var heroSide   = (view.Hero.X - wall.X) * wall.NormalX + (view.Hero.Z - wall.Z) * wall.NormalZ;
        var cameraSide = (view.Camera.X - wall.X) * wall.NormalX + (view.Camera.Z - wall.Z) * wall.NormalZ;

        if (view.Radius <= 0f || heroSide * cameraSide >= 0f)
            return 0f;

        var amount   = heroSide > 0f ? opening.HeroAtNormal : opening.HeroAtBack;
        var behind   = SmoothStep(0f, SideRampMeters, MathF.Abs(heroSide));
        var distance = MathF.Sqrt((point.X - view.Hero.X) * (point.X - view.Hero.X) + (point.Z - view.Hero.Z) * (point.Z - view.Hero.Z));
        var outside  = SmoothStep(view.Radius - EdgeMeters, view.Radius, distance);

        return amount * behind * (1f - outside);
    }

    //Wie sehr der Punkt den Helden verdeckt: 1 auf der Sichtlinie von der Kamera zum Körper des Helden, 0 abseits davon und hinter ihm
    private static float GetCover(WorldPoint point, WallFadeView view)
    {
        var toPointX = point.X - view.Camera.X;
        var toPointY = point.Y - view.Camera.Y;
        var toPointZ = point.Z - view.Camera.Z;
        var depth    = MathF.Sqrt(toPointX * toPointX + toPointY * toPointY + toPointZ * toPointZ);

        if (depth <= 0f || view.HeroHeight <= 0f)
            return 0f;

        var rayX = toPointX / depth;
        var rayY = toPointY / depth;
        var rayZ = toPointZ / depth;

        var toCameraX = view.Camera.X - view.Hero.X;
        var toCameraY = view.Camera.Y - view.Hero.Y;
        var toCameraZ = view.Camera.Z - view.Hero.Z;

        var along  = rayX * toCameraX + rayY * toCameraY + rayZ * toCameraZ;
        var spread = 1f - rayY * rayY;
        var height = spread < 0.0001f ? 0f : Math.Clamp((toCameraY - rayY * along) / spread, 0f, view.HeroHeight);
        var reach  = Math.Max(0f, rayY * height - along);

        if (depth >= reach)
            return 0f;

        var gapX = view.Camera.X + rayX * reach - view.Hero.X;
        var gapY = view.Camera.Y + rayY * reach - view.Hero.Y - height;
        var gapZ = view.Camera.Z + rayZ * reach - view.Hero.Z;
        var gap  = MathF.Sqrt(gapX * gapX + gapY * gapY + gapZ * gapZ);

        return 1f - SmoothStep(CoverMeters - CoverEdgeMeters, CoverMeters, gap);
    }

    private static float SmoothStep(float from, float to, float value)
    {
        if (to <= from)
            return value < from ? 0f : 1f;

        var share = Math.Clamp((value - from) / (to - from), 0f, 1f);

        return share * share * (3f - 2f * share);
    }
}
