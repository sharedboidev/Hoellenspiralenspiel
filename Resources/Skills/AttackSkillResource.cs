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

    //Kugeln, die nach einem gelandeten Treffer aus dem Ziel springen. 0 heißt keine
    [ExportGroup("Scatter")]
    [Export]
    public int ScatterCount { get; set; }

    [Export]
    public float ScatterWeaponDamagePercent { get; set; } = 50f;

    //In Pixeln. Die Kugeln landen so nah am Ziel, dass dieser Radius es erreicht
    [Export]
    public float ScatterImpactRadius { get; set; } = 75f;

    [Export]
    public float ScatterFlightSec { get; set; } = 0.6f;

    [Export]
    public PackedScene ScatterScene { get; set; }

    public override SkillKind Kind => SkillKind.Attack;

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForAttack(Id, new AttackDefinition(NameOrId, WeaponDamagePercent, ConvertsDamageType ? DealtAs : null)) with
        {
            Scatter = ScatterCount > 0 ? new ScatterSettings(ScatterCount, ScatterWeaponDamagePercent, ScatterImpactRadius, ScatterFlightSec) : null
        };
}
