using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public sealed class Cast3D
{
    private readonly Unit3D         caster;
    private readonly HashSet<ulong> hitUnits = new();

    public Cast3D(Unit3D caster, HitRequest hit)
    {
        this.caster = caster;

        Faction = caster.Faction;
        Hit     = hit;
    }

    public Faction    Faction { get; }
    public HitRequest Hit     { get; }

    public bool CanHit(Unit3D unit)
        => GodotObject.IsInstanceValid(unit) &&
           unit.IsTargetable &&
           unit.Faction != Faction &&
           !hitUnits.Contains(unit.GetInstanceId());

    public void ApplyTo(Unit3D unit)
    {
        hitUnits.Add(unit.GetInstanceId());

        var result   = HitResolver.Resolve(Hit, unit.Stats, GameRandom.Shared);
        var attacker = GodotObject.IsInstanceValid(caster) ? caster : null;

        unit.ReceiveDamage(result, attacker);
    }
}
