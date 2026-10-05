using Godot;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class Commandline : Control
{
    [Export] private TextEdit textEdit;

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.Enter } keyEvent || !keyEvent.IsReleased())
            return;

        if (Visible)
            ExecuteCommand();
        else
            ShowCommandline();
    }

    private void ShowCommandline()
        => Visible = true;

    private void ExecuteCommand()
    {
        CommandResolver.Resolve(textEdit.Text);
        
        HideCommandline();
    }

    private void HideCommandline()
        => Visible = false;
}