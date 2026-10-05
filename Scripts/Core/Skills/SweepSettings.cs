using System;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Schlag der Nahkampfwaffe, der jeden im Bogen vor dem Schlagenden trifft. Wie beim einfachen Schlag zählt der Abstand von Rand zu Rand
public sealed record SweepSettings(float ArcDegrees, float RangeFactor = 1f)
{
    public const float FullCircleDegrees = 360f;

    public float GetEngageRange(WeaponProfile weapon)
        => Math.Max(0f, weapon.Range * RangeFactor);

    public float GetReach(WeaponProfile weapon)
        => Math.Max(0f, weapon.Reach * RangeFactor);

    //Alles in Pixeln auf dem Boden. Getroffen ist, wessen Körper in den Bogen ragt, auch wenn seine Mitte knapp daneben liegt
    public bool Reaches(float offsetX, float offsetY, float facingX, float facingY, float reachPx, float casterRadiusPx, float targetRadiusPx)
    {
        var distance = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);

        if (distance - casterRadiusPx - targetRadiusPx > reachPx)
            return false;

        var facingLength = MathF.Sqrt(facingX * facingX + facingY * facingY);

        if (ArcDegrees >= FullCircleDegrees || distance <= targetRadiusPx || facingLength <= 0f)
            return true;

        var cosine    = Math.Clamp((offsetX * facingX + offsetY * facingY) / (distance * facingLength), -1f, 1f);
        var angle     = MathF.Acos(cosine) * 180f / MathF.PI;
        var bodyAngle = MathF.Asin(Math.Min(1f, targetRadiusPx / distance)) * 180f / MathF.PI;

        return angle <= ArcDegrees / 2f + bodyAngle;
    }
}
