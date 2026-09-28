using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Resources.Skills;

[GlobalClass]
public partial class AttackSkillResource : SkillResource
{
    [ExportGroup("Attack")]
    [Export]
    public float WeaponDamagePercent { get; set; } = 100f;

    [Export]
    public bool ConvertsDamageType { get; set; }

    [Export]
    public DamageType DealtAs { get; set; }

    public override SkillKind Kind => SkillKind.Attack;

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForAttack(Id, new AttackDefinition(NameOrId, WeaponDamagePercent, ConvertsDamageType ? DealtAs : null));
}
