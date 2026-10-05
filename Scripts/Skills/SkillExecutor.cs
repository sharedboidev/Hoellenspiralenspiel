using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills;

public static class SkillExecutor
{
    private static readonly List<BaseUnit> UnitsInReach = new();

    public static void Execute(BaseUnit caster, SkillResource skill, SkillAim aim)
    {
        var definition = skill.Definition;
        var cast       = new SkillCast(caster, HitRequests.ForSkill(caster.Stats, caster.Weapon, definition));

        switch (definition.Delivery)
        {
            case SkillDelivery.Weapon when caster.Weapon.IsRanged:
                LaunchProjectile(caster, cast, caster.WeaponProjectileScene, caster.Weapon.GetProjectile(), aim, caster.Weapon.ExtraProjectiles);

                break;
            case SkillDelivery.Weapon:
                StrikeInMelee(caster, cast, skill.EffectScene, aim);

                break;
            case SkillDelivery.WeaponSweep when caster.Weapon.IsRanged:
                GD.PushWarning($"{caster.Name} braucht für {definition.Name} eine Nahkampfwaffe.");

                break;
            case SkillDelivery.WeaponSweep:
                Sweep(caster, cast, skill.EffectScene, definition.Sweep, aim);

                break;
            case SkillDelivery.Projectile:
                LaunchProjectile(caster, cast, skill.EffectScene, definition.Projectile, aim);

                break;
            case SkillDelivery.AreaAroundCaster:
                LaunchArea(caster, cast, skill.EffectScene, StartAtBodyEdge(definition.Area, caster), caster.GlobalPosition);

                break;
            case SkillDelivery.AreaAtPoint:
                LaunchArea(caster, cast, skill.EffectScene, definition.Area, aim.CurrentPoint);

                break;
        }
    }

    //Der Hieb ist auch zu sehen, wenn er ins Leere geht. Trifft er, läuft er durch das Ziel
    private static void StrikeInMelee(BaseUnit caster, SkillCast cast, PackedScene slashScene, SkillAim aim)
    {
        var hits = aim.HasTarget && cast.CanHit(aim.Target) && caster.DistancePxTo(aim.Target) <= caster.Weapon.Reach;

        var radiusPx = hits
                           ? WorldScale.GroundDistancePx(caster.GlobalPosition, aim.Target.GlobalPosition)
                           : caster.BodyRadiusPx + caster.Weapon.Range;

        ShowSlash(caster, slashScene, GetFacing(caster, aim), radiusPx);

        if (hits)
            cast.ApplyTo(aim.Target, true);
    }

    //Der Bogen zeigt, wohin der Schlagende beim Ausholen schaute. Wer währenddessen hinter ihn läuft, entgeht ihm
    private static void Sweep(BaseUnit caster, SkillCast cast, PackedScene slashScene, SweepSettings sweep, SkillAim aim)
    {
        if (sweep is null)
            return;

        var facing  = GetFacing(caster, aim);
        var reachPx = sweep.GetReach(caster.Weapon);
        var center  = caster.GlobalPosition;

        ShowSlash(caster, slashScene, facing, caster.BodyRadiusPx + reachPx, sweep.ArcDegrees);

        UnitRegistry.FindNear(center, caster.BodyRadiusPx + reachPx, UnitsInReach);

        foreach (var unit in UnitsInReach)
        {
            if (!cast.CanHit(unit))
                continue;

            var offset = unit.GlobalPosition - center;

            if (sweep.Reaches(WorldScale.ToPx(offset.X), WorldScale.ToPx(offset.Z), facing.X, facing.Z, reachPx, caster.BodyRadiusPx, unit.BodyRadiusPx))
                cast.ApplyTo(unit, true);
        }
    }

    private static Vector3 GetFacing(BaseUnit caster, SkillAim aim)
    {
        if (caster.FacingDirection != Vector3.Zero)
            return caster.FacingDirection;

        return WorldScale.OnGround(aim.CurrentPoint - caster.GlobalPosition).Normalized();
    }

    private static void ShowSlash(BaseUnit caster, PackedScene scene, Vector3 facing, float radiusPx, float arcDegrees = 0f)
    {
        if (scene is null)
            return;

        var slash = scene.Instantiate<MeleeSlash>();

        slash.Launch(facing, WorldScale.ToMeters(radiusPx), arcDegrees);

        caster.GetParent().AddChild(slash);

        slash.GlobalPosition = WorldScale.OnGround(caster.GlobalPosition);
    }

    //Erhöhtes Projektiltempo lässt die Reichweite gleich. Weitere Pfeile einer Waffe zählen nur für ihre eigenen Angriffe
    private static void LaunchProjectile(BaseUnit caster, SkillCast cast, PackedScene scene, ProjectileSettings settings, SkillAim aim, int extraProjectiles = 0)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat kein Projektil für seinen Skill.");

            return;
        }

        var origin    = WorldScale.OnGround(caster.GlobalPosition);
        var direction = WorldScale.OnGround(aim.CurrentPoint) - origin;
        var count     = Math.Max(1, caster.Stats.GetFinalWhole(CombatStat.ProjectileCount)) + Math.Max(0, extraProjectiles);
        var speedup   = caster.Stats.GetTotalMultiplier(CombatStat.ProjectileSpeed);

        if (speedup > 0f && !speedup.Equals(1f))
            settings = settings with { Speed = settings.Speed * speedup, LifetimeSec = settings.LifetimeSec / speedup };

        for (var i = 0; i < count; i++)
        {
            var projectile = scene.Instantiate<SkillProjectile>();
            var spread     = Mathf.DegToRad(ProjectileSpread.GetOffsetDegrees(i, count));

            projectile.Launch(cast, settings, scene, direction.Rotated(Vector3.Up, spread));

            caster.GetParent().AddChild(projectile);

            projectile.GlobalPosition = origin;
        }
    }

    //Eine Fläche um den Wirkenden beginnt wie jede Reichweite an seinem Rand
    private static AreaSettings StartAtBodyEdge(AreaSettings settings, BaseUnit caster)
        => settings is null ? null : settings with { Radius = settings.Radius + caster.BodyRadiusPx };

    private static void LaunchArea(BaseUnit caster, SkillCast cast, PackedScene scene, AreaSettings settings, Vector3 center)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat keine Fläche für seinen Skill.");

            return;
        }

        var area = scene.Instantiate<SkillArea>();

        area.Launch(cast, settings);

        caster.GetParent().AddChild(area);

        area.GlobalPosition = WorldScale.OnGround(center);
    }
}
