using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem : Node2D,
                                         ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public abstract bool   IsStackable  { get; }
    public          string TooltipTitle => Name;

    public abstract string GetTooltipDescription();

    public override void _Ready() => Icon = GetNode<TextureRect>("Icon");
}