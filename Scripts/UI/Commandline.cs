using Godot;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Commandline : Control
{
    public delegate void SpawnUnitsEvent(string unitId, int amount);

    [Export] private LineEdit lineEdit;
    public event SpawnUnitsEvent SpawnUnits;

    public override void _Ready()
        => lineEdit.TextSubmitted += ExecuteCommand;

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.KpEnter })
            return;

        ShowCommandline();

        GetViewport().SetInputAsHandled();
    }

    private void ShowCommandline()
    {
        Visible = true;

        lineEdit.Edit();
    }

    private void ExecuteCommand(string commandInput)
    {
        HideCommandline();

        switch (CommandResolver.Resolve(commandInput))
        {
            case SpawnDefinition spawnDefinition:
                SpawnUnits?.Invoke(spawnDefinition.UnitId, spawnDefinition.Amount);

                break;

            case InvalidCommand invalidCommand:
                GD.PushWarning(invalidCommand.Reason);

                break;
        }
    }

    private void HideCommandline()
    {
        lineEdit.Clear();

        Visible = false;
    }
}
