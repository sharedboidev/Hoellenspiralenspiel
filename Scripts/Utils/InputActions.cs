using Godot;

namespace Hoellenspiralenspiel.Scripts.Utils;

//Namen der Eingabeaktionen aus den Projekteinstellungen. Die Namen beschreiben die Funktion, nicht die Taste
public static class InputActions
{
    public static readonly StringName MoveLeft             = "move_left";
    public static readonly StringName MoveRight            = "move_right";
    public static readonly StringName MoveUp               = "move_up";
    public static readonly StringName MoveDown             = "move_down";
    public static readonly StringName ToggleCharacterSheet = "toggle_character_sheet";
    public static readonly StringName ToggleOverlayMap     = "toggle_overlay_map";
    public static readonly StringName PrimaryAction        = "primary_action";
}
