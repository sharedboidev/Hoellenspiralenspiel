using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Settings;

//Ein Klick auf eine Belegung wartet auf die nächste Taste oder, bei Skill-Plätzen, auch auf eine Maustaste.
//Escape bricht ab. Die Regeln stehen im Kern unter BindingRules
public partial class ControlsTab : VBoxContainer, ISettingsTab
{
    private readonly Dictionary<string, Button> buttons = new();

    private string      capturing;
    private Key         pendingModifier  = Key.None;
    private MouseButton swallowedRelease = MouseButton.None;
    private Label       statusLabel;

    public override void _Ready()
    {
        statusLabel = GetNode<Label>("%BindingStatus");

        var rows = GetNode<Container>("%BindingRows");

        foreach (var (action, label) in InputActions.Rebindable)
            rows.AddChild(CreateRow(action, label));

        GetNode<Button>("%ResetButton").Pressed += () =>
        {
            UserSettings.Instance?.ResetBindings();

            Cancel("All keys are back to their defaults.");
        };
    }

    public void ShowCurrent()
    {
        swallowedRelease = MouseButton.None;

        Cancel(string.Empty);
    }

    public void Commit()
    {
        Cancel(string.Empty);
    }

    //Wer das Spiel verlässt, etwa mit Alt+Tab, belegt nichts. Auch nicht mit dem Klick, der ihn zurückholt
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && capturing is not null)
            Cancel("Cancelled.");
    }

    public bool TakesInput(InputEvent inputEvent)
    {
        //Das Loslassen der abgefangenen Maustaste gehört noch dazu, sonst drückte es den Knopf darunter
        if (inputEvent is InputEventMouseButton { Pressed: false } release && release.ButtonIndex == swallowedRelease)
        {
            swallowedRelease = MouseButton.None;

            return true;
        }

        if (capturing is null)
            return false;

        switch (inputEvent)
        {
            case InputEventKey { Pressed: true, Echo: false } key when key.PhysicalKeycode == Key.Escape || key.Keycode == Key.Escape:
                Cancel("Cancelled.");

                return true;
            case InputEventKey { Pressed: true, Echo: false } key when pendingModifier != Key.None:
                Cancel("Key combinations are not supported.");

                return true;
            //Alt, Strg, Umschalt und Windows gelten erst beim Loslassen. Sonst belegte Alt+Tab schon Alt
            case InputEventKey { Pressed: true, Echo: false } key when IsModifier(key):
                pendingModifier = key.PhysicalKeycode;

                return true;
            case InputEventKey { Pressed: false } key when pendingModifier != Key.None && key.PhysicalKeycode == pendingModifier:
                pendingModifier = Key.None;

                Bind(KeyBindings.ToBinding(key));

                return true;
            case InputEventKey { Pressed: true, Echo: false } key:
                Bind(KeyBindings.ToBinding(key));

                return true;
            case InputEventKey:
                return true;
            //Das Rad scrollt die Liste. Es lässt sich nicht belegen
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight }:
                return false;
            case InputEventMouseButton { Pressed: true } when pendingModifier != Key.None:
                Cancel("Key combinations are not supported.");

                return true;
            case InputEventMouseButton { Pressed: true } mouse:
                swallowedRelease = mouse.ButtonIndex;

                Bind(KeyBindings.ToBinding(mouse));

                return true;
            case InputEventMouseButton:
                return true;
            default:
                return false;
        }
    }

    private static bool IsModifier(InputEventKey key)
        => key.PhysicalKeycode is Key.Alt or Key.Ctrl or Key.Shift or Key.Meta;

    private void Cancel(string status)
    {
        capturing       = null;
        pendingModifier = Key.None;

        ShowBindings();
        ShowStatus(status);
    }

    private HBoxContainer CreateRow(StringName action, string label)
    {
        var row    = new HBoxContainer { Name = $"{action}Row" };
        var name   = new Label { Text = label, CustomMinimumSize = new Vector2(420, 0) };
        var button = new Button { Name = $"{action}Button", CustomMinimumSize = new Vector2(300, 52) };

        name.AddThemeFontSizeOverride("font_size", 28);
        button.AddThemeFontSizeOverride("font_size", 28);

        button.Pressed += () => StartCapture(action);

        buttons[action] = button;

        row.AddChild(name);
        row.AddChild(button);

        return row;
    }

    private void StartCapture(string action)
    {
        capturing = action;

        ShowBindings();

        buttons[action].Text = BindingRules.IsSkillSlot(action) ? "Press a key or mouse button..." : "Press a key...";

        ShowStatus("Escape cancels.");
    }

    private void Bind(InputBinding binding)
    {
        var action = capturing;

        capturing = null;

        if (UserSettings.Instance is not { } settings)
            return;

        var before = settings.Bindings.GetValueOrDefault(action);
        var result = settings.Rebind(action, binding);
        var key    = KeyBindings.GetLabel(binding);

        ShowBindings();

        ShowStatus(result.Outcome switch
        {
            BindingOutcome.Bound              => $"{LabelOf(action)} is now on {key}.",
            BindingOutcome.Swapped            => $"{LabelOf(action)} is now on {key}. {LabelOf(result.OtherAction)} moved to {KeyBindings.GetLabel(before)}.",
            BindingOutcome.Unchanged          => $"{LabelOf(action)} already is on {key}.",
            BindingOutcome.MouseOnlyForSkills => "Mouse buttons are for skills only.",
            BindingOutcome.SwapImpossible     => $"{key} belongs to {LabelOf(result.OtherAction)}, which can't take {KeyBindings.GetLabel(before)}.",
            _                                 => $"{key} is reserved."
        });
    }

    private void ShowBindings()
    {
        var bindings = UserSettings.Instance?.Bindings;

        foreach (var (action, button) in buttons)
            button.Text = bindings is not null && bindings.TryGetValue(action, out var binding) ? KeyBindings.GetLabel(binding) : InputActions.GetKeyLabel(action);
    }

    private void ShowStatus(string text)
        => statusLabel.Text = text;

    private static string LabelOf(string action)
        => InputActions.Rebindable.FirstOrDefault(entry => entry.Action == action).Label ?? action;
}
