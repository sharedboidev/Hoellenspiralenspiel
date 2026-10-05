using Godot;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Commandline : Control
{
    public delegate void SpawnUnitsEvent(string unitId, int amount);

    [Export] private TextEdit textEdit;
    public event SpawnUnitsEvent SpawnUnits;

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey {Keycode: Key.Enter} keyEvent || !keyEvent.IsReleased())
            return;

        if (Visible)
            ExecuteCommand();
        else
            ShowCommandline();
    }

    private void ShowCommandline()
    {
        Visible = true;
        
        textEdit.GrabFocus();
    }

    private void ExecuteCommand()
    {
        var commandInterpretation = CommandResolver.Resolve(textEdit.Text);

        HideCommandline();
        
        if (commandInterpretation is not SpawnDefinition spawnDefinition)
            return;

        SpawnUnits?.Invoke(spawnDefinition.UnitId, spawnDefinition.Amount);
    }

    private void HideCommandline()
    {
        textEdit.Clear();
        
        Visible = false;
    }
}