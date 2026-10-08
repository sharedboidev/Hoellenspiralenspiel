using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills;

//Was ein Skill beim Ausführen nutzt, zunächst alles aus dem Skill selbst. Wer davon abweicht, etwa der Schuss mit dem Pfeil der Waffe,
//legt sich mit with eine Kopie an. Die Definition des Skills teilen sich alle, an ihr ändert keiner etwas
public record SkillExecutionDefinition(BaseUnit Caster, SkillResource Skill, SkillAim SkillAim)
{
    public SkillDefinition SkillDefinition => Skill.Definition;

    public SkillCast Cast { get; init; } = new(Caster, HitRequests.ForSkill(Caster.Stats, Caster.Weapon, Skill.Definition));

    public PackedScene EffectScene { get; init; } = Skill.EffectScene;

    public ProjectileSettings Projectile { get; init; } = Skill.Definition.Projectile;

    public AreaSettings Area { get; init; } = Skill.Definition.Area;

    public SweepSettings Sweep { get; init; } = Skill.Definition.Sweep;
}

public static class SkillExecutor
{
    //Die Kugeln springen aus der Mitte des Körpers
    private const float ScatterStartHeightShare  = 0.5f;

    //Der Schuss in den Himmel geht so weit aus der Senkrechten Richtung Ziel, wie der Held den Bogen hebt (Hero.SkyAimDegrees), und verlässt den Bogen in dieser Höhe
    private const float SkyShotTiltDegrees       = 20f;
    private const float SkyShotStartHeightMeters = 1.4f;
    private const float SkyShotStartAheadMeters  = 0.4f;

    //Der Blitz verlässt den Wirkenden in dieser Höhe und trifft die Mitte des Körpers
    private const float ChainStartHeightShare = 0.7f;

    public const string BrittleShatterPath = "res://Resources/Skills/Effects/brittle_shatter.tres";

    private static readonly List<BaseUnit> UnitsInReach = new();
    private static readonly List<BaseUnit> ChainLinks   = new();

    private static SkillResource brittleShatter;

    //chargePercent zählt nur für einen geladenen Schuss: So weit war er beim Loslassen geladen
    public static void Execute(SkillExecutionDefinition executionDefinition, float chargePercent = 0f)
    {
        switch (executionDefinition.SkillDefinition.Delivery)
        {
            case SkillDelivery.Weapon when executionDefinition.Caster.Weapon.IsRanged:
                LaunchProjectile(executionDefinition with
                                 {
                                     EffectScene = executionDefinition.Caster.WeaponProjectileScene,
                                     Projectile = executionDefinition.Caster.Weapon.GetProjectile()
                                 },
                                 executionDefinition.Caster.Weapon.ExtraProjectiles);

                break;
            case SkillDelivery.WeaponSweep or SkillDelivery.MeleeStrike or SkillDelivery.WeaponWhirl when executionDefinition.Caster.Weapon.IsRanged:
                GD.PushWarning($"{executionDefinition.Caster.Name} braucht für {executionDefinition.SkillDefinition.Name} eine Nahkampfwaffe.");

                break;
            case SkillDelivery.ArrowRain or SkillDelivery.ChargedShot when !executionDefinition.Caster.Weapon.IsRanged:
                GD.PushWarning($"{executionDefinition.Caster.Name} braucht für {executionDefinition.SkillDefinition.Name} einen Bogen.");

                break;
            case SkillDelivery.ArrowRain:
                RainArrows(executionDefinition);

                break;
            case SkillDelivery.ChargedShot:
                ShootCharged(executionDefinition, chargePercent);

                break;
            case SkillDelivery.Weapon or SkillDelivery.MeleeStrike:
                StrikeInMelee(executionDefinition);

                break;
            case SkillDelivery.WeaponSweep:
                Sweep(executionDefinition, GetFacing(executionDefinition.Caster, executionDefinition.SkillAim));

                break;
            case SkillDelivery.WeaponWhirl:
                Whirl(executionDefinition);

                break;
            case SkillDelivery.Projectile:
                LaunchProjectile(executionDefinition);

                break;
            case SkillDelivery.AreaAroundCaster:
                LaunchArea(executionDefinition with { Area = StartAtBodyEdge(executionDefinition) }, executionDefinition.Caster.GlobalPosition);

                break;
            case SkillDelivery.AreaAtPoint:
                LaunchArea(executionDefinition, executionDefinition.SkillAim.CurrentPoint);

                break;
            case SkillDelivery.ChainBeam:
                ChainBeam(executionDefinition);

                break;
            case SkillDelivery.LingeringCloud:
                LaunchCloud(executionDefinition, executionDefinition.SkillAim.CurrentPoint);

                break;
        }
    }

