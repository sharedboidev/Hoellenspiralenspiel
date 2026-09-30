using System;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Platz hält einen Skill oder die Item-Basis eines Tranks, nie einen bestimmten Stapel
public sealed class SkillLoadout
{
    private readonly Entry[] slots;

    public SkillLoadout(int slotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotCount);

        slots = new Entry[slotCount];
    }

    public int SlotCount => slots.Length;

    public event Action<int> SlotChanged;

    public string GetSkillId(int slot)
        => IsValidSlot(slot) && !slots[slot].IsConsumable ? slots[slot].Id : null;

    public string GetConsumableId(int slot)
        => IsValidSlot(slot) && slots[slot].IsConsumable ? slots[slot].Id : null;

    public bool IsEmpty(int slot)
        => !IsValidSlot(slot) || slots[slot].Id is null;

    public void Assign(int slot, string skillId)
        => Put(slot, new Entry(false, skillId));

    public void AssignConsumable(int slot, string itemBaseId)
        => Put(slot, new Entry(true, itemBaseId));

    public void Clear(int slot)
        => Assign(slot, null);

    public int FindSlotOf(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return -1;

        return Array.FindIndex(slots, entry => !entry.IsConsumable && entry.Id == skillId);
    }

    private void Put(int slot, Entry entry)
    {
        if (!IsValidSlot(slot))
            throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Die Leiste hat {slots.Length} Plätze.");

        var newEntry = string.IsNullOrWhiteSpace(entry.Id) ? default : entry;

        if (slots[slot] == newEntry)
            return;

        slots[slot] = newEntry;

        SlotChanged?.Invoke(slot);
    }

    private bool IsValidSlot(int slot)
        => slot >= 0 && slot < slots.Length;

    private readonly record struct Entry(bool IsConsumable, string Id);
}
