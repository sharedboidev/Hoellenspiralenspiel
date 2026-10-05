using System.ComponentModel;

namespace Hoellenspiralenspiel.Enums;

public enum CombatStat
{
    Life,
    Mana,
    Damagereduction,
    //In Prozent des physischen Schadens eines Angriffs, siehe ManaLeech
    [Description("Life Leech")]
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

    [Description("Blocked Damage")]
    BlockReduction,

    [Description("Projectiles")]
    ProjectileCount,

    //Wirkt nur auf Angriffe, ElementalDamage auch auf Zauber
    [Description("Elemental Damage with Attacks")]
    ElementalAttackDamage,

    //Flach und lokal fügt ein Element der Waffe Schaden hinzu, in Prozent erhöht es den Schaden dieses Elements
    [Description("Fire Damage")]
    FireDamage,
    [Description("Frost Damage")]
    FrostDamage,
    [Description("Lightning Damage")]
    LightningDamage,

    //Für jeden getroffenen Gegner eines Angriffs einzeln
    [Description("Life per Enemy Hit")]
    LifeOnHit,
    [Description("Life per Enemy Killed")]
    LifeOnKill,
    [Description("Mana per Enemy Killed")]
    ManaOnKill,

    [Description("Mana Leech")]
    ManaLeech,

    //Prozent erhöhen den Schaden über Zeit, More ist der Multiplikator auf ihn
    [Description("Damage over Time")]
    DamageOverTime,

    //Lokal und negativ: senkt die Anforderungen an Attribute, nie die an das Level
    [Description("Attribute Requirements")]
    AttributeRequirements
}