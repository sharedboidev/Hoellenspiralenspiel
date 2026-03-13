using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class LevelUpDialog : Control
{
    private Player2D player;

    public override void _Ready()
    {
        LoadNodes();
        SubscribeClickEvents();
        SetPositionInViewport();
    }

    public void ShowDialog()
        => Visible = true;

    private void LoadNodes()
        => player = GetTree().CurrentScene.GetNodeOrNull<Player2D>("%Player 2D");

    private void SubscribeClickEvents()
    {
        GetNode<RaiseAttributeComponent>("%RaiseStrengthComponent").AttributeRaisedClicked  += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseDexComponent").AttributeRaisedClicked       += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseIntComponent").AttributeRaisedClicked       += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseConstiComponent").AttributeRaisedClicked    += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseAwarenessComponent").AttributeRaisedClicked += OnAttributeRaisedClicked;
    }

    private void SetPositionInViewport()
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize    = GetNode<PanelContainer>(nameof(PanelContainer)).Size;

        var position = (viewportSize*new Vector2(1f, 1.25f) - panelSize) / 2;
        Position = position;
    }

    private void OnAttributeRaisedClicked(Attributes attribute)
    {
        if (player is null)
            return;

        switch (attribute)
        {
            case Attributes.Strength:
                player.StrengthBase++;

                break;
            case Attributes.Dexterity:
                player.DexterityBase++;

                break;
            case Attributes.Intelligence:
                player.IntelligenceBase++;

                break;
            case Attributes.Constitution:
                player.ConstitutionBase++;

                break;
            case Attributes.Awareness:
                player.AwarenessBase++;

                break;
        }

        GD.Print($"{attribute} raised");

        player.AttributePointsAllowedToSpend--;
        
        Visible = player.AttributePointsAllowedToSpend > 0;
    }
}