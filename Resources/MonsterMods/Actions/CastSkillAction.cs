using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Actions;

//Der Skill kostet das Monster weder Mana noch Abklingzeit, den Takt bestimmt der Auslöser
[GlobalClass]
public partial class CastSkillAction : ModAction
{
    [Export]
    public SkillResource Skill { get; set; }

    [Export]
    public ModAim Aim { get; set; } = ModAim.Target;

    //Zählt nur für die zufälligen Zielpunkte
    [Export]
    public float Radius { get; set; } = 300f;

    [Export]
    public int Count { get; set; } = 1;

    public override void Run(ModContext context)
    {
        if (Skill is null)
            return;

        for (var i = 0; i < Count; i++)
        {
            if (!ModAimResolver.TryResolve(context, Aim, Radius, out var aim))
                return;

            SkillExecutor.Execute(context.Owner, Skill, aim);
        }
    }
}
