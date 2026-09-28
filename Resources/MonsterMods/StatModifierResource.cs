using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

[GlobalClass]
public partial class StatModifierResource : Resource
{
    [Export]
    public CombatStat Stat { get; set; }

    [Export]
    public ModificationType Modification { get; set; }

    //Bei Percentage und More ein Bruch: 0,33 bedeutet 33 %
    [Export]
    public float Value { get; set; }

    public CombatStatModifier ToModifier(string originId = "")
        => new(Stat, Modification, Value, originId);
}
