using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

//Ein ausgelöster Skill. Fraktion und Treffer stehen beim Auslösen fest,
//damit der Skill weiterwirkt, auch wenn der Wirkende inzwischen tot ist
public sealed class SkillCast
{
    //Einheiten, die dieser Wurf schon getroffen hat. Forks teilen sich die Liste, niemand wird doppelt getroffen
    private readonly HashSet<ulong> hitUnits = new();

    public SkillCast(Faction faction, HitRequest hit)
    {
        Faction = faction;
        Hit     = hit;
    }

    public Faction    Faction { get; }
    public HitRequest Hit     { get; }

    //Ein Skill trifft Einheiten, deren Fraktion sich von der des Wirkenden unterscheidet
    public bool CanHit(BaseUnit unit)
        => GodotObject.IsInstanceValid(unit) &&
           unit.IsTargetable &&
           unit.Faction != Faction &&
           !hitUnits.Contains(unit.GetInstanceId());

    public bool HasHit(BaseUnit unit)
        => GodotObject.IsInstanceValid(unit) && hitUnits.Contains(unit.GetInstanceId());

    //Würfelt den Treffer über die zentrale Trefferauflösung. Angewendet wird er vom Ziel
    public void ApplyTo(BaseUnit unit)
    {
        hitUnits.Add(unit.GetInstanceId());

        unit.ReceiveDamage(HitResolver.Resolve(Hit, unit.Stats, GameRandom.Shared));
    }
}
