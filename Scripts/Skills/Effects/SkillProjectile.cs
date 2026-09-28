using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Die Szene muss nach rechts zeigen
public partial class SkillProjectile : Area2D
{
    private readonly List<BaseUnit>     forkTargets  = new();
    private readonly List<BaseUnit>     unitsInRange = new();
    private          double             ageSec;
    private          SkillCast          cast;
    private          Vector2            direction = Vector2.Right;
    private          int                generation;
    private          bool               isSpent;
    private          PackedScene        scene;
    private          ProjectileSettings settings;

    //Vor dem Einhängen in den Szenenbaum aufrufen
    public void Launch(SkillCast          skillCast,
                       ProjectileSettings projectileSettings,
                       PackedScene        projectileScene,
                       Vector2            flightDirection,
                       int                forkGeneration = 0)
    {
        cast       = skillCast;
        settings   = projectileSettings;
        scene      = projectileScene;
        generation = forkGeneration;
        direction  = flightDirection.LengthSquared() < 0.0001f ? Vector2.Right : flightDirection.Normalized();

        CollisionLayer = CollisionLayers.Spells;
        CollisionMask  = CollisionLayers.GetSkillMask(cast.Faction);
        Rotation       = direction.Angle();
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        foreach (var sprite in this.GetAllChildren<AnimatedSprite2D>())
            sprite.Play();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isSpent || cast is null)
            return;

        GlobalPosition += direction * settings.Speed * (float)delta;
        ageSec         += delta;

        if (ageSec >= settings.LifetimeSec)
            Spend();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (isSpent || cast is null)
            return;

        if (body is not BaseUnit unit)
        {
            //Die Maske enthält außer den feindlichen Körpern nur Wände
            Spend();

            return;
        }

        if (!cast.CanHit(unit))
            return;

        cast.ApplyTo(unit);

        Fork(unit);
        Spend();
    }

    private void Fork(BaseUnit hitUnit)
    {
        if (!settings.CanFork || generation >= settings.ForkGenerations)
            return;

        var origin = hitUnit.BodyCenter;
        var parent = GetParent();

        UnitRegistry.FindNear(origin, settings.ForkRange, unitsInRange);

        NearestPicker.Pick(unitsInRange,
                           unit => cast.CanHit(unit) ? origin.DistanceSquaredTo(unit.BodyCenter) : -1f,
                           settings.ForkRange,
                           settings.ForkCount,
                           forkTargets);

        foreach (var target in forkTargets)
        {
            var fork = scene.Instantiate<SkillProjectile>();

            fork.Launch(cast, settings, scene, target.BodyCenter - origin, generation + 1);

            //Der Treffer wird mitten im Physikschritt gemeldet, neue Flächen dürfen erst danach entstehen
            Callable.From(() => SpawnFork(parent, fork, origin)).CallDeferred();
        }
    }

    private static void SpawnFork(Node parent, SkillProjectile fork, Vector2 origin)
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
