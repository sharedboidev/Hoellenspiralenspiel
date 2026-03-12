using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class XpBar : Control
{
    private TextureProgressBar xpBar;

    public override void _Ready()
    {
        xpBar = GetNode<TextureProgressBar>("%Bar");

        var viewportSize = GetViewportRect().Size;

        var xPositionBar = viewportSize.X - xpBar.Size.X;
        var yPositionBar = viewportSize.Y - xpBar.Size.Y +10;

        Position = new Vector2(xPositionBar, yPositionBar);
    }
}