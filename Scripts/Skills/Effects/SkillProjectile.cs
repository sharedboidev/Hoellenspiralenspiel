using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Die Szene muss nach -Z zeigen. Ihre Kollisionsform ist ein hoher Zylinder, damit die Höhe des Ziels keine Rolle spielt
public partial class SkillProjectile : Area3D
{
    private readonly List<BaseUnit>     forkTargets  = new();
    private readonly List<BaseUnit>     unitsInRange = new();
    private          double             ageSec;
    private          SkillCast          cast;
    private          Vector3            direction = Vector3.Forward;
    private          int                generation;
    private          bool               isSpent;
    private          PackedScene        scene;
    private          ProjectileSettings settings;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(SkillCast          skillCast,
                       ProjectileSettings projectileSettings,
                       PackedScene        projectileScene,
                       Vector3            flightDirection,
                       int                forkGeneration = 0)
    {
        var onGround = WorldScale.OnGround(flightDirection);

        cast       = skillCast;
        settings   = projectileSettings;
        scene      = projectileScene;
        generation = forkGeneration;
        direction  = onGround.LengthSquared() < 0.0001f ? Vector3.Forward : onGround.Normalized();

        CollisionLayer = CollisionLayers.Spells;
        CollisionMask  = CollisionLayers.GetSkillMask(cast.Faction);
        Rotation       = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
    }

    public override void _Ready()
        => BodyEntered += OnBodyEntered;

    public override void _PhysicsProcess(double delta)
    {
        if (isSpent || cast is null)
            return;

        GlobalPosition += direction * WorldScale.ToMeters(settings.Speed) * (float)delta;
        ageSec         += delta;

        if (ageSec >= settings.LifetimeSec)
            Spend();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (isSpent || cast is null)
            return;

        if (body is not BaseUnit unit)
        {
            Spend();

            return;
        }

        if (!cast.CanHit(unit))
            return;

        cast.ApplyTo(unit);

        Fork(unit);

        //Der Wurf merkt sich jeden Getroffenen, ein durchstoßendes Projektil trifft ihn deshalb kein zweites Mal
        if (!settings.Pierces)
            Spend();
    }

    private void Fork(BaseUnit hitUnit)
    {
        if (!settings.CanFork || generation >= settings.ForkGenerations)
            return;

        var origin = hitUnit.GlobalPosition;
        var parent = GetParent();

        UnitRegistry.FindNear(origin, settings.ForkRange, unitsInRange);

        NearestPicker.Pick(unitsInRange,
                           unit => cast.CanHit(unit) ? GetDistanceSquaredPx(origin, unit) : -1f,
                           settings.ForkRange,
                           settings.ForkCount,
                           forkTargets);

        foreach (var target in forkTargets)
        {
            var fork = scene.Instantiate<SkillProjectile>();

            fork.Launch(cast, settings, scene, target.GlobalPosition - origin, generation + 1);

            //Der Treffer wird mitten im Physikschritt gemeldet, neue Flächen dürfen erst danach entstehen
            Callable.From(() => SpawnFork(parent, fork, origin)).CallDeferred();
        }
    }

    private static float GetDistanceSquaredPx(Vector3 origin, BaseUnit unit)
    {
        var distancePx = unit.DistancePxTo(origin);

        return distancePx * distancePx;
    }

    private static void SpawnFork(Node parent, SkillProjectile fork, Vector3 origin)
    {
        if (!IsInstanceValid(parent))
        {
            fork.QueueFree();

            return;
        }

        parent.AddChild(fork);

        fork.GlobalPosition = origin;
    }

    private void Spend()
    {
        isSpent = true;

        QueueFree();
    }
}
