using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillPicker : PopupPanel
{
    public delegate void SkillChosenEventHandler(int slot, SkillResource skill);

    private const int   GapToSlotPx   = 8;
    private const float EntryWidth    = 320f;
    private const float EntryHeight   = 56f;
    private const int   EntryFontSize = 22;

    private VBoxContainer entries;
    private int           slot;

    public event SkillChosenEventHandler SkillChosen;

    public void Open(int forSlot, IReadOnlyList<SkillResource> skills, Rect2 slotRect, BaseUnit caster)
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

        AddEntry(null, caster);

        foreach (var skill in skills)
            AddEntry(skill, caster);

        var size     = (Vector2I)GetContentsMinimumSize();
        var position = new Vector2I((int)slotRect.Position.X, (int)slotRect.Position.Y - size.Y - GapToSlotPx);

        Popup(new Rect2I(position.Max(Vector2I.Zero), size));
    }

    private void AddEntry(SkillResource skill, BaseUnit caster)
    {
        var entry = new SkillPickerEntry
        {
            ExpandIcon        = true,
            Alignment         = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(EntryWidth, EntryHeight)
        };

        entry.Init(skill, caster);
        entry.AddThemeFontSizeOverride("font_size", EntryFontSize);

        entry.Pressed += () => Choose(skill);

        entries.AddChild(entry);
    }

    private void Choose(SkillResource skill)
    {
        Hide();

        SkillChosen?.Invoke(slot, skill);
    }
}
