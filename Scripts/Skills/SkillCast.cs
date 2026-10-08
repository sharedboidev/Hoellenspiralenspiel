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

    //Nur ein Schlag im Nahkampf kann Reflect auslösen, Projektile und Flächen nicht. damageFactor schwächt nur diesen einen Treffer, etwa nach einem Sprung
    public HitResult ApplyTo(BaseUnit unit, bool isMelee = false, float damageFactor = 1f)
    {
        hitUnits.Add(unit.GetInstanceId());

        var hit      = damageFactor.Equals(1f) ? Hit : Hit.Times(damageFactor);
        var result   = HitResolver.Resolve(hit, unit.Stats, GameRandom.Shared);
        var attacker = GodotObject.IsInstanceValid(caster) ? caster : null;

        unit.ReceiveDamage(result, attacker);

        attacker?.NotifyHitDealt(result, unit);

        if (isMelee && attacker is not null && !attacker.IsDead)
            unit.ReflectMeleeHit(result, attacker);

        return result;
    }
}
