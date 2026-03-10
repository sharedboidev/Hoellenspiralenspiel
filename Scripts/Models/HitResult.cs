using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Models;

public record HitResult(float RawValue, HitType HitType, LifeModificationMode LifeModificationMode, BaseUnit Target)
{
    public int MitigatedDamage => GetMitigatenPhysicalDamage();

    private int GetMitigatenPhysicalDamage()
    {
        if (LifeModificationMode != LifeModificationMode.Damage)
            return (int)RawValue;

        var rawDamage       = RawValue;
        var mitigatedDamage = RawValue - Target.ArmorFinal * rawDamage / (Target.ArmorFinal + 5 * rawDamage);

        return (int)mitigatedDamage;
    }
}