    //Eine Einheit mit Brittle zerspringt beim Tod: eine Fläche um sie, ab ihrem Rand gemessen, mit einem Anteil ihres Lebens als Kälte.
    //Sie gehört dem, der Brittle gelegt hat, Kills und Beute gehen an ihn. Wer daran mit Brittle stirbt, zerspringt ebenfalls
    public static void Shatter(BaseUnit source, BaseUnit victim)
    {
        brittleShatter ??= ResourceLoader.Load<SkillResource>(BrittleShatterPath);

        if (brittleShatter?.Definition is not { Spell: { } spell, Area: { } area })
        {
            GD.PushWarning($"{BrittleShatterPath} ist kein Zauber mit Fläche.");

            return;
        }

        var request = BrittleShatter.CreateRequest(source.Stats, spell, victim.LifeMaximum);

        var executionDefinition = new SkillExecutionDefinition(source, brittleShatter, new SkillAim(victim.GlobalPosition))
        {
            Cast = new SkillCast(source, request),
            Area = area with { Radius = area.Radius + victim.BodyRadiusPx }
        };

        LaunchArea(executionDefinition, victim.GlobalPosition);
    }

    private static void LaunchCloud(SkillExecutionDefinition executionDefinition, Vector3 center)
    {
        var scene    = executionDefinition.EffectScene;
        var caster   = executionDefinition.Caster;
        var settings = executionDefinition.SkillDefinition.Cloud;

        if (scene is null || settings is null)
        {
            GD.PushWarning($"{caster.Name} hat keinen Nebel für seinen Skill.");

            return;
        }

        var cloud = scene.Instantiate<LingeringCloud>();

        cloud.Launch(caster, settings);

        caster.GetParent().AddChild(cloud);

        cloud.GlobalPosition = WorldScale.OnGround(center);
    }

    //Der Blitz springt ohne Flugzeit vom Wirkenden zum ersten Ziel und von dort zum nächsten Gegner, den er noch nicht getroffen hat.
    //Jeder Sprung macht weniger Schaden, Mauern halten ihn auf. Ohne Ziel zuckt er ins Leere zum Mauspunkt, höchstens bis zur Reichweite
    private static void ChainBeam(SkillExecutionDefinition executionDefinition)
    {
        var caster = executionDefinition.Caster;
        var cast   = executionDefinition.Cast;
        var scene  = executionDefinition.EffectScene;

        if (executionDefinition.SkillDefinition.Chain is not { } chain)
        {
            GD.PushWarning($"{caster.Name} hat keinen Blitz für {executionDefinition.SkillDefinition.Name}.");

            return;
        }

        var from   = caster.GlobalPosition + Vector3.Up * (caster.PickHeight * ChainStartHeightShare);
        var aimAt  = GetChainAimPoint(caster, executionDefinition.SkillAim, chain);
        var target = ChooseChainStart(caster, cast, executionDefinition.SkillAim, chain, from, aimAt);

        if (target is null)
        {
            ShowChainArc(caster, scene, from, StopAtWall(caster, from, aimAt));

            return;
        }

        var jumps = chain.GetJumps(ChainSettings.GetProliferate(caster.Stats));

        for (var jump = 0; target is not null; jump++)
        {
            //Vor dem Treffer gemessen, er kann das Ziel töten
            var to     = GetBodyCenter(target);
            var origin = target.GlobalPosition;
            var radius = target.BodyRadiusPx;

            ShowChainArc(caster, scene, from, to);

            cast.ApplyTo(target, false, chain.GetDamageFactor(jump));

            if (jump >= jumps)
                break;

            from   = to;
            target = PickChainLink(caster, cast, origin, chain.JumpRange + radius, from, unit => Math.Max(0f, WorldScale.GroundDistancePx(origin, unit.GlobalPosition) - radius - unit.BodyRadiusPx), chain.JumpRange);
        }
    }

