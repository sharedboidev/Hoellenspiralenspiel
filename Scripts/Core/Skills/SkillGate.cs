using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public enum SkillUseCheck
{
    Ready,
    OnCooldown,
    NotEnoughMana,
    NeedsMeleeWeapon,
    NeedsBow
}

public static class SkillGate
{
    public const float UnlimitedMana = float.PositiveInfinity;

    public static SkillUseCheck Check(SkillDefinition skill, SkillCooldowns cooldowns, float availableMana, bool holdsRangedWeapon = false)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(cooldowns);

        if (skill.NeedsMeleeWeapon && holdsRangedWeapon)
            return SkillUseCheck.NeedsMeleeWeapon;

        if (skill.NeedsBow && !holdsRangedWeapon)
            return SkillUseCheck.NeedsBow;

        if (!cooldowns.IsReady(skill.Id))
            return SkillUseCheck.OnCooldown;

        if (skill.ManaCost > 0 && availableMana < skill.ManaCost)
            return SkillUseCheck.NotEnoughMana;

        return SkillUseCheck.Ready;
    }

    public static bool FitsWeapon(SkillDefinition skill, bool holdsRangedWeapon)
        => skill is not null && !(skill.NeedsMeleeWeapon && holdsRangedWeapon) && !(skill.NeedsBow && !holdsRangedWeapon);
}
