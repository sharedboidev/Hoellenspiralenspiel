using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;

namespace Hoellenspiralenspiel.Scripts.Utils;

//Übersetzt zwischen der InputMap von Godot und den Belegungen des Kerns
public static class KeyBindings
{
    private static Dictionary<string, InputBinding> defaults;

    //Die Belegung aus project.godot, festgehalten bevor Einstellungen sie ändern
    public static IReadOnlyDictionary<string, InputBinding> Defaults => defaults ??= ReadInputMap();

    public static InputBinding ToBinding(InputEvent inputEvent)
        => inputEvent switch
        {
            InputEventKey key                 => new InputBinding(BindingKind.Key, (long)(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode)),
            InputEventMouseButton mouseButton => new InputBinding(BindingKind.Mouse, (long)mouseButton.ButtonIndex),
            _                                 => default
        };

    //Nur die Lage der Taste, ohne Umschalt, Strg oder Alt, für alle Geräte. So passt sie auf jedes Tastaturlayout
    public static InputEvent ToEvent(InputBinding binding)
        => binding.Kind == BindingKind.Mouse
               ? new InputEventMouseButton { ButtonIndex = (MouseButton)binding.Code, Device = -1 }
               : new InputEventKey { PhysicalKeycode = (Key)binding.Code, Device = -1 };

    public static string GetLabel(InputBinding binding)
        => binding.IsValid ? InputActions.GetLabel(ToEvent(binding)) : string.Empty;

    //Eine Aktion, die gerade gehalten wird, gilt danach als losgelassen. Sonst passte das spätere Loslassen nicht mehr und sie bliebe gedrückt
    public static void Apply(IReadOnlyDictionary<string, InputBinding> bindings)
    {
        foreach (var (action, binding) in bindings)
        {
            if (!InputMap.HasAction(action) || ReadAction(action) == binding)
                continue;

            Input.ActionRelease(action);

            InputMap.ActionEraseEvents(action);
            InputMap.ActionAddEvent(action, ToEvent(binding));
        }
    }

    private static InputBinding ReadAction(StringName action)
        => InputMap.ActionGetEvents(action).Select(ToBinding).FirstOrDefault(binding => binding.IsValid);

    private static Dictionary<string, InputBinding> ReadInputMap()
        => InputActions.Rebindable.Select(entry => entry.Action)
                       .Where(InputMap.HasAction)
                       .Select(action => (Name: action.ToString(), Binding: ReadAction(action)))
                       .Where(entry => entry.Binding.IsValid)
                       .ToDictionary(entry => entry.Name, entry => entry.Binding);
}
