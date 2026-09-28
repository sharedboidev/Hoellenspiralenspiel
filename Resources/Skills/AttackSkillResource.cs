using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Resources.Skills;

//Eine ATTACK: skaliert mit dem Schaden der Waffe
[GlobalClass]
public partial class AttackSkillResource : SkillResource
{
    [ExportGroup("Attack")]
    [Export]
    public float WeaponDamagePercent { get; set; } = 100f;

    //Wandelt den Schaden der Waffe in DealtAs um. Sonst gilt die Schadensart der Waffe
    [Export]
    public bool ConvertsDamageType { get; set; }

    [Export]
    public DamageType DealtAs { get; set; }

    public override SkillKind Kind => SkillKind.Attack;

    public override string DamageSummary => ConvertsDamageType
            ? $"{WeaponDamagePercent:0.##}% weapon damage as {DealtAs}"
            : $"{WeaponDamagePercent:0.##}% weapon damage";

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForAttack(Id, new AttackDefinition(NameOrId, WeaponDamagePercent, ConvertsDamageType ? DealtAs : null));
}
