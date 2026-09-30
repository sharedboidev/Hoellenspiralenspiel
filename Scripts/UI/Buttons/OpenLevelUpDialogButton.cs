using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Buttons;

public partial class OpenLevelUpDialogButton : TextureButton
{
    public delegate void OpenDialogPressedEventHandler();

    public Label SpendablePointLabel { get; set; }

    public event OpenDialogPressedEventHandler OpenDialogPressed;

    public override void _Ready()
    {
        SpendablePointLabel = GetNode<Label>("%SpendablePointsLabel");
    }

    public void _on_open_level_up_dialog_button_up()
    {
        OpenDialogPressed?.Invoke();

        Visible = false;
    }
}