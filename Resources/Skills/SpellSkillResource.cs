using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Resources.Skills;

[GlobalClass]
public partial class SpellSkillResource : SkillResource
{
    [ExportGroup("Spell")]
    [Export]
    public float MinDamage { get; set; }

    [Export]
    public float MaxDamage { get; set; }

    [Export]
    public DamageType DamageType { get; set; } = DamageType.Fire;

    [Export(PropertyHint.Range, "0.0, 100.0,")]
    public float CriticalHitChance { get; set; } = 5f;

    //So lange steht der Held beim Wirken, ausgelöst wird nach der Hälfte. Gegner nehmen die Zeiten ihres EnemyResource
    [Export(PropertyHint.Range, "0.0, 3.0, 0.05")]
    public double CastSec { get; set; } = 0.4;

    public override SkillKind Kind => SkillKind.Spell;

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForSpell(Id, new SpellDefinition(NameOrId, MinDamage, MaxDamage, DamageType, CriticalHitChance)) with { CastSec = CastSec };
}
