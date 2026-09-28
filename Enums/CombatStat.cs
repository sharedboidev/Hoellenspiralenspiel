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
    [Description("Fire Resistance")]
    FireResistance,
    [Description("Frost Resistance")]
    FrostResistance,
    [Description("Lightning Resistance")]
    LightningResistance,
    [Description("Spell Damage")]
    SpellDamage,
    [Description("Melee Parry")]
    MeleeParry,
    [Description("Spell Parry")]
    SpellParry,
    [Description("Melee Block")]
    MeleeBlock,
    [Description("Spell Block")]
    SpellBlock,

    //Neue Werte nur hier am Ende anhängen: Affixe speichern den Stat als Zahl
    [Description("Light Radius")]
    LightRadius,
    Movementspeed,
    [Description("Hit Chance")]
    HitChance,

    //Anteil des Schadens in Prozent, den ein Block abfängt
    [Description("Blocked Damage")]
    BlockReduction
}