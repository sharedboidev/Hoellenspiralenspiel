using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public enum SkillUseCheck
{
    Ready,
    OnCooldown,
    NotEnoughMana
}

//Prüft, ob eine Einheit einen Skill gerade einsetzen kann
public static class SkillGate
{
    //Für Einheiten ohne Mana, sie zahlen nichts
    public const float UnlimitedMana = float.PositiveInfinity;

    public static SkillUseCheck Check(SkillDefinition skill, SkillCooldowns cooldowns, float availableMana)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(cooldowns);

        if (!cooldowns.IsReady(skill.Id))
            return SkillUseCheck.OnCooldown;

        if (skill.ManaCost > 0 && availableMana < skill.ManaCost)
            return SkillUseCheck.NotEnoughMana;

        return SkillUseCheck.Ready;
    }
}
