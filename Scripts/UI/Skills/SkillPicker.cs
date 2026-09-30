using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillPicker : PopupPanel
{
    public delegate void SkillChosenEventHandler(int slot, SkillResource skill);

    public delegate void ConsumableChosenEventHandler(int slot, ConsumableBaseResource consumable);

    private const int   GapToSlotPx     = 8;
    private const float EntryWidth      = 320f;
    private const float EntryHeight     = 56f;
    private const int   EntryFontSize   = 22;
    private const int   HeadingFontSize = 18;

    private VBoxContainer entries;
    private int           slot;

    public event SkillChosenEventHandler      SkillChosen;
    public event ConsumableChosenEventHandler ConsumableChosen;

    public void Open(int                                   forSlot,
                     IReadOnlyList<SkillResource>          skills,
                     IReadOnlyList<ConsumableBaseResource> consumables,
                     Rect2                                 slotRect,
                     IHero                                 caster)
    {
        slot = forSlot;

        if (entries is null)
        {
            entries = new VBoxContainer();

            AddChild(entries);
        }

        foreach (var oldEntry in entries.GetChildren())
        {
            entries.RemoveChild(oldEntry);
            oldEntry.QueueFree();
        }

        AddSkillEntry(null, caster);

        foreach (var skill in skills)
            AddSkillEntry(skill, caster);

        if (consumables.Count > 0)
            AddHeading("Consumables");

        foreach (var consumable in consumables)
            AddConsumableEntry(consumable, caster.Items.CountInInventory(consumable.Id));

        var size     = (Vector2I)GetContentsMinimumSize();
        var position = new Vector2I((int)slotRect.Position.X, (int)slotRect.Position.Y - size.Y - GapToSlotPx);

        Popup(new Rect2I(position.Max(Vector2I.Zero), size));
    }

    private void AddSkillEntry(SkillResource skill, IHero caster)
    {
        var entry = CreateEntry();

        entry.Init(skill, caster);

        entry.Pressed += () => ChooseSkill(skill);
    }

    private void AddConsumableEntry(ConsumableBaseResource consumable, int countInInventory)
    {
        var entry = CreateEntry();

        entry.InitConsumable(consumable, countInInventory);

        entry.Pressed += () => ChooseConsumable(consumable);
    }

    private SkillPickerEntry CreateEntry()
    {
        var entry = new SkillPickerEntry
        {
            ExpandIcon        = true,
            Alignment         = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(EntryWidth, EntryHeight)
        };

        entry.AddThemeFontSizeOverride("font_size", EntryFontSize);

        entries.AddChild(entry);

        return entry;
    }

    private void AddHeading(string text)
    {
        var heading = new Label { Text = text };

        heading.AddThemeFontSizeOverride("font_size", HeadingFontSize);
        heading.AddThemeColorOverride("font_color", Colors.Gray);

        entries.AddChild(heading);
    }

    private void ChooseSkill(SkillResource skill)
    {
        Hide();

        SkillChosen?.Invoke(slot, skill);
    }

    private void ChooseConsumable(ConsumableBaseResource consumable)
    {
        Hide();

        ConsumableChosen?.Invoke(slot, consumable);
    }
}