    //Der Gegner unter der Maus hat Vorrang, wenn der Blitz ihn erreicht. Sonst trifft er den Gegner, der dem Mauspunkt am nächsten liegt,
    //höchstens eine Sprungweite davon entfernt
    private static BaseUnit ChooseChainStart(BaseUnit caster, SkillCast cast, SkillAim aim, ChainSettings chain, Vector3 from, Vector3 aimAt)
    {
        if (aim.HasTarget && cast.CanHit(aim.Target) && caster.DistancePxTo(aim.Target) <= chain.Range && IsWithoutWall(caster, from, GetBodyCenter(aim.Target)))
            return aim.Target;

        return PickChainLink(caster,
                             cast,
                             aimAt,
                             chain.JumpRange,
                             from,
                             unit => caster.DistancePxTo(unit) <= chain.Range ? unit.DistancePxTo(aimAt) : -1f,
                             chain.JumpRange);
    }

    //getDistancePx gibt für einen Kandidaten, der nicht in Frage kommt, einen negativen Wert
    private static BaseUnit PickChainLink(BaseUnit caster, SkillCast cast, Vector3 center, float searchPx, Vector3 from, Func<BaseUnit, float> getDistancePx, float maxDistancePx)
    {
        UnitRegistry.FindNear(center, searchPx, UnitsInReach);

        NearestPicker.Pick(UnitsInReach,
                           unit =>
                           {
                               if (!cast.CanHit(unit))
                                   return -1f;

                               var distancePx = getDistancePx(unit);

                               return distancePx >= 0f && distancePx <= maxDistancePx && IsWithoutWall(caster, from, GetBodyCenter(unit)) ? distancePx * distancePx : -1f;
                           },
                           maxDistancePx,
                           1,
                           ChainLinks);

        return ChainLinks.Count > 0 ? ChainLinks[0] : null;
    }

    private static Vector3 GetChainAimPoint(BaseUnit caster, SkillAim aim, ChainSettings chain)
    {
        var offset  = WorldScale.OnGround(aim.CurrentPoint - caster.GlobalPosition);
        var (x, z)  = chain.ClampToRange(WorldScale.ToPx(offset.X), WorldScale.ToPx(offset.Z), caster.BodyRadiusPx);

        return WorldScale.OnGround(caster.GlobalPosition) + new Vector3(WorldScale.ToMeters(x), 0f, WorldScale.ToMeters(z));
    }

    private static Vector3 GetBodyCenter(BaseUnit unit)
        => unit.GlobalPosition + Vector3.Up * (unit.PickHeight * ScatterStartHeightShare);

