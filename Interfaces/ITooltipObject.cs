using Godot;

namespace Hoellenspiralenspiel.Interfaces;

public interface ITooltipObject
{
    [Export]
    public string TooltipTitle { get; set; }

    string GetTooltipDescription();
}