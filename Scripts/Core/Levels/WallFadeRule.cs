using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//In Metern, Y zeigt nach oben
public readonly record struct WorldPoint(float X, float Y, float Z);

//Hero ist der Ort der Füße des Helden
public readonly record struct WallFadeView(WorldPoint Hero, float HeroHeight, WorldPoint Camera, float Radius);

public readonly record struct WallPlane(float X, float Z, float NormalX, float NormalZ);

//Die Mittellinie eines Mauerstücks am Boden, von einem Ende zum anderen
public readonly record struct WallLine(float X0, float Z0, float X1, float Z1)
{
    public float DistanceTo(float x, float z)
    {
        var dX     = X1 - X0;
        var dZ     = Z1 - Z0;
        var length = dX * dX + dZ * dZ;
        var share  = length <= 0f ? 0f : Math.Clamp(((x - X0) * dX + (z - Z0) * dZ) / length, 0f, 1f);
        var gapX   = x - (X0 + dX * share);
        var gapZ   = z - (Z0 + dZ * share);

        return MathF.Sqrt(gapX * gapX + gapZ * gapZ);
    }
}

//Ein Mauerstück, wie die Auswahl der Blocker es braucht
public readonly record struct WallPiece(WallPlane Plane, WallLine Line, WallOpening Opening);

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

    //Das eigene Mauerstück trifft die Sichtlinie an ihrem Ende, es hält sie nicht auf
    private const float OwnWallAt = 0.999f;

    //1 ist Mauerwerk, 0 ist freie Sicht. Mauerwerk öffnet sich, wenn der Held hinter ihm steht, und nur im Umkreis des Helden.
    //Wo es den Helden selbst verdeckt, bleibt es nie ganz zu. Die Mittellinien der Mauerstücke in seinem Umkreis halten die Sicht auf
    public static float GetOpacity(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view, IReadOnlyList<WallLine> blockers = null)
        => 1f - Math.Max(GetOpening(point, wall, opening, view, blockers), WallOpeningRule.Half * GetCover(point, view));

    public static bool IsSeeThrough(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view, IReadOnlyList<WallLine> blockers = null)
        => GetOpacity(point, wall, opening, view, blockers) < SeeThroughBelow;

    //Wahr, wenn sich das Stück für den Helden überhaupt öffnet: Er steht hinter ihm, und sein Raum lässt es zu
    public static bool Opens(WallPlane wall, WallOpening opening, WallFadeView view)
    {
        var heroSide   = SideOf(view.Hero, wall);
        var cameraSide = SideOf(view.Camera, wall);

        if (view.Radius <= 0f || heroSide * cameraSide >= 0f)
            return false;

        return (heroSide > 0f ? opening.HeroAtNormal : opening.HeroAtBack) > 0f;
    }

    //Die Stücke im Lichtradius des Helden, die seine Sicht aufhalten, die nächsten zuerst und höchstens max.
    //Ein Stück, das sich selbst öffnet, hält nichts auf: Was dahinter liegt, sieht der Held durch es hindurch
    public static List<WallLine> SelectBlockers(IEnumerable<WallPiece> pieces, WallFadeView view, int max)
        => pieces.Where(piece => !Opens(piece.Plane, piece.Opening, view))
                 .Select(piece => (piece.Line, Distance: piece.Line.DistanceTo(view.Hero.X, view.Hero.Z)))
                 .Where(entry => entry.Distance <= view.Radius)
                 .OrderBy(entry => entry.Distance)
                 .Take(max)
                 .Select(entry => entry.Line)
                 .ToList();

    //Wahr, wenn eines der Stücke zwischen den beiden Punkten steht. Ein Stück, das genau am Ziel endet oder liegt, zählt nicht
    public static bool IsBlocked(WorldPoint from, WorldPoint to, IReadOnlyList<WallLine> blockers)
    {
        if (blockers is null)
            return false;

        var dX = to.X - from.X;
        var dZ = to.Z - from.Z;

        foreach (var line in blockers)
        {
            var eX          = line.X1 - line.X0;
            var eZ          = line.Z1 - line.Z0;
            var denominator = dX * eZ - dZ * eX;

            if (MathF.Abs(denominator) < 0.000001f)
                continue;

            var aX    = line.X0 - from.X;
            var aZ    = line.Z0 - from.Z;
            var along = (aX * eZ - aZ * eX) / denominator;
            var where = (aX * dZ - aZ * dX) / denominator;

            if (along > 0f && along < OwnWallAt && where >= 0f && where <= 1f)
                return true;
        }

        return false;
    }

    //Der Punkt auf der Mittellinie des Stücks, der Held sieht ihn von seiner Seite aus
    public static WorldPoint OnCenterLine(WorldPoint point, WallPlane wall)
    {
        var side = (point.X - wall.X) * wall.NormalX + (point.Z - wall.Z) * wall.NormalZ;

        return new WorldPoint(point.X - side * wall.NormalX, point.Y, point.Z - side * wall.NormalZ);
    }

    private static float SideOf(WorldPoint point, WallPlane wall)
        => (point.X - wall.X) * wall.NormalX + (point.Z - wall.Z) * wall.NormalZ;

    private static float GetOpening(WorldPoint point, WallPlane wall, WallOpening opening, WallFadeView view, IReadOnlyList<WallLine> blockers)
    {
        var heroSide   = SideOf(view.Hero, wall);
        var cameraSide = SideOf(view.Camera, wall);

        if (view.Radius <= 0f || heroSide * cameraSide >= 0f)
            return 0f;

        var amount   = heroSide > 0f ? opening.HeroAtNormal : opening.HeroAtBack;
        var behind   = SmoothStep(0f, SideRampMeters, MathF.Abs(heroSide));
        var distance = MathF.Sqrt((point.X - view.Hero.X) * (point.X - view.Hero.X) + (point.Z - view.Hero.Z) * (point.Z - view.Hero.Z));
        var outside  = SmoothStep(view.Radius - EdgeMeters, view.Radius, distance);

        if (outside >= 1f || IsBlocked(view.Hero, OnCenterLine(point, wall), blockers))
            return 0f;

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
