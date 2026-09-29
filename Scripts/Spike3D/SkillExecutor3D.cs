using System;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public static class SkillExecutor3D
{
    public static void Execute(Unit3D caster, SkillResource skill, Aim3D aim)
    {
        var definition = skill.Definition;
        var cast       = new Cast3D(caster, HitRequests.ForSkill(caster.Stats, caster.Weapon, definition));

        switch (definition.Delivery)
        {
            case SkillDelivery.Weapon when caster.Weapon.IsRanged:
                LaunchProjectile(caster, cast, caster.WeaponProjectileScene, caster.Weapon.GetProjectile(), aim);

                break;
            case SkillDelivery.Weapon:
                StrikeInMelee(caster, cast, aim);

                break;
            case SkillDelivery.Projectile:
                LaunchProjectile(caster, cast, EffectScenes3D.Find(skill.Id), definition.Projectile, aim);

                break;
            default:
                GD.PushWarning($"Flächen gibt es in 3D noch nicht, {skill.NameOrId} bleibt ohne Wirkung.");

                break;
        }
    }

    private static void StrikeInMelee(Unit3D caster, Cast3D cast, Aim3D aim)
    {
        if (!aim.HasTarget || !cast.CanHit(aim.Target) || caster.DistancePxTo(aim.Target) > caster.Weapon.Reach)
            return;

        cast.ApplyTo(aim.Target);
    }

    private static void LaunchProjectile(Unit3D caster, Cast3D cast, PackedScene scene, ProjectileSettings settings, Aim3D aim)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat kein Projektil für seinen Skill.");

            return;
        }

        var origin    = WorldScale.OnGround(caster.GlobalPosition);
        var direction = WorldScale.OnGround(aim.CurrentPoint) - origin;
        var count     = Math.Max(1, caster.Stats.GetFinalWhole(CombatStat.ProjectileCount));

        for (var i = 0; i < count; i++)
        {
            var projectile = scene.Instantiate<Projectile3D>();
            var spread     = Mathf.DegToRad(ProjectileSpread.GetOffsetDegrees(i, count));

            projectile.Launch(cast, settings, scene, direction.Rotated(Vector3.Up, spread));

            caster.GetParent().AddChild(projectile);

            projectile.GlobalPosition = origin;
        }
    }
}
