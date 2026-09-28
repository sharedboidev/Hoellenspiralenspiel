using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public sealed class SkillLoadout
{
    private readonly string[] slots;

    public SkillLoadout(int slotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotCount);

        slots = new string[slotCount];
    }

    public int SlotCount => slots.Length;

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

    public int FindSlotOf(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return -1;

        return Array.IndexOf(slots, skillId);
    }

    private bool IsValidSlot(int slot)
        => slot >= 0 && slot < slots.Length;
}
