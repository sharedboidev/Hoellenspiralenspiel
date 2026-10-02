using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

//Ein Attribut auf Monsterlevel 1 und sein Wachstum je weiterem Level
public readonly record struct AttributeGrowth(int AtLevelOne, float PerLevel = 0f)
{
    public static AttributeGrowth One { get; } = new(1);

    public int GetAt(int level)
        => EnemyScaling.GetAttribute(AtLevelOne, PerLevel, level);
}

//Alles, was ein EnemyResource über einen Gegner sagt, ohne Szene und Effekte der Mods
public sealed record EnemyDefinition
{
    public EnemyDefinition(string id, string name)
    {
        Id   = id ?? string.Empty;
        Name = string.IsNullOrWhiteSpace(name) ? Id : name;
    }

    public string Id   { get; }
    public string Name { get; }

    //Wird auf das Bereichslevel der Karte addiert
    public int LevelOffset { get; init; }

    public int Xp { get; init; } = 100;

    public int GoldMin { get; init; } = 1;
    public int GoldMax { get; init; } = 4;

    public string LootTableId { get; init; } = string.Empty;

    public AttributeGrowth Strength     { get; init; } = AttributeGrowth.One;
    public AttributeGrowth Dexterity    { get; init; } = AttributeGrowth.One;
    public AttributeGrowth Intelligence { get; init; } = AttributeGrowth.One;
    public AttributeGrowth Constitution { get; init; } = AttributeGrowth.One;
    public AttributeGrowth Awareness    { get; init; } = AttributeGrowth.One;

    public int   LifeBonus           { get; init; }
    public float Movementspeed       { get; init; } = 50f;
    public int   Armor               { get; init; }
    public int   Dodge               { get; init; } = 6;
    public int   FireResistance      { get; init; }
    public int   FrostResistance     { get; init; }
    public int   LightningResistance { get; init; }

    //Die erste Waffe in der Liste führt das Monster, sie ersetzt die natürliche Waffe
    public IReadOnlyList<ItemDefinition> Equipment { get; init; } = [];

    public float      DamageMin         { get; init; } = 1f;
    public float      DamageMax         { get; init; } = 3f;
    public DamageType DamageType        { get; init; } = DamageType.Crush;
    public float      CriticalHitChance { get; init; } = 5f;

    //Das Monster nimmt den ersten Skill der Liste, der nicht abklingt. Ohne Eintrag bleibt der Standardangriff
    public IReadOnlyList<SkillDefinition> Skills { get; init; } = [];

    public float AttackRange       { get; init; } = 150f;
    public float AttackWindupSec   { get; init; } = 0.3f;
    public float AttackRecoverySec { get; init; } = 0.2f;

    public EnemyBehaviour Behaviour { get; init; } = new();

    public float ReturnSpeedFactor { get; init; } = 0.5f;

    //Der Rückweg endet an einem zufälligen Punkt in diesem Abstand um den Startort
    public float HomeRadius { get; init; } = 150f;

    public bool IsBoss { get; init; }

    public IReadOnlyList<MonsterModDefinition> FixedMods { get; init; } = [];

    public ItemDefinition WieldedWeapon => Equipment.FirstOrDefault(item => item.Weapon is not null);

    public WeaponProfile NaturalWeapon => new(DamageMin, DamageMax, 1f, CriticalHitChance, DamageType, AttackRange);

    public bool UsesProjectiles
    {
        get
        {
            var hasRangedWeapon = WieldedWeapon?.Weapon.IsRanged == true;

            return Skills.Any(skill => skill.Delivery == SkillDelivery.Projectile || (skill.Delivery == SkillDelivery.Weapon && hasRangedWeapon)) ||
                   (Skills.Count == 0 && hasRangedWeapon);
        }
    }
}
