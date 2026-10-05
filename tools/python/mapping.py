"""Welche Familie aus poedb welche Affix-Datei wird.

Ein Eintrag je Vorlage aus data/<Seite>.json: (Datei, Stat, ModificationType, lokal, Brüche, negativ) und bei einem
hybriden Affix dazu (Stat, ModificationType, lokal) seines zweiten Stats. Lokal heißt, der Affix verändert das Item selbst.
Negativ dreht die Werte um, aus "18% reduced" wird -18. Vorlagen, die hier fehlen, übernimmt generate.py nicht.
"""

FLAT, PERCENTAGE, MORE = 0, 1, 2

WEAPON = {
    '#% increased Physical Damage': ('IncreasedPhysicalDamage', 'PhysicalDamage', PERCENTAGE, True, False, False),
    'Adds # to # Physical Damage': ('AddedPhysicalDamage', 'PhysicalDamage', FLAT, True, False, False),
    'Adds # to # Fire Damage': ('AddedFireDamage', 'FireDamage', FLAT, True, False, False),
    'Adds # to # Cold Damage': ('AddedFrostDamage', 'FrostDamage', FLAT, True, False, False),
    'Adds # to # Lightning Damage': ('AddedLightningDamage', 'LightningDamage', FLAT, True, False, False),
    '#% increased Elemental Damage with Attack Skills': ('IncreasedElementalDamage', 'ElementalAttackDamage', PERCENTAGE, False, False, False),
    '+# to Strength': ('FlatStrength', 'Strength', FLAT, False, False, False),
    '+# to Dexterity': ('FlatDexterity', 'Dexterity', FLAT, False, False, False),
    '+# to Intelligence': ('FlatIntelligence', 'Intelligence', FLAT, False, False, False),
    '#% increased Attack Speed': ('IncreasedAttackspeed', 'Attackspeed', PERCENTAGE, True, False, False),
    '#% increased Critical Strike Chance': ('IncreasedCriticalHitChance', 'CriticalHitChance', PERCENTAGE, True, False, False),
    '+#% to Global Critical Strike Multiplier': ('FlatCriticalDamage', 'CriticalDamage', FLAT, False, False, False),
    'Grants # Life per Enemy Hit': ('LifeOnHit', 'LifeOnHit', FLAT, False, False, False),
    'Gain # Life per Enemy Killed': ('LifeOnKill', 'LifeOnKill', FLAT, False, False, False),
    'Gain # Mana per Enemy Killed': ('ManaOnKill', 'ManaOnKill', FLAT, False, False, False),
    '#% reduced Attribute Requirements': ('ReducedAttributeRequirements', 'AttributeRequirements', PERCENTAGE, True, False, True),
    '#% of Physical Attack Damage Leeched as Life': ('LifeLeech', 'Leech', FLAT, False, True, False),
    '#% of Physical Attack Damage Leeched as Mana': ('ManaLeech', 'ManaLeech', FLAT, False, True, False),
    '+#% to Damage over Time Multiplier': ('DamageOverTimeMultiplier', 'DamageOverTime', MORE, False, False, False),
    '+# to maximum Mana': ('FlatMana', 'Mana', FLAT, False, False, False),
    '#% increased Spell Damage': ('IncreasedSpellDamage', 'SpellDamage', PERCENTAGE, False, False, False),
    '#% increased Fire Damage': ('IncreasedFireDamage', 'FireDamage', PERCENTAGE, False, False, False),
    '#% increased Cold Damage': ('IncreasedFrostDamage', 'FrostDamage', PERCENTAGE, False, False, False),
    '#% increased Lightning Damage': ('IncreasedLightningDamage', 'LightningDamage', PERCENTAGE, False, False, False),
    '#% increased Mana Regeneration Rate': ('IncreasedManaRegeneration', 'Manaregeneration', PERCENTAGE, False, False, False),
    '+#% Chance to Block Spell Damage': ('FlatSpellBlock', 'SpellBlock', FLAT, False, False, False),
    '+#% Chance to Block Attack Damage': ('FlatMeleeBlock', 'MeleeBlock', FLAT, False, False, False),
    '#% increased Projectile Speed': ('IncreasedProjectileSpeed', 'ProjectileSpeed', PERCENTAGE, False, False, False),
    'Bow Attacks fire # additional Arrows': ('AdditionalArrows', 'ProjectileCount', FLAT, True, False, False),
    '#% increased Spell Damage / +# to maximum Mana': ('IncreasedSpellDamageAndMana', 'SpellDamage', PERCENTAGE, False, False, False, ('Mana', FLAT, False)),
    'Adds # to # Fire Damage to Spells': ('AddedFireDamageToSpells', 'AddedFireToSpells', FLAT, False, False, False),
    'Adds # to # Cold Damage to Spells': ('AddedFrostDamageToSpells', 'AddedFrostToSpells', FLAT, False, False, False),
    'Adds # to # Lightning Damage to Spells': ('AddedLightningDamageToSpells', 'AddedLightningToSpells', FLAT, False, False, False),
    '#% increased Cast Speed': ('IncreasedCastSpeed', 'CastSpeed', PERCENTAGE, False, False, False),
    '#% increased Spell Critical Strike Chance': ('IncreasedSpellCriticalHitChance', 'SpellCriticalHitChance', PERCENTAGE, False, False, False),
    '+#% to Fire Damage over Time Multiplier': ('FireDamageOverTimeMultiplier', 'FireDamageOverTime', MORE, False, False, False),
    '+#% to Physical Damage over Time Multiplier': ('PhysicalDamageOverTimeMultiplier', 'PhysicalDamageOverTime', MORE, False, False, False),
}

