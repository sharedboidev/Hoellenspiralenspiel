using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Buttons;

public partial class OpenLevelUpDialogButton : TextureButton
{
    public delegate void OpenDialogPressedEventHandler();

    public event OpenDialogPressedEventHandler OpenDialogPressed;

    public override void _Ready()
        => SetPositionInViewport();

    private void SetPositionInViewport()
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize    = Size;

        var position = (viewportSize * new Vector2(.65f, 1.6f) - panelSize) / 2;
        Position = position;
    }

    public void _on_open_level_up_dialog_button_up()
    {
        OpenDialogPressed?.Invoke();

        Visible = false;
    }
}