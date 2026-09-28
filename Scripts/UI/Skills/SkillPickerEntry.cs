using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillPickerEntry : Button
{
    private BaseUnit caster;

    public SkillResource Skill { get; private set; }

    public void Init(SkillResource skill, BaseUnit skillCaster)
    {
        Skill  = skill;
        caster = skillCaster;
        Text   = skill?.NameOrId ?? "Empty";
        Icon   = skill?.Icon;
    }

    public override string _GetTooltip(Vector2 atPosition)
        => Skill is null || caster is null
                ? SkillTooltip.BuildNote("Empty", "Clears the slot")
                : SkillTooltip.Build(Skill, caster);

    public override Control _MakeCustomTooltip(string forText)
        => SkillTooltip.CreateContent(forText);
}
