using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Resources.Skills;

//Ein SPELL: bringt seinen eigenen Grundschaden mit und skaliert nicht mit der Waffe
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

    public override SkillKind Kind => SkillKind.Spell;

    public override string DamageSummary => $"{MinDamage:0.##} to {MaxDamage:0.##} {DamageType} damage";

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForSpell(Id, new SpellDefinition(NameOrId, MinDamage, MaxDamage, DamageType, CriticalHitChance));
}
