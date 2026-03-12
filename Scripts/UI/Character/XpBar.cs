using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class XpBar : Control
{
    private TextureProgressBar xpBar;
    private Label              xpDisplayLabel;

    public override void _Ready()
    {
        LoadNodes();
        SetPositionInViewport();
    }

    private void SetPositionInViewport()
    {
        var viewportSize = GetViewportRect().Size;

        var xPositionBar = (viewportSize.X - xpBar.Size.X * Scale.X) / 2;
        var yPositionBar = viewportSize.Y - xpBar.Size.Y * Scale.Y - 10;

        Position = new Vector2(xPositionBar, yPositionBar);
    }

    public void SetMaxValue(Player2D player)
    {
        
    }

    public void SetCurrentValue(Player2D player)
    {
        
    }

    private void LoadNodes()
    {
        xpBar          = GetNode<TextureProgressBar>("%Bar");
        xpDisplayLabel = GetNode<Label>("%XpDisplay");
    }

    public void _on_bar_mouse_exited()
        => xpDisplayLabel.Visible = false;

    public void _on_bar_mouse_entered()
        => xpDisplayLabel.Visible = true;
}