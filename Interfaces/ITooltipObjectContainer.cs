using Godot;

namespace Hoellenspiralenspiel.Interfaces;

public interface ITooltipObjectContainer
{
    public ITooltipObject ContainedItem { get; set; }
    public Vector2        Position      { get; set; }
    public Vector2        Size          { get; set; }
}