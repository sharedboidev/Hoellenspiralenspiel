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

    public static string GetKeyLabel(StringName action)
    {
        if (!InputMap.HasAction(action))
            return string.Empty;

        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            switch (inputEvent)
            {
                case InputEventMouseButton mouseButton:
                    return GetMouseButtonLabel(mouseButton.ButtonIndex);
                case InputEventKey key:
                    return OS.GetKeycodeString(GetKeycode(key));
            }
        }

        return string.Empty;
    }

    private static string GetMouseButtonLabel(MouseButton button)
        => button switch
        {
            MouseButton.Left   => "LMB",
            MouseButton.Right  => "RMB",
            MouseButton.Middle => "MMB",
            _                  => $"M{(int)button}"
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
