
namespace Hoellenspiralenspiel.Interfaces;

public interface ITooltipObject
{
    public string TooltipTitle { get; }

    string GetTooltipDescription();
}