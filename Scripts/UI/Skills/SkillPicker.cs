using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

//Die Liste aller bekannten Skills. Ein Klick legt den Skill auf den Platz, für den die Liste geöffnet wurde
public partial class SkillPicker : PopupPanel
{
    public delegate void SkillChosenEventHandler(int slot, SkillResource skill);

    private const int   GapToSlotPx   = 8;
    private const float EntryWidth    = 320f;
    private const float EntryHeight   = 56f;
    private const int   EntryFontSize = 22;

    private VBoxContainer entries;
    private int           slot;

    //Ohne Skill soll der Platz geleert werden
    public event SkillChosenEventHandler SkillChosen;

    //Öffnet die Liste über dem Platz der Leiste
    public void Open(int forSlot, IReadOnlyList<SkillResource> skills, Rect2 slotRect)
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

        AddEntry("Empty", null, "Clears the slot", null);

        foreach (var skill in skills)
            AddEntry(skill.NameOrId, skill.Icon, skill.GetTooltip(), skill);

        var size     = (Vector2I)GetContentsMinimumSize();
        var position = new Vector2I((int)slotRect.Position.X, (int)slotRect.Position.Y - size.Y - GapToSlotPx);

        Popup(new Rect2I(position.Max(Vector2I.Zero), size));
    }

    private void AddEntry(string text, Texture2D icon, string tooltip, SkillResource skill)
    {
        var entry = new Button
        {
            Text              = text,
            Icon              = icon,
            ExpandIcon        = true,
            Alignment         = HorizontalAlignment.Left,
            TooltipText       = tooltip,
            CustomMinimumSize = new Vector2(EntryWidth, EntryHeight)
        };

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
