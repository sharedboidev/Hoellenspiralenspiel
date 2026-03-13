using Godot;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class RaiseAttributeComponent : PanelContainer
{
    public delegate void AttributeRaisedClickedEventHandler(Attributes attribute);

    [Export]
    public Attributes Attribute { get; set; }

    public event AttributeRaisedClickedEventHandler AttributeRaisedClicked;

    public override void _Ready()
        => GetNode<Label>("%Attribute").Text = $"{Attribute}";

    public void _on_raise_attribute_button_down()
        => AttributeRaisedClicked?.Invoke(Attribute);
}