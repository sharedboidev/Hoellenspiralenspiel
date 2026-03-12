using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class XpBar : Control
{
    private TextureProgressBar xpBar;

    public override void _Ready()
    {
        xpBar = GetNode<TextureProgressBar>("%Bar");

        var viewportSize = GetViewportRect().Size;

        var xPositionBar = (viewportSize.X - xpBar.Size.X * Scale.X) / 2;
        var yPositionBar = viewportSize.Y - xpBar.Size.Y * Scale.Y - 10;

        GlobalPosition = new Vector2(xPositionBar, yPositionBar);
    }
}