using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

public static class ModAimResolver
{
    //Der Boden ist isometrisch gestaucht, ein Kreis am Boden ist auf dem Bildschirm halb so hoch wie breit
    private const float GroundSquash = 0.5f;

    public static bool TryResolve(ModContext context, ModAim aim, float radius, out SkillAim result)
    {
        result = default;

        switch (aim)
        {
            case ModAim.Self:
                result = new SkillAim(context.Owner.BodyCenter);

                return true;

            case ModAim.Target:
                return TryAimAt(context.Owner.Target, out result);

            case ModAim.Other:
                return TryAimAt(context.OtherOrTarget, out result);

            case ModAim.RandomPointAroundSelf:
                result = new SkillAim(GetRandomPointAround(context.Owner.BodyCenter, radius));

                return true;

            case ModAim.RandomPointAroundTarget:
                if (!TryAimAt(context.Owner.Target, out var atTarget))
                    return false;

                result = new SkillAim(GetRandomPointAround(atTarget.Point, radius));

                return true;

            default: return false;
        }
    }

    public static Vector2 GetRandomPointAround(Vector2 center, float radius)
    {
        var angle    = GameRandom.Shared.NextFloat() * MathF.Tau;
        var distance = MathF.Sqrt(GameRandom.Shared.NextFloat()) * Math.Max(0f, radius);

        return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * GroundSquash) * distance;
    }

    private static bool TryAimAt(BaseUnit unit, out SkillAim result)
    {
        result = default;

        if (!GodotObject.IsInstanceValid(unit) || !unit.IsTargetable)
            return false;

        result = new SkillAim(unit.BodyCenter, unit);

        return true;
    }
}
