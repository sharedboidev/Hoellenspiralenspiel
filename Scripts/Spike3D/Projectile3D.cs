using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Die Szene muss nach -Z zeigen. Ihre Kollisionsform ist ein hoher Zylinder, damit die Höhe des Ziels keine Rolle spielt
public partial class Projectile3D : Area3D
{
    private readonly List<Unit3D>       forkTargets  = new();
    private readonly List<Unit3D>       unitsInRange = new();
    private          double             ageSec;
    private          Cast3D             cast;
    private          Vector3            direction = Vector3.Forward;
    private          int                generation;
    private          bool               isSpent;
    private          PackedScene        scene;
    private          ProjectileSettings settings;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(Cast3D             skillCast,
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

        if (body is not Unit3D unit)
        {
            Spend();

            return;
        }

        if (!cast.CanHit(unit))
            return;

        cast.ApplyTo(unit);

        Fork(unit);
        Spend();
    }

    private void Fork(Unit3D hitUnit)
    {
        if (!settings.CanFork || generation >= settings.ForkGenerations)
            return;

        var origin = hitUnit.GlobalPosition;
        var parent = GetParent();

        UnitRegistry3D.FindNear(origin, settings.ForkRange, unitsInRange);

        NearestPicker.Pick(unitsInRange,
                           unit => cast.CanHit(unit) ? GetDistanceSquaredPx(origin, unit) : -1f,
                           settings.ForkRange,
                           settings.ForkCount,
                           forkTargets);

        foreach (var target in forkTargets)
        {
            var fork = scene.Instantiate<Projectile3D>();

            fork.Launch(cast, settings, scene, target.GlobalPosition - origin, generation + 1);

            //Der Treffer wird mitten im Physikschritt gemeldet, neue Flächen dürfen erst danach entstehen
            Callable.From(() => SpawnFork(parent, fork, origin)).CallDeferred();
        }
    }

    private static float GetDistanceSquaredPx(Vector3 origin, Unit3D unit)
    {
        var distancePx = WorldScale.GroundDistancePx(origin, unit.GlobalPosition);

        return distancePx * distancePx;
    }

    private static void SpawnFork(Node parent, Projectile3D fork, Vector3 origin)
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
