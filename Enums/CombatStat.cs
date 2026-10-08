using System.ComponentModel;

namespace Hoellenspiralenspiel.Enums;

public enum CombatStat
{
    [Description("maximum Life")]
    Life,
    [Description("maximum Mana")]
    Mana,
    //In Prozent, mindert physischen Schaden nach der Rüstung, höchstens um 90 %
    [Description("additional Physical Damage Reduction")]
    Damagereduction,
    //In Prozent des physischen Schadens eines Angriffs, siehe ManaLeech
    [Description("Life Leech")]
    Leech,
    Dodge,
    [Description("Life Regeneration")]
    Liferegeneration,
    [Description("Mana Regeneration")]
    Manaregeneration,
    [Description("Elemental Damage")]
    ElementalDamage,
    [Description("Physical Damage")]
    PhysicalDamage,
    [Description("Attack Speed")]
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
    AttributeRequirements,

    //Global "Adds X to Y": X steht im Stat ohne Max, Y im Stat mit Max. Er zählt zum Grundschaden jeder Attack oder jedes Zaubers
    [Description("Physical Damage to Attacks")]
    AddedPhysicalToAttacks,
    AddedPhysicalToAttacksMax,
    [Description("Fire Damage to Attacks")]
    AddedFireToAttacks,
    AddedFireToAttacksMax,
    [Description("Frost Damage to Attacks")]
    AddedFrostToAttacks,
    AddedFrostToAttacksMax,
    [Description("Lightning Damage to Attacks")]
    AddedLightningToAttacks,
    AddedLightningToAttacksMax,
    [Description("Fire Damage to Spells")]
    AddedFireToSpells,
    AddedFireToSpellsMax,
    [Description("Frost Damage to Spells")]
    AddedFrostToSpells,
    AddedFrostToSpellsMax,
    [Description("Lightning Damage to Spells")]
    AddedLightningToSpells,
    AddedLightningToSpellsMax,

    //In Prozent des physischen Schadens eines Nahkampftreffers vor der Minderung
    [Description("Physical Damage reflected to Melee Attackers")]
    ReflectPhysical,

    //Sammel-Stats: Das StatSheet gibt ihren Wert an jede der drei Resistenzen weiter
    [Description("all Elemental Resistances")]
    AllElementalResistances,

    //Grundwert 75 %, mehr als 90 % zählt nie
    [Description("maximum Fire Resistance")]
    MaxFireResistance,
    [Description("maximum Frost Resistance")]
    MaxFrostResistance,
    [Description("maximum Lightning Resistance")]
    MaxLightningResistance,
    [Description("all maximum Resistances")]
    AllMaximumResistances,

    //In Prozent, gilt für jeden Effekt eines Treffers: Bleed, Burn, Chill, Shock
    [Description("chance to Avoid Ailments")]
    AilmentAvoidance,

    //In Prozent des Zusatzschadens, den ein Krit auf diese Einheit macht
    [Description("reduced Extra Damage from Critical Strikes")]
    ReducedCriticalDamageTaken,

    [Description("Cast Speed")]
    CastSpeed,

    //Wirkt nur auf Zauber, CriticalHitChance auf alles
    [Description("Spell Critical Strike Chance")]
    SpellCriticalHitChance,

    //More-Modifier auf Schaden über Zeit einer Schadensart: Bleed ist physisch, Burn Feuer
    [Description("Physical Damage over Time")]
    PhysicalDamageOverTime,
    [Description("Fire Damage over Time")]
    FireDamageOverTime,

    [Description("Projectile Speed")]
    ProjectileSpeed,

    //Weitere Sprünge für Skills, die von Ziel zu Ziel springen. Zusätzliche Projektile zählen für sie nicht
    [Description("Proliferate")]
    Proliferate
}