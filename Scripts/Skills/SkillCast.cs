using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

public sealed class SkillCast
{
    private readonly BaseUnit       caster;
    private readonly HashSet<ulong> hitUnits = new();

    public SkillCast(BaseUnit caster, HitRequest hit)
    {
        this.caster = caster;

        Faction = caster.Faction;
        Hit     = hit;
    }

    public Faction    Faction { get; }
    public HitRequest Hit     { get; }

    public bool CanHit(BaseUnit unit)
        => GodotObject.IsInstanceValid(unit) &&
           unit.IsTargetable &&
           unit.Faction != Faction &&
           !hitUnits.Contains(unit.GetInstanceId());

    public void ApplyTo(BaseUnit unit)
    {
        hitUnits.Add(unit.GetInstanceId());

        var result   = HitResolver.Resolve(Hit, unit.Stats, GameRandom.Shared);
        var attacker = GodotObject.IsInstanceValid(caster) ? caster : null;

        unit.ReceiveDamage(result, attacker);

        attacker?.NotifyHitDealt(result, unit);
    }
}
