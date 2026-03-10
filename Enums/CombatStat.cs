using System.ComponentModel;

namespace Hoellenspiralenspiel.Enums;

public enum CombatStat
{
    Life,
    Mana,
    Damagereduction,
    Leech,
    Dodge,
    Liferegeneration,
    Manaregeneration,
    [Description("Elemental Damage")]
    ElementalDamage,
    [Description("Physical Damage")]
    PhysicalDamage,
    Attackspeed,
    [Description("Critical Hitchance")]
    CriticalHitChance,
    [Description("Critical Damage")]
    CriticalDamage,
    Range,
    Armor,
    Strength,
    Dexterity,
    Intelligence,
    Constitution,
    Awareness,
    AreaOfEffect,
    FireResistance,
    FrostResistance,
    LightningResistance
}