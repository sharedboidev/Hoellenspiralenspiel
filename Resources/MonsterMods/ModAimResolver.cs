using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

public static class ModAimResolver
{
    public static bool TryResolve(ModContext context, ModAim aim, float radius, out SkillAim result)
    {
        result = default;

        switch (aim)
        {
            case ModAim.Self:
                result = new SkillAim(context.Owner.GlobalPosition);

                return true;

            case ModAim.Target:
                return TryAimAt(context.Owner.Target, out result);

            case ModAim.Other:
                return TryAimAt(context.OtherOrTarget, out result);

            case ModAim.RandomPointAroundSelf:
                result = new SkillAim(GetRandomPointAround(context.Owner.GlobalPosition, radius));

                return true;

            case ModAim.RandomPointAroundTarget:
                if (!TryAimAt(context.Owner.Target, out var atTarget))
                    return false;

                result = new SkillAim(GetRandomPointAround(atTarget.CurrentPoint, radius));

                return true;

            default: return false;
        }
    }

    public static Vector3 GetRandomPointAround(Vector3 center, float radiusPx)
    {
        var angle    = GameRandom.Shared.NextFloat() * MathF.Tau;
        var distance = WorldScale.ToMeters(MathF.Sqrt(GameRandom.Shared.NextFloat()) * Math.Max(0f, radiusPx));

        return WorldScale.OnGround(center) + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * distance;
    }

    private static bool TryAimAt(BaseUnit unit, out SkillAim result)
    {
        result = default;

        if (!GodotObject.IsInstanceValid(unit) || !unit.IsTargetable)
            return false;

        result = new SkillAim(unit.GlobalPosition, unit);

        return true;
    }
}
