using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Balance;

//Baut Definitionen aus den Resources des Spiels, damit die Bilanz mit den echten Zahlen rechnet.
//Fehlt ein Feld in der Datei, gilt der Standard der Resource. Für Gegner ist das der Standard von EnemyDefinition,
//für Items und Skills steht er hier, weil die Resource-Klassen Godot brauchen
internal static class GameData
{
    private const string EnemiesFolder = "res://Resources/Enemies";

    public static string Root { get; } = FindRepositoryRoot();

    public static IReadOnlyList<EnemyDefinition> AllEnemies()
        => Directory.GetFiles(ToFile(EnemiesFolder), "*.tres")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => ReadEnemy(TresFile.Read(path)))
                    .ToList();

    public static EnemyDefinition Enemy(string id)
        => ReadEnemy(TresFile.Read(ToFile($"{EnemiesFolder}/{id}.tres")));

    public static ItemDefinition Weapon(string id)
        => Item($"res://Resources/Items/Weapons/{id}.tres");

    public static ItemDefinition Armor(string id)
        => Item($"res://Resources/Items/Armors/{id}.tres");

    public static SkillDefinition PlayerSkill(string id)
        => Skill($"res://Resources/Skills/Player/{id}.tres");

    public static MonsterModDefinition PoolMod(string id)
        => Mod($"res://Resources/MonsterMods/Pool/{id}.tres");

    public static string ToFile(string resPath)
        => Path.Combine(Root, resPath["res://".Length..].Replace('/', Path.DirectorySeparatorChar));

    private static EnemyDefinition ReadEnemy(TresFile file)
    {
        var values   = file.Resource;
        var defaults = new EnemyDefinition(values.String("Id"), values.String("DisplayName"));

        return defaults with
        {
            LevelOffset = values.Int("LevelOffset", defaults.LevelOffset),
            Xp = values.Int("Xp", defaults.Xp),
            GoldMin = values.Int("GoldMin", defaults.GoldMin),
            GoldMax = values.Int("GoldMax", defaults.GoldMax),
            LootTableId = values.String("LootTableId", defaults.LootTableId),
            Strength = Growth(values, "Strength", defaults.Strength),
            Dexterity = Growth(values, "Dexterity", defaults.Dexterity),
            Intelligence = Growth(values, "Intelligence", defaults.Intelligence),
            Constitution = Growth(values, "Constitution", defaults.Constitution),
            Awareness = Growth(values, "Awareness", defaults.Awareness),
            LifeBonus = values.Int("LifeBonus", defaults.LifeBonus),
            Movementspeed = values.Float("Movementspeed", defaults.Movementspeed),
            Armor = values.Int("Armor", defaults.Armor),
            Dodge = values.Int("Dodge", defaults.Dodge),
            FireResistance = values.Int("FireResistance", defaults.FireResistance),
            FrostResistance = values.Int("FrostResistance", defaults.FrostResistance),
            LightningResistance = values.Int("LightningResistance", defaults.LightningResistance),
            Equipment = values.References("Equipment").Select(reference => Item(file.ResolveExt(reference.Id))).ToArray(),
            DamageMin = values.Float("DamageMin", defaults.DamageMin),
            DamageMax = values.Float("DamageMax", defaults.DamageMax),
            DamageType = values.Enum("DamageType", defaults.DamageType),
            CriticalHitChance = values.Float("CriticalHitChance", defaults.CriticalHitChance),
            Skills = values.References("Skills").Select(reference => Skill(file.ResolveExt(reference.Id))).ToArray(),
            AttackRange = values.Float("AttackRange", defaults.AttackRange),
            AttackWindupSec = values.Float("AttackWindupSec", defaults.AttackWindupSec),
            AttackRecoverySec = values.Float("AttackRecoverySec", defaults.AttackRecoverySec),
            Behaviour = defaults.Behaviour with
            {
                AggroRange = values.Float("AggroRange", defaults.Behaviour.AggroRange),
                ChaseTimeSec = values.Double("ChaseTimeSec", defaults.Behaviour.ChaseTimeSec)
            },
            ReturnSpeedFactor = values.Float("ReturnSpeedFactor", defaults.ReturnSpeedFactor),
            HomeRadius = values.Float("HomeRadius", defaults.HomeRadius),
            IsBoss = values.Bool("IsBoss", defaults.IsBoss),
            FixedMods = values.References("FixedMods").Select(reference => Mod(file.ResolveExt(reference.Id))).ToArray()
        };
    }

    private static AttributeGrowth Growth(TresSection values, string attribute, AttributeGrowth fallback)
        => new(values.Int(attribute, fallback.AtLevelOne), values.Float($"{attribute}PerLevel", fallback.PerLevel));

    private static ItemDefinition Item(string resPath)
    {
        var file   = TresFile.Read(ToFile(resPath));
        var values = file.Resource;
        var id     = values.String("Id");
        var name   = values.String("DisplayName");
        var guard  = new GuardStats(values.Float("MeleeBlock", 0f), values.Float("SpellBlock", 0f), values.Float("MeleeParry", 0f), values.Float("SpellParry", 0f));

        if (file.ScriptPath.EndsWith("/WeaponBaseResource.cs", StringComparison.Ordinal))
        {
            var weapon = new WeaponStats(values.Float("MinDamage", 0f),
                                         values.Float("MaxDamage", 0f),
                                         values.Float("AttacksPerSecond", 1f),
                                         values.Float("CriticalHitChance", 0f),
                                         values.Enum("WeaponType", WeaponType.Undefined),
                                         values.Enum("WieldStrategy", WieldStrategy.Undefined))
            {
                Range           = values.Float("Range", WeaponProfile.DefaultMeleeRange),
                IsRanged        = values.Has("ProjectileScene"),
                ProjectileSpeed = values.Float("ProjectileSpeed", 1400f)
            };

            return ItemDefinition.ForWeapon(id, name, values.Enum("Slot", ItemSlot.PhysicalWeapon), weapon) with { Guard = guard };
        }

        if (file.ScriptPath.EndsWith("/ArmorBaseResource.cs", StringComparison.Ordinal))
            return ItemDefinition.ForArmor(id, name, values.Enum("Slot", ItemSlot.Torso), values.Int("Armor", 0)) with { Guard = guard };

        throw new InvalidDataException($"{resPath} ist weder Waffe noch Rüstung");
    }

    private static SkillDefinition Skill(string resPath)
    {
        var file     = TresFile.Read(ToFile(resPath));
        var values   = file.Resource;
        var id       = values.String("Id");
        var name     = values.String("DisplayName") is { Length: > 0 } displayName ? displayName : id;
        var delivery = values.Enum("Delivery", SkillDelivery.Weapon);

        var skill = file.ScriptPath switch
        {
            var script when script.EndsWith("/AttackSkillResource.cs", StringComparison.Ordinal) =>
                    SkillDefinition.ForAttack(id, new AttackDefinition(name,
                                                                       values.Float("WeaponDamagePercent", 100f),
                                                                       values.Bool("ConvertsDamageType", false) ? values.Enum("DealtAs", DamageType.Crush) : null)
                                              {
                                                  IgnoresPierceHitPenalty = values.Bool("IgnoresPierceHitPenalty", false)
                                              }) with
                    {
                        Scatter = values.Int("ScatterCount", 0) > 0
                                ? new ScatterSettings(values.Int("ScatterCount", 0),
                                                      values.Float("ScatterWeaponDamagePercent", 50f),
                                                      values.Float("ScatterImpactRadius", 75f),
                                                      values.Float("ScatterFlightSec", 0.6f))
                                : null
                    },
            var script when script.EndsWith("/SpellSkillResource.cs", StringComparison.Ordinal) =>
                    SkillDefinition.ForSpell(id, new SpellDefinition(name,
                                                                     values.Float("MinDamage", 0f),
                                                                     values.Float("MaxDamage", 0f),
                                                                     values.Enum("DamageType", DamageType.Fire),
                                                                     values.Float("CriticalHitChance", 5f))) with
                    {
                        CastSec = values.Double("CastSec", 0.4)
                    },
            _ => throw new InvalidDataException($"{resPath} ist kein Skill")
        };

        return skill with
        {
            ManaCost = values.Float("ManaCost", 0f),
            CooldownSec = values.Double("CooldownSec", 0),
            Delivery = delivery,
            Projectile = delivery == SkillDelivery.Projectile
                    ? new ProjectileSettings(values.Float("ProjectileSpeed", 800f),
                                             values.Float("ProjectileLifetimeSec", 2f),
                                             values.Int("ForkCount", 0),
                                             values.Int("ForkGenerations", 0),
                                             values.Float("ForkRange", 600f))
                    : null,
            Area = delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint
                    ? new AreaSettings(values.Float("AreaRadius", 300f), values.Float("AreaExpansionSec", 0f), values.Float("AreaDelaySec", 0f))
                    : null,
            Rain = delivery == SkillDelivery.ArrowRain
                    ? new RainSettings(values.Int("RainCount", 5),
                                       values.Float("RainRadius", 200f),
                                       values.Float("RainImpactRadius", 75f),
                                       values.Float("RainDelaySec", 0.5f),
                                       values.Float("RainDurationSec", 1f))
                    : null,
            Charge = delivery == SkillDelivery.ChargedShot
                    ? new ChargeSettings(values.Float("ChargeRatePerSec", 20f),
                                         values.Float("ChargeMinPercent", 100f / 3f),
                                         values.Float("ChargeMaxPercent", 150f),
                                         values.Float("ChargeOverholdSec", 0.5f),
                                         values.Double("ChargeOverholdCooldownSec", 5))
                    : null,
            Sweep = delivery is SkillDelivery.WeaponSweep or SkillDelivery.WeaponWhirl
                    ? new SweepSettings(values.Float("SweepArcDegrees", 180f), values.Float("SweepRangeFactor", 1f))
                    : null,
            Channel = delivery == SkillDelivery.WeaponWhirl
                    ? new ChannelSettings(values.Float("ChannelManaPerSec", 3f),
                                          values.Float("ChannelTicksPerAttack", 1f),
                                          values.Float("ChannelTurnsPerTick", 1f),
                                          values.Bool("ChannelGrantsPhasing", false))
                    : null
        };
    }

    private static MonsterModDefinition Mod(string resPath)
    {
        var file   = TresFile.Read(ToFile(resPath));
        var values = file.Resource;

        var modifiers = values.References("Modifiers")
                              .Select(reference => reference.IsExternal ? TresFile.Read(ToFile(file.ResolveExt(reference.Id))).Resource : file.Sub(reference.Id))
                              .Select(modifier => new CombatStatModifier(modifier.Enum("Stat", default(CombatStat)),
                                                                         modifier.Enum("Modification", ModificationType.Flat),
                                                                         modifier.Float("Value", 0f)))
                              .ToArray();

        return new MonsterModDefinition(values.String("Id"), values.String("DisplayName"))
        {
            Weight         = values.Float("Weight", 1f),
            MinLevel       = values.Int("MinLevel", 1),
            ExclusiveGroup = values.String("ExclusiveGroup"),
            Fit            = values.Enum("Fit", MonsterModFit.Any),
            Modifiers      = modifiers
        };
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Das Godot-Projekt liegt nicht über dem Testordner.");
    }
}
