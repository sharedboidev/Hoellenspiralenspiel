using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Actions;

[GlobalClass]
public partial class HealAction : ModAction
{
    [Export(PropertyHint.Range, "0,100,1")]
    public float PercentOfMaxLife { get; set; } = 25f;

    public override void Run(ModContext context)
    {
        var owner = context.Owner;

        if (owner.IsDead)
            return;

        var lifeBefore = owner.LifeCurrent;

        owner.LifeCurrent += owner.LifeMaximum * PercentOfMaxLife / 100f;

        var healed = owner.LifeCurrent - lifeBefore;

        if (healed >= 1f)
            owner.ShowHeal(healed, owner.CombatTextOffset);
    }
}
