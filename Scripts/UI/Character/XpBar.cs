using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class XpBar : Control
{
    private          IHero              hero;
    [Export] private Node               player;
    private          TextureProgressBar xpBar;
    private          Label              xpDisplayLabel;

    public override void _Ready()
    {
        xpBar          = GetNode<TextureProgressBar>("%Bar");
        xpDisplayLabel = GetNode<Label>("%XpDisplay");
        hero           = player as IHero;

        SetPositionInViewport();

        if (hero is null)
        {
            GD.PushError($"Der XP-Balken braucht einen Helden, {player?.Name} ist keiner.");

            return;
        }

        hero.XpChanged += SetValues;

        SetValues();
    }

    public override void _ExitTree()
    {
        if (hero is not null)
            hero.XpChanged -= SetValues;
    }

    private void SetValues()
    {
        xpBar.MinValue = 0;
        xpBar.MaxValue = hero.XpForNextLevel - hero.XpFloorCurrentLevel;
        xpBar.Value    = hero.XpTotal - hero.XpFloorCurrentLevel;

        xpDisplayLabel.Text = $"{xpBar.Value:N0}/{xpBar.MaxValue:N0}";
    }

    private void SetPositionInViewport()
    {
        var viewportSize = GetViewportRect().Size;

        var xPositionBar = (viewportSize.X - xpBar.Size.X * Scale.X) / 2;
        var yPositionBar = viewportSize.Y - xpBar.Size.Y * Scale.Y - 10;

        Position = new Vector2(xPositionBar, yPositionBar);
    }

    public void _on_bar_mouse_exited()
        => xpDisplayLabel.Visible = false;

    public void _on_bar_mouse_entered()
        => xpDisplayLabel.Visible = true;
}
