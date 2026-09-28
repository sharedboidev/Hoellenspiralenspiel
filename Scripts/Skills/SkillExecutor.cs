using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

public static class SkillExecutor
{
    public static void Execute(BaseUnit caster, SkillResource skill, SkillAim aim)
    {
        var definition = skill.Definition;
        var cast       = new SkillCast(caster.Faction, HitRequests.ForSkill(caster.Stats, caster.Weapon, definition));

        switch (definition.Delivery)
        {
            case SkillDelivery.Weapon when caster.Weapon.IsRanged:
                LaunchProjectile(caster, cast, caster.WeaponProjectileScene, caster.Weapon.GetProjectile(), aim);

                break;
            case SkillDelivery.Weapon:
                StrikeInMelee(caster, cast, aim);

                break;
            case SkillDelivery.Projectile:
                LaunchProjectile(caster, cast, skill.EffectScene, definition.Projectile, aim);

                break;
            case SkillDelivery.AreaAroundCaster:
                LaunchArea(caster, cast, skill.EffectScene, definition.Area, caster.BodyCenter);

                break;
            case SkillDelivery.AreaAtPoint:
                LaunchArea(caster, cast, skill.EffectScene, definition.Area, aim.CurrentPoint);

                break;
        }
    }

    private static void StrikeInMelee(BaseUnit caster, SkillCast cast, SkillAim aim)
    {
        if (!aim.HasTarget || !cast.CanHit(aim.Target) || caster.DistanceTo(aim.Target) > caster.Weapon.Reach)
            return;

        cast.ApplyTo(aim.Target);
    }

    private static void LaunchProjectile(BaseUnit caster, SkillCast cast, PackedScene scene, ProjectileSettings settings, SkillAim aim)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat kein Projektil für seinen Skill.");

            return;
        }

        var origin     = caster.BodyCenter;
        var projectile = scene.Instantiate<SkillProjectile>();

        projectile.Launch(cast, settings, scene, aim.CurrentPoint - origin);

        caster.GetParent().AddChild(projectile);

        projectile.GlobalPosition = origin;
    }

    private static void LaunchArea(BaseUnit caster, SkillCast cast, PackedScene scene, AreaSettings settings, Vector2 center)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat keine Fläche für seinen Skill.");

            return;
        }

        var area = scene.Instantiate<SkillArea>();

        area.Launch(cast, settings);

        caster.GetParent().AddChild(area);

        area.GlobalPosition = center;
    }
}
