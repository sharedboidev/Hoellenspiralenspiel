using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Skills;

public abstract partial class BaseSkill : Node2D
{
    public abstract float      CriticalHitChance { get; }
    public abstract CombatStat MitigatedBy       { get; }
    public          BaseUnit   User              { get; private set; }

    [Export]
    public int MinDamage { get; set; }

    [Export]
    public int MaxDamage { get; set; }

    [Export]
    public float Cooldown { get; set; }

    public void Init(BaseUnit byUnit)
        => User = byUnit;
}