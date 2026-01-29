using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class FireballSkill : BaseSpell
{


    public FireballSkill(BaseUnit owner)
            : base(50, 250, 10, 0.25d, owner){}
}