using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;

namespace Hoellenspiralenspiel.Resources.Enemies;

[GlobalClass]
public partial class EnemyResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    //Die Szene bringt nur Aussehen, Animationen und Kollisionsform mit
    [Export]
    public PackedScene Scene { get; set; }

    //Wird auf das Bereichslevel der Karte addiert
    [Export]
    public int LevelOffset { get; set; }

    [Export]
    public int Xp { get; set; } = 100;

    //Gold auf Monsterlevel 1, vor den Aufschlägen für Level und Seltenheit. 0 und 0 heißt, der Gegner trägt kein Gold
    [Export]
    public int GoldMin { get; set; } = 1;

    [Export]
    public int GoldMax { get; set; } = 4;

    [Export]
    public string LootTableId { get; set; } = string.Empty;

    [ExportGroup("Attributes")]
    [Export]
    public int Strength { get; set; } = 1;

    [Export]
    public int Dexterity { get; set; } = 1;

    [Export]
    public int Intelligence { get; set; } = 1;

    [Export]
    public int Constitution { get; set; } = 1;

    [Export]
    public int Awareness { get; set; } = 1;

    [ExportSubgroup("Growth per Level")]
    [Export]
    public float StrengthPerLevel { get; set; }

    [Export]
    public float DexterityPerLevel { get; set; }

    [Export]
    public float IntelligencePerLevel { get; set; }

    [Export]
    public float ConstitutionPerLevel { get; set; }

    [Export]
    public float AwarenessPerLevel { get; set; }

    [ExportGroup("Base Values")]
    [Export]
    public int LifeBonus { get; set; }

    [Export]
    public float Movementspeed { get; set; } = 50f;

    [Export]
    public int Armor { get; set; }

    [Export]
    public int Dodge { get; set; } = 6;

    [Export]
    public int FireResistance { get; set; }

    [Export]
    public int FrostResistance { get; set; }

    [Export]
    public int LightningResistance { get; set; }

    //Die erste Waffe in der Liste führt das Monster, sie ersetzt die natürliche Waffe
    [ExportGroup("Equipment")]
    [Export]
    public Array<EquippableBaseResource> Equipment { get; set; } = new();

    [ExportGroup("Natural Weapon")]
    [Export]
    public float DamageMin { get; set; } = 1f;

    [Export]
    public float DamageMax { get; set; } = 3f;

    [Export]
    public DamageType DamageType { get; set; } = DamageType.Crush;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float CriticalHitChance { get; set; } = 5f;

    //Das Monster nimmt den ersten Skill der Liste, der nicht abklingt. Ohne Eintrag bleibt der Standardangriff
    [ExportGroup("Attack")]
    [Export]
    public Array<SkillResource> Skills { get; set; } = new();

    [Export]
    public float AttackRange { get; set; } = 150f;

    [Export]
    public float AttackWindupSec { get; set; } = 0.3f;

    [Export]
    public float AttackRecoverySec { get; set; } = 0.2f;

    [ExportGroup("Behaviour")]
    [Export]
    public float AggroRange { get; set; } = 500f;

    //So lange folgt das Monster einem Ziel, das außerhalb von AggroRange bleibt
    [Export]
    public double ChaseTimeSec { get; set; } = 6;

    [Export(PropertyHint.Range, "0.1,1,0.05")]
    public float ReturnSpeedFactor { get; set; } = 0.5f;

    //Der Rückweg endet an einem zufälligen Punkt in diesem Abstand um den Startort
    [Export]
    public float HomeRadius { get; set; } = 150f;

    //Ein Boss bekommt feste Mods statt gewürfelter. Größe, XP, Beute und Gold kommen aus den Boss-Werten des EnemyController
    [ExportGroup("Boss")]
    [Export]
    public bool IsBoss { get; set; }

    [Export]
    public Array<MonsterModResource> FixedMods { get; set; } = new();

    public string NameOrId => string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;

    public WeaponBaseResource WieldedWeapon => Equipment.OfType<WeaponBaseResource>().FirstOrDefault();

    //Die Definition entsteht beim ersten Zugriff. Wer danach Werte der Resource ändert, sieht davon nichts
    public EnemyDefinition Core => field ??= new EnemyDefinition(Id, DisplayName)
    {
        LevelOffset         = LevelOffset,
        Xp                  = Xp,
        GoldMin             = GoldMin,
        GoldMax             = GoldMax,
        LootTableId         = LootTableId ?? string.Empty,
        Strength            = new AttributeGrowth(Strength, StrengthPerLevel),
        Dexterity           = new AttributeGrowth(Dexterity, DexterityPerLevel),
        Intelligence        = new AttributeGrowth(Intelligence, IntelligencePerLevel),
        Constitution        = new AttributeGrowth(Constitution, ConstitutionPerLevel),
        Awareness           = new AttributeGrowth(Awareness, AwarenessPerLevel),
        LifeBonus           = LifeBonus,
        Movementspeed       = Movementspeed,
        Armor               = Armor,
        Dodge               = Dodge,
        FireResistance      = FireResistance,
        FrostResistance     = FrostResistance,
        LightningResistance = LightningResistance,
        Equipment           = Equipment.Where(item => item is not null).Select(item => item.Definition).ToArray(),
        DamageMin           = DamageMin,
        DamageMax           = DamageMax,
        DamageType          = DamageType,
        CriticalHitChance   = CriticalHitChance,
        Skills              = Skills.Where(skill => skill is not null).Select(skill => skill.Definition).ToArray(),
        AttackRange         = AttackRange,
        AttackWindupSec     = AttackWindupSec,
        AttackRecoverySec   = AttackRecoverySec,
        Behaviour           = new EnemyBehaviour { AggroRange = AggroRange, ChaseTimeSec = ChaseTimeSec },
        ReturnSpeedFactor   = ReturnSpeedFactor,
        HomeRadius          = HomeRadius,
        IsBoss              = IsBoss,
        FixedMods           = FixedMods.Where(mod => mod is not null).Select(mod => mod.Definition).ToArray()
    };
}
