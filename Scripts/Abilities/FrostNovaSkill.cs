using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class FrostNovaSkill : BaseSpell
{
    public FrostNovaSkill(BaseUnit owner)
            : base(10, 50, 10, baseCooldown: .5, owner) { }
}