    private static bool IsWithoutWall(BaseUnit caster, Vector3 from, Vector3 to)
        => caster.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to, CollisionLayers.Walls)).Count == 0;

    private static Vector3 StopAtWall(BaseUnit caster, Vector3 from, Vector3 to)
    {
        var hit = caster.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to, CollisionLayers.Walls));

        return hit.Count > 0 ? hit["position"].AsVector3() : to;
    }

    private static void ShowChainArc(BaseUnit caster, PackedScene scene, Vector3 from, Vector3 to)
    {
        if (scene is null)
            return;

        var arc = scene.Instantiate<ChainArc>();

        arc.Launch(from, to);

        caster.GetParent().AddChild(arc);
    }

    //Der Hieb ist auch zu sehen, wenn er ins Leere geht. Trifft er, läuft er durch das Ziel
    private static void StrikeInMelee(SkillExecutionDefinition skillExecutionDefinition)
    {
        var caster = skillExecutionDefinition.Caster;
        var cast = skillExecutionDefinition.Cast;
        var aim = skillExecutionDefinition.SkillAim;
        var skill = skillExecutionDefinition.Skill;
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
    private static void RainArrows(SkillExecutionDefinition executionDefinition)
    {
        var caster = executionDefinition.Caster;
        var aim = executionDefinition.SkillAim;
        var skill = executionDefinition.Skill;
        
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
    //private static void ShootCharged(BaseUnit caster, SkillResource skill, SkillAim aim, float chargePercent)
    private static void ShootCharged(SkillExecutionDefinition executionDefinition, float chargePercent)
    {       
        var caster = executionDefinition.Caster;
        var skill = executionDefinition.Skill;

        if (skill.Definition.Charge is not { } charge || caster.Weapon.GetProjectile() is not { } projectile)
        {
            GD.PushWarning($"{caster.Name} kann {skill.Definition.Name} nicht laden.");

            return;
        }

        var attack = charge.GetAttack(skill.Definition.Attack, chargePercent);
        var share  = charge.GetShownShare(chargePercent);
        var pierce = charge.Pierces(chargePercent);
        var shot   = executionDefinition with
        {
            Cast = new SkillCast(caster, HitRequests.ForAttack(caster.Stats, caster.Weapon, attack)),
            EffectScene = skill.EffectScene ?? caster.WeaponProjectileScene,
            Projectile = projectile with { Pierces = pierce }
        };

        LaunchProjectile(shot,
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
    private static void Whirl(SkillExecutionDefinition executionDefinition)
    {
        var caster = executionDefinition.Caster;
        
        if (executionDefinition.Sweep is not { } sweep)
        {
            GD.PushWarning($"{caster.Name} hat keinen Kreis für {executionDefinition.SkillDefinition.Name}.");

            return;
        }

        var tick = executionDefinition with { EffectScene = null, Sweep = sweep with { ArcDegrees = SweepSettings.FullCircleDegrees } };

        Sweep(tick, GetFacing(caster, executionDefinition.SkillAim));
    }

    //Der Schweif des Wirbels hängt am Wirbelnden und folgt seiner Waffe, bis er ihn mit Dismiss zurücknimmt. Sein Radius ist der Kreis der Ticks
    public static WhirlTrail ShowWhirlTrail(BaseUnit caster, SkillResource skill)
    {
        if (skill.Definition.Sweep is not { } sweep)
            return null;

        return WhirlTrail.Show(skill.EffectScene, caster, WorldScale.ToMeters(caster.BodyRadiusPx + sweep.GetReach(caster.Weapon)));
    }

    //Der Bogen zeigt, wohin der Schlagende beim Ausholen schaute. Wer währenddessen hinter ihn läuft, entgeht ihm
    private static void Sweep(SkillExecutionDefinition executionDefinition, Vector3 facing)
    {
        var sweep = executionDefinition.Sweep;
        var caster = executionDefinition.Caster;
        var cast = executionDefinition.Cast;
        var slashScene = executionDefinition.EffectScene;
        
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
    //LaunchProjectile(caster, cast, skill.EffectScene, definition.Projectile, aim);
    private static void LaunchProjectile(SkillExecutionDefinition   executionDefinition,
                                         int                        extraProjectiles = 0,
                                         Action<SkillProjectile>    prepare          = null)
    {
        var skillEffectScene = executionDefinition.EffectScene;
        var effektSettings = executionDefinition.Projectile;
        var caster = executionDefinition.Caster;
        var aim = executionDefinition.SkillAim;
        var cast = executionDefinition.Cast;
        
        if (skillEffectScene is null || effektSettings is null)
        {
            GD.PushWarning($"{caster.Name} hat kein Projektil für seinen Skill.");

            return;
        }

        var origin    = WorldScale.OnGround(caster.GlobalPosition);
        var direction = WorldScale.OnGround(aim.CurrentPoint) - origin;
        var count     = 1 + BonusProjectiles.FromStats(caster.Stats) + Math.Max(0, extraProjectiles);
        var speedup   = caster.Stats.GetTotalMultiplier(CombatStat.ProjectileSpeed);

        if (speedup > 0f && !speedup.Equals(1f))
            effektSettings = effektSettings with { Speed = effektSettings.Speed * speedup, LifetimeSec = effektSettings.LifetimeSec / speedup };

        for (var i = 0; i < count; i++)
        {
            var projectile = skillEffectScene.Instantiate<SkillProjectile>();
            var spread     = Mathf.DegToRad(ProjectileSpread.GetOffsetDegrees(i, count));

            projectile.Launch(cast, effektSettings, skillEffectScene, direction.Rotated(Vector3.Up, spread));
            prepare?.Invoke(projectile);

            caster.GetParent().AddChild(projectile);

            projectile.GlobalPosition = origin;
        }
    }

    //Eine Fläche um den Wirkenden beginnt wie jede Reichweite an seinem Rand
    private static AreaSettings StartAtBodyEdge(SkillExecutionDefinition executionDefinition)
        => executionDefinition.Area is { } area ? area with { Radius = area.Radius + executionDefinition.Caster.BodyRadiusPx } : null;

    private static void LaunchArea(SkillExecutionDefinition executionDefinition, Vector3 center)
    {
        var scene = executionDefinition.EffectScene;
        var caster = executionDefinition.Caster;
        var areaSettings = executionDefinition.Area;
        
        if (scene is null || areaSettings is null)
        {
            GD.PushWarning($"{caster.Name} hat keine Fläche für seinen Skill.");

            return;
        }

        var area = scene.Instantiate<SkillArea>();

        area.Launch(executionDefinition.Cast, areaSettings);

        caster.GetParent().AddChild(area);

        area.GlobalPosition = WorldScale.OnGround(center);
    }
}
