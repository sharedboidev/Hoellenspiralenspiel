using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillPickerEntry : Button
{
    private IHero caster;
    private int   consumableCount;

    public SkillResource Skill { get; private set; }

    public ConsumableBaseResource Consumable { get; private set; }

    public void Init(SkillResource skill, IHero skillCaster)
    {
        Skill  = skill;
        caster = skillCaster;
        Text   = skill?.NameOrId ?? "Empty";
        Icon   = skill?.Icon;
    }

    public void InitConsumable(ConsumableBaseResource consumable, int countInInventory)
    {
        Consumable      = consumable;
        consumableCount = countInInventory;
        Text            = $"{consumable.Definition.Name} ({countInInventory:N0})";
        Icon            = consumable.Icon;
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        if (Consumable is not null)
            return ConsumableTooltip.Build(Consumable, consumableCount);

        if (Skill is null || caster is null)
            return SkillTooltip.BuildNote("Empty", "Clears the slot");

        return SkillTooltip.Build(Skill, caster);
    }

    public override Control _MakeCustomTooltip(string forText)
        => SkillTooltip.CreateContent(forText);
}
