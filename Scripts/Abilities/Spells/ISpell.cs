using Godot;

namespace Hoellenspiralenspiel.Scripts.Abilities.Spells;

public interface ISpell
{
    void Init(BaseSkill skill,
              Vector2   globalPlayerPosition,
              Vector2   globalMousePosition);
}