using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class LevelDisplay : PanelContainer
{
    private Label label;

    public override void _Ready() => label = GetNode<Label>("%" + nameof(Label));

    public void SetDisplayedValue(int level) => label.Text = $"{level:N0}";
}