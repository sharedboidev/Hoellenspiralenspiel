using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class LevelUpDialog : Control, IClosableWindow
{
    private          IHero hero;
    [Export] private Node  player;

    public override void _Ready()
    {
        hero = player as IHero;

        if (hero is null)
            GD.PushError($"Der Level-up-Dialog braucht einen Helden, {player?.Name} ist keiner.");

        SubscribeClickEvents();
    }

    public bool IsOpen => Visible;

    public void ShowDialog()
        => Visible = true;

    public void Close() => Hide();

    private void SubscribeClickEvents()
    {
        GetNode<RaiseAttributeComponent>("%RaiseStrengthComponent").AttributeRaisedClicked  += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseDexComponent").AttributeRaisedClicked       += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseIntComponent").AttributeRaisedClicked       += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseConstiComponent").AttributeRaisedClicked    += OnAttributeRaisedClicked;
        GetNode<RaiseAttributeComponent>("%RaiseAwarenessComponent").AttributeRaisedClicked += OnAttributeRaisedClicked;
    }

    private void OnAttributeRaisedClicked(Attributes attribute)
    {
        if (hero is null)
            return;

        hero.RaiseAttribute(attribute);

        Visible = hero.AttributePoints > 0;
    }
}