ARMOUR = {
    '+# to maximum Life': ('FlatLife', 'Life', FLAT, False, False, False),
    '#% increased Armour': ('IncreasedArmor', 'Armor', PERCENTAGE, True, False, False),
    '+# to Armour': ('FlatArmor', 'Armor', FLAT, True, False, False),
    '+# to Strength': ('FlatStrength', 'Strength', FLAT, False, False, False),
    '+# to Dexterity': ('FlatDexterity', 'Dexterity', FLAT, False, False, False),
    '+# to Intelligence': ('FlatIntelligence', 'Intelligence', FLAT, False, False, False),
    'Regenerate # Life per second': ('FlatLifeRegeneration', 'Liferegeneration', FLAT, False, True, False),
    '#% increased Life Regeneration rate': ('IncreasedLifeRegeneration', 'Liferegeneration', PERCENTAGE, False, False, False),
    '+#% to Fire Resistance': ('FireResistance', 'FireResistance', FLAT, False, False, False),
    '+#% to Cold Resistance': ('FrostResistance', 'FrostResistance', FLAT, False, False, False),
    '+#% to Lightning Resistance': ('LightningResistance', 'LightningResistance', FLAT, False, False, False),
    '#% reduced Attribute Requirements': ('ReducedAttributeRequirements', 'AttributeRequirements', PERCENTAGE, True, False, True),
    '#% increased Attack Speed': ('IncreasedAttackspeed', 'Attackspeed', PERCENTAGE, False, False, False),
    '#% of Physical Attack Damage Leeched as Life': ('LifeLeech', 'Leech', FLAT, False, True, False),
    '#% of Physical Attack Damage Leeched as Mana': ('ManaLeech', 'ManaLeech', FLAT, False, True, False),
    'Gain # Life per Enemy Hit with Attacks': ('LifeOnHit', 'LifeOnHit', FLAT, False, False, False),
    'Gain # Life per Enemy Killed': ('LifeOnKill', 'LifeOnKill', FLAT, False, False, False),
    'Gain # Mana per Enemy Killed': ('ManaOnKill', 'ManaOnKill', FLAT, False, False, False),
    '#% Chance to Block Spell Damage': ('FlatSpellBlock', 'SpellBlock', FLAT, False, False, False),
    '+# to Armour / +# to maximum Life': ('FlatArmorAndLife', 'Armor', FLAT, True, False, False, ('Life', FLAT, False)),
    'Adds # to # Physical Damage to Attacks': ('AddedPhysicalDamageToAttacks', 'AddedPhysicalToAttacks', FLAT, False, False, False),
    'Adds # to # Fire Damage to Attacks': ('AddedFireDamageToAttacks', 'AddedFireToAttacks', FLAT, False, False, False),
    'Adds # to # Cold Damage to Attacks': ('AddedFrostDamageToAttacks', 'AddedFrostToAttacks', FLAT, False, False, False),
    'Adds # to # Lightning Damage to Attacks': ('AddedLightningDamageToAttacks', 'AddedLightningToAttacks', FLAT, False, False, False),
    'Reflects # Physical Damage to Melee Attackers': ('ReflectPhysicalDamage', 'ReflectPhysical', FLAT, False, False, False),
    '#% additional Physical Damage Reduction': ('PhysicalDamageReduction', 'Damagereduction', FLAT, False, False, False),
    '#% increased Chance to Block': ('IncreasedBlockChance', 'MeleeBlock', PERCENTAGE, True, False, False),
    '+#% to all Elemental Resistances': ('AllElementalResistances', 'AllElementalResistances', FLAT, False, False, False),
    '#% chance to Avoid Elemental Ailments': ('AilmentAvoidance', 'AilmentAvoidance', FLAT, False, False, False),
    'You take #% reduced Extra Damage from Critical Strikes': ('ReducedCriticalDamageTaken', 'ReducedCriticalDamageTaken', FLAT, False, False, False),
    '#% to maximum Fire Resistance': ('MaxFireResistance', 'MaxFireResistance', FLAT, False, False, False),
    '#% to maximum Cold Resistance': ('MaxFrostResistance', 'MaxFrostResistance', FLAT, False, False, False),
    '#% to maximum Lightning Resistance': ('MaxLightningResistance', 'MaxLightningResistance', FLAT, False, False, False),
    '#% to all maximum Resistances': ('AllMaximumResistances', 'AllMaximumResistances', FLAT, False, False, False),
}

# Seite auf poedb, Ordner unter Prefixes/ und Suffixes/, ItemSlot, WeaponType (None für Rüstung), Tabelle.
# Die Zahlen sind die der Enums ItemSlot und WeaponType
SWORD, BOW, STAFF = 1, 6, 4
PHYSICAL_WEAPON, HELMET, OFFHAND, HANDS, TORSO, SPELL_WEAPON = 1, 4, 6, 12, 15, 18
CLASSES = [
    ('One_Hand_Swords', 'Weapons/Swords', PHYSICAL_WEAPON, SWORD, WEAPON),
    ('Bows', 'Weapons/Bows', PHYSICAL_WEAPON, BOW, WEAPON),
    ('Staves', 'Weapons/Staves', SPELL_WEAPON, STAFF, WEAPON),
    ('Helmets_str', 'Armors/Helmets', HELMET, None, ARMOUR),
    ('Body_Armours_str', 'Armors/BodyArmours', TORSO, None, ARMOUR),
    ('Gloves_str', 'Armors/Gloves', HANDS, None, ARMOUR),
    ('Shields_str', 'Armors/Shields', OFFHAND, None, ARMOUR),
]

# PoE nennt zwei Familien gleich, hier bekommt jede ihren eigenen Namen: (Datei, PoE-Name) → unser Name
FAMILY_NAMES = {('FlatLifeRegeneration', 'of Recuperation'): 'of Wellness'}
