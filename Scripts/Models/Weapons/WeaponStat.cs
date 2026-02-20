using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Models.Weapons;

public enum WeaponStat
{
    Undefined,
    [Description("Physical Damage")]
    PhysicalDamage,
    [Description("Elemental Damage")]
    ElementalDamage,
    [Description("Attackspeed")]
    AttackSpeed,
    [Description("Critical Hit Chance")]
    CriticalHitChance,
    //...
}