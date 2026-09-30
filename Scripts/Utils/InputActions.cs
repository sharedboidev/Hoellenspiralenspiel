using Godot;

namespace Hoellenspiralenspiel.Scripts.Utils;

//Namen der Eingabeaktionen aus den Projekteinstellungen. Die Namen beschreiben die Funktion, nicht die Taste
public static class InputActions
{
    private const string HeadlessDisplayServer = "headless";

    public static readonly StringName MoveLeft            = "move_left";
    public static readonly StringName MoveRight            = "move_right";
    public static readonly StringName MoveUp               = "move_up";
    public static readonly StringName MoveDown             = "move_down";
    public static readonly StringName ToggleCharacterSheet = "toggle_character_sheet";
    public static readonly StringName ToggleOverlayMap     = "toggle_overlay_map";
    public static readonly StringName ToggleLootLabels     = "toggle_loot_labels";
    public static readonly StringName OpenTownPortal       = "open_town_portal";
    public static readonly StringName TogglePauseMenu      = "toggle_pause_menu";
    public static readonly StringName CloseWindows         = "close_windows";

    public static readonly StringName[] SkillSlots =
    [
        "skill_slot_1",
        "skill_slot_2",
        "skill_slot_3",
        "skill_slot_4",
        "skill_slot_5",
        "skill_slot_6",
        "skill_slot_7",
        "skill_slot_8",
        "skill_slot_9",
        "skill_slot_10"
    ];

    //Was sich in den Einstellungen umbelegen lässt, in der Reihenfolge der Liste. Escape bleibt fest
    public static readonly (StringName Action, string Label)[] Rebindable =
    [
        (MoveUp, "Move Up"),
        (MoveLeft, "Move Left"),
        (MoveDown, "Move Down"),
        (MoveRight, "Move Right"),
        (SkillSlots[0], "Skill 1"),
        (SkillSlots[1], "Skill 2"),
        (SkillSlots[2], "Skill 3"),
        (SkillSlots[3], "Skill 4"),
        (SkillSlots[4], "Skill 5"),
        (SkillSlots[5], "Skill 6"),
        (SkillSlots[6], "Skill 7"),
        (SkillSlots[7], "Skill 8"),
        (SkillSlots[8], "Skill 9"),
        (SkillSlots[9], "Skill 10"),
        (ToggleCharacterSheet, "Character & Inventory"),
        (ToggleOverlayMap, "Map"),
        (ToggleLootLabels, "Item Names"),
        (OpenTownPortal, "Town Portal"),
        (CloseWindows, "Close Windows")
    ];

    public static string GetKeyLabel(StringName action)
    {
        if (!InputMap.HasAction(action))
            return string.Empty;

        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            var label = GetLabel(inputEvent);

            if (label.Length > 0)
                return label;
        }

        return string.Empty;
    }

    public static string GetLabel(InputEvent inputEvent)
        => inputEvent switch
        {
            InputEventMouseButton mouseButton => GetMouseButtonLabel(mouseButton.ButtonIndex),
            InputEventKey key                 => OS.GetKeycodeString(GetKeycode(key)),
            _                                 => string.Empty
        };

    private static string GetMouseButtonLabel(MouseButton button)
        => button switch
        {
            MouseButton.Left     => "LMB",
            MouseButton.Right    => "RMB",
            MouseButton.Middle   => "MMB",
            MouseButton.Xbutton1 => "M4",
            MouseButton.Xbutton2 => "M5",
            _                    => $"M{(int)button}"
        };

    //Aktionen speichern die Lage der Taste. Beschriftet wird mit dem Zeichen, das die Tastatur des Spielers dort hat
    private static Key GetKeycode(InputEventKey key)
    {
        if (key.Keycode != Key.None)
            return key.Keycode;

        //Ohne Fenster gibt es keine Tastatur, deren Belegung sich abfragen ließe
        if (DisplayServer.GetName() == HeadlessDisplayServer)
            return key.PhysicalKeycode;

        var keycode = DisplayServer.KeyboardGetKeycodeFromPhysical(key.PhysicalKeycode);

        return keycode != Key.None ? keycode : key.PhysicalKeycode;
    }
}
