using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Objects;

//Etwas in der Welt, zu dem der Held hinläuft und das er dann benutzt
public interface IUsable
{
    Vector3 GlobalPosition { get; }

    bool IsInReachOf(BaseUnit unit);

    void Use();

    void SetHighlight(bool active);
}
