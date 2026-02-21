using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class LightningStrikeSkill : BaseSpell
{
    public LightningStrikeSkill(BaseUnit owner) 
    : base(50, 350, 25, .1f, owner) { }
}