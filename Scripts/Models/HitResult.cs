using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Models;

public record HitResult(float RawValue, HitType HitType, LifeModificationMode LifeModificationMode, BaseUnit Target, CombatStat MitigatedBy)
{
    public int MitigatedDamage => MitigatedBy is CombatStat.Armor ? GetMitigatenPhysicalDamage() : GetMitigatedElementalDamage();

    private int GetMitigatenPhysicalDamage() => GetMitigatedDamage(() =>
    {
        //Armor Formula, In eigenen Typen verschieben
        var mitigatedDamage = RawValue - Target.ArmorFinal * RawValue / (Target.ArmorFinal + 5 * RawValue);

        return (int)mitigatedDamage;
    });

    private int GetMitigatedElementalDamage() => MitigatedBy switch
    {
        CombatStat.FireResistance => GetMitigatedDamage(() => MitigateByResistanceValue(Target.FireResiFinal)),
        CombatStat.FrostResistance => GetMitigatedDamage(() => MitigateByResistanceValue(Target.FrostResiFinal)),
        CombatStat.LightningResistance => GetMitigatedDamage(() => MitigateByResistanceValue(Target.LightningResiFinal)),
        _ => (int)RawValue
    };

    //Elemental Resi Formula, In eigenen Typen verschieben
    private int MitigateByResistanceValue(int resistanceValue) => (int)(((float)100 - resistanceValue) / 100 * RawValue);

    private int GetMitigatedDamage(Func<int> calculateMitigation)
    {
        if (LifeModificationMode != LifeModificationMode.Damage)
            return (int)RawValue;

        return calculateMitigation();
    }
}