using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Welcher Skill auf welchem Platz der Leiste liegt. Gespeichert wird die Id des Skills, ein leerer Platz ist null.
//Derselbe Skill darf auf mehreren Plätzen liegen
public sealed class SkillLoadout
{
    private readonly string[] slots;

    public SkillLoadout(int slotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotCount);

        slots = new string[slotCount];
    }

    public int SlotCount => slots.Length;

    //Feuert mit der Nummer des Platzes, dessen Belegung sich geändert hat
    public event Action<int> SlotChanged;

    public string GetSkillId(int slot)
        => IsValidSlot(slot) ? slots[slot] : null;

    public bool IsEmpty(int slot)
        => GetSkillId(slot) is null;

    public void Assign(int slot, string skillId)
    {
        if (!IsValidSlot(slot))
            throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Die Leiste hat {slots.Length} Plätze.");

        var newSkillId = string.IsNullOrWhiteSpace(skillId) ? null : skillId;

        if (slots[slot] == newSkillId)
            return;

        slots[slot] = newSkillId;

        SlotChanged?.Invoke(slot);
    }

    public void Clear(int slot)
        => Assign(slot, null);

    //Der erste Platz, auf dem der Skill liegt, oder -1
    public int FindSlotOf(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return -1;

        return Array.IndexOf(slots, skillId);
    }

    private bool IsValidSlot(int slot)
        => slot >= 0 && slot < slots.Length;
}
