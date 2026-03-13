using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class XpBar : Control
{
    private TextureProgressBar xpBar;
    private Label              xpDisplayLabel;
    private Player2D           player;

    public override void _Ready()
    {
        LoadNodes();
        SetPositionInViewport();
        SetValues();
    }

    private void SetValues()
    {
        SetBorderValues();
        SetCurrentValue();

        xpDisplayLabel.Text = $"{xpBar.Value:N0}/{xpBar.MaxValue:N0}";
    }

    private void SetPositionInViewport()
    {
        var viewportSize = GetViewportRect().Size;

        var xPositionBar = (viewportSize.X - xpBar.Size.X * Scale.X) / 2;
        var yPositionBar = viewportSize.Y - xpBar.Size.Y * Scale.Y - 10;

        Position = new Vector2(xPositionBar, yPositionBar);
    }

    public void SetBorderValues()
    {
        xpBar.MinValue = 0;
        xpBar.MaxValue = player.XpForNextLevel - player.XpFloorCurrentLevel;
    }

    public void SetCurrentValue()
        => xpBar.Value = player.XpDelta;

    private void LoadNodes()
    {
        xpBar          = GetNode<TextureProgressBar>("%Bar");
        xpDisplayLabel = GetNode<Label>("%XpDisplay");
        player         = GetTree().CurrentScene.GetNodeOrNull<Player2D>("%Player 2D");
        
        if(player is null)
            return;
        
        player.PropertyChanged += PlayerOnPropertyChanged;
    }

    private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName != nameof(Player2D.XpTotal))
            return;

        SetValues();
    }

    public void _on_bar_mouse_exited()
        => xpDisplayLabel.Visible = false;

    public void _on_bar_mouse_entered()
        => xpDisplayLabel.Visible = true;
}