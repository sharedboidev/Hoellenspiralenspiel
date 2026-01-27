using Godot;

namespace Hoellenspiralenspiel.Interfaces;

public interface ITooltipObjectContainer
{
    public ITooltipObject ContainedItem      { get; set; }
    public Vector2        TooltipAnchorPoint { get; }
    public Vector2        Size               { get; set; }
}