using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills;

public record SkillExecutionDefinition(BaseUnit Caster, SkillResource Skill, SkillAim SkillAim);

public static class SkillExecutor
{
    //Die Kugeln springen aus der Mitte des Körpers
    private const float ScatterStartHeightShare  = 0.5f;

    //Der Schuss in den Himmel geht so weit aus der Senkrechten Richtung Ziel, wie der Held den Bogen hebt (Hero.SkyAimDegrees), und verlässt den Bogen in dieser Höhe
    private const float SkyShotTiltDegrees       = 20f;
    private const float SkyShotStartHeightMeters = 1.4f;
    private const float SkyShotStartAheadMeters  = 0.4f;

    private static readonly List<BaseUnit> UnitsInReach = new();

    //chargePercent zählt nur für einen geladenen Schuss: So weit war er beim Loslassen geladen
    public static void Execute(BaseUnit caster, SkillResource skill, SkillAim aim, float chargePercent = 0f)
    {
        var definition = skill.Definition;
        var cast       = new SkillCast(caster, HitRequests.ForSkill(caster.Stats, caster.Weapon, definition));

        switch (definition.Delivery)
        {
            case SkillDelivery.Weapon when caster.Weapon.IsRanged:
                LaunchProjectile(caster, cast, caster.WeaponProjectileScene, caster.Weapon.GetProjectile(), aim, caster.Weapon.ExtraProjectiles);

                break;
            case SkillDelivery.WeaponSweep or SkillDelivery.MeleeStrike or SkillDelivery.WeaponWhirl when caster.Weapon.IsRanged:
                GD.PushWarning($"{caster.Name} braucht für {definition.Name} eine Nahkampfwaffe.");

                break;
            case SkillDelivery.ArrowRain or SkillDelivery.ChargedShot when !caster.Weapon.IsRanged:
                GD.PushWarning($"{caster.Name} braucht für {definition.Name} einen Bogen.");

                break;
            case SkillDelivery.ArrowRain:
                RainArrows(caster, skill, aim);

                break;
            case SkillDelivery.ChargedShot:
                ShootCharged(caster, skill, aim, chargePercent);

                break;
            case SkillDelivery.Weapon or SkillDelivery.MeleeStrike:
                StrikeInMelee(caster, cast, skill, aim);

                break;
            case SkillDelivery.WeaponSweep:
                Sweep(caster, cast, skill.EffectScene, definition.Sweep, GetFacing(caster, aim));

                break;
            case SkillDelivery.WeaponWhirl:
                Whirl(caster, cast, skill, aim);

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
    private static void StrikeInMelee(BaseUnit caster, SkillCast cast, SkillResource skill, SkillAim aim)
    {
        var facing = GetFacing(caster, aim);
        var target = ChooseStrikeTarget(caster, cast, aim, facing);
        var hits   = target is not null;

        var radiusPx = hits
                           ? WorldScale.GroundDistancePx(caster.GlobalPosition, target.GlobalPosition)
                           : caster.BodyRadiusPx + caster.Weapon.Range;

        ShowSlash(caster, skill.EffectScene, facing, radiusPx);

        if (!hits)
            return;

        //Vor dem Treffer gemessen, er kann das Ziel töten
        var scatterOrigin = target.GlobalPosition + Vector3.Up * (target.PickHeight * ScatterStartHeightShare);
        var targetRadius  = target.BodyRadiusPx;

        if (cast.ApplyTo(target, true).HasLanded)
            Scatter(caster, skill, scatterOrigin, targetRadius);
    }

    //Der angeklickte Gegner in Reichweite hat Vorrang. Sonst trifft der Schlag den nächsten Gegner in der Zone vor dem Schlagenden
    private static BaseUnit ChooseStrikeTarget(BaseUnit caster, SkillCast cast, SkillAim aim, Vector3 facing)
    {
        if (aim.HasTarget && cast.CanHit(aim.Target) && caster.DistancePxTo(aim.Target) <= caster.Weapon.Reach)
            return aim.Target;

        var center = caster.GlobalPosition;

        UnitRegistry.FindNear(center, caster.BodyRadiusPx + caster.Weapon.Reach, UnitsInReach);
        UnitsInReach.RemoveAll(unit => !cast.CanHit(unit));

        return StrikeHitbox.PickNearest(UnitsInReach,
                                        unit => (WorldScale.ToPx(unit.GlobalPosition.X - center.X), WorldScale.ToPx(unit.GlobalPosition.Z - center.Z), unit.BodyRadiusPx),
                                        facing.X,
                                        facing.Z,
                                        caster.Weapon.Reach,
                                        caster.BodyRadiusPx);
    }

    //Die Kugeln springen aus dem Körper des Getroffenen und schlagen um die Stelle ein, an der er beim Treffer stand. Läuft er weg, kann er ihnen entkommen
    private static void Scatter(BaseUnit caster, SkillResource skill, Vector3 origin, float targetRadiusPx)
    {
        if (skill.Definition.Scatter is not { } scatter || skill is not AttackSkillResource { ScatterScene: { } scene })
            return;

        var hit    = HitRequests.ForAttack(caster.Stats, caster.Weapon, scatter.GetAttack(skill.Definition.Attack));
        var area   = new AreaSettings(scatter.ImpactRadius, 0f, scatter.FlightSec);
        var center = WorldScale.OnGround(origin);
        var count  = scatter.GetCount(BonusProjectiles.FromStats(caster.Stats));

        foreach (var (x, y) in scatter.PickLandings(count, targetRadiusPx, GameRandom.Shared))
        {
            var landing = center + new Vector3(WorldScale.ToMeters(x), 0f, WorldScale.ToMeters(y));
            var ball    = scene.Instantiate<LobbedArea>();

            ball.Launch(new SkillCast(caster, hit), area);
            ball.LaunchFrom(origin - landing);

            caster.GetParent().AddChild(ball);

            ball.GlobalPosition = landing;
        }
    }

    //Der Pfeil in den Himmel ist nur zu sehen. Die Pfeile des Regens fallen um die Stelle, auf die der Held beim Schuss zielte,
    //schräg aus seiner Richtung. Wer von dort wegläuft, kann ihnen entkommen
    private static void RainArrows(BaseUnit caster, SkillResource skill, SkillAim aim)
    {
        if (skill.Definition.Rain is not { } rain || skill is not AttackSkillResource { RainScene: { } scene })
        {
            GD.PushWarning($"{caster.Name} hat keine Pfeile für {skill.Definition.Name}.");

            return;
        }

        var center    = WorldScale.OnGround(aim.CurrentPoint);
        var direction = WorldScale.OnGround(center - caster.GlobalPosition);
        var facing    = direction.LengthSquared() > 0f ? direction.Normalized() : GetFacing(caster, aim);
        var count     = rain.GetCount(BonusProjectiles.ForBow(caster.Stats, caster.Weapon));
        var hit       = HitRequests.ForSkill(caster.Stats, caster.Weapon, skill.Definition);
        var delays    = rain.GetImpactDelays(count);
        var landings  = rain.PickLandings(count, GameRandom.Shared);

        ShowSkyShot(caster, skill.EffectScene, facing);

        for (var i = 0; i < count; i++)
        {
            var landing = center + new Vector3(WorldScale.ToMeters(landings[i].X), 0f, WorldScale.ToMeters(landings[i].Y));
            var arrow   = scene.Instantiate<FallingArea>();

            arrow.Launch(new SkillCast(caster, hit), new AreaSettings(rain.ImpactRadius, 0f, delays[i]));
            arrow.FallAlong(facing);

            caster.GetParent().AddChild(arrow);

            arrow.GlobalPosition = landing;
        }
    }

    //Der Pfeil der Waffe fliegt mit dem Anteil des Waffenschadens, den die Ladung ergibt. Über 100 % durchstößt er.
    //Das Projektil des Skills zeigt die Ladung, fehlt es, fliegt der gewöhnliche Pfeil der Waffe
    private static void ShootCharged(BaseUnit caster, SkillResource skill, SkillAim aim, float chargePercent)
    {
        if (skill.Definition.Charge is not { } charge || caster.Weapon.GetProjectile() is not { } projectile)
        {
            GD.PushWarning($"{caster.Name} kann {skill.Definition.Name} nicht laden.");

            return;
        }

        var attack = charge.GetAttack(skill.Definition.Attack, chargePercent);
        var cast   = new SkillCast(caster, HitRequests.ForAttack(caster.Stats, caster.Weapon, attack));
        var scene  = skill.EffectScene ?? caster.WeaponProjectileScene;
        var share  = charge.GetShownShare(chargePercent);
        var pierce = charge.Pierces(chargePercent);

        LaunchProjectile(caster,
                         cast,
                         scene,
                         projectile with { Pierces = pierce },
                         aim,
                         caster.Weapon.ExtraProjectiles,
                         arrow => (arrow as ChargedArrow)?.ShowCharge(share, pierce));
    }

    private static void ShowSkyShot(BaseUnit caster, PackedScene scene, Vector3 facing)
    {
        if (scene is null)
            return;

        var tilt  = Mathf.DegToRad(SkyShotTiltDegrees);
        var arrow = scene.Instantiate<SkyArrow>();

        arrow.Launch(Vector3.Up * Mathf.Cos(tilt) + facing * Mathf.Sin(tilt));

        caster.GetParent().AddChild(arrow);

        arrow.GlobalPosition = caster.GlobalPosition + Vector3.Up * SkyShotStartHeightMeters + facing * SkyShotStartAheadMeters;
    }

    //Ein Tick des Wirbels trifft jeden im Kreis. Zu sehen gibt es je Tick nichts Eigenes: Der Schweif aus ShowWhirlTrail läuft die ganze Zeit mit der Waffe
    private static void Whirl(BaseUnit caster, SkillCast cast, SkillResource skill, SkillAim aim)
    {
        var definition = skill.Definition;

        if (definition.Sweep is not { } sweep)
        {
            GD.PushWarning($"{caster.Name} hat keinen Kreis für {definition.Name}.");

            return;
        }

        Sweep(caster, cast, null, sweep with { ArcDegrees = SweepSettings.FullCircleDegrees }, GetFacing(caster, aim));
    }

    //Der Schweif des Wirbels hängt am Wirbelnden und folgt seiner Waffe, bis er ihn mit Dismiss zurücknimmt. Sein Radius ist der Kreis der Ticks
    public static WhirlTrail ShowWhirlTrail(BaseUnit caster, SkillResource skill)
    {
        if (skill.Definition.Sweep is not { } sweep)
            return null;

        return WhirlTrail.Show(skill.EffectScene, caster, WorldScale.ToMeters(caster.BodyRadiusPx + sweep.GetReach(caster.Weapon)));
    }

    //Der Bogen zeigt, wohin der Schlagende beim Ausholen schaute. Wer währenddessen hinter ihn läuft, entgeht ihm
    private static void Sweep(BaseUnit caster, SkillCast cast, PackedScene slashScene, SweepSettings sweep, Vector3 facing)
    {
        if (sweep is null)
            return;

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

    //Erhöhtes Projektiltempo lässt die Reichweite gleich. Weitere Pfeile einer Waffe zählen nur für ihre eigenen Angriffe.
    //prepare richtet jedes Projektil vor dem Einhängen ein, etwa mit seiner Ladung
    private static void LaunchProjectile(BaseUnit               caster,
                                         SkillCast              cast,
                                         PackedScene            scene,
                                         ProjectileSettings     settings,
                                         SkillAim               aim,
                                         int                    extraProjectiles = 0,
                                         Action<SkillProjectile> prepare          = null)
    {
        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat kein Projektil für seinen Skill.");

            return;
        }

        var origin    = WorldScale.OnGround(caster.GlobalPosition);
        var direction = WorldScale.OnGround(aim.CurrentPoint) - origin;
        var count     = 1 + BonusProjectiles.FromStats(caster.Stats) + Math.Max(0, extraProjectiles);
        var speedup   = caster.Stats.GetTotalMultiplier(CombatStat.ProjectileSpeed);

        if (speedup > 0f && !speedup.Equals(1f))
            settings = settings with { Speed = settings.Speed * speedup, LifetimeSec = settings.LifetimeSec / speedup };

        for (var i = 0; i < count; i++)
        {
            var projectile = scene.Instantiate<SkillProjectile>();
            var spread     = Mathf.DegToRad(ProjectileSpread.GetOffsetDegrees(i, count));

            projectile.Launch(cast, settings, scene, direction.Rotated(Vector3.Up, spread));
            prepare?.Invoke(projectile);

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
