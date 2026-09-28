using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Abilities;

public class FireballSkill : BaseSpell
{
    public FireballSkill(BaseUnit owner)
            : this(owner, 50, 75) { }

    //Gegner wirken denselben Zauber mit eigenem Grundschaden
    public FireballSkill(BaseUnit owner, int baseDamageMin, int baseDamageMax)
            : base(baseDamageMin, baseDamageMax, 10, 0.25d, DamageType.Fire, owner) { }
}
