using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Tests.Balance;

//Beschreibt einen Kämpfer. Jeder Kampf baut daraus ein frisches Blatt, denn Statuseffekte verändern es
internal sealed class Fighter
{
    //Wie im Spiel: Schlag und Zauber des Helden lösen nach der Hälfte ihrer Dauer aus
    private const double HeroImpactFraction = 0.5;

    public static readonly SkillDefinition StandardAttack = SkillDefinition.ForAttack("attack", AttackDefinition.Standard);

    private readonly Action<StatSheet> applyValues;
    private readonly EnemyDefinition   enemy;

    private Fighter(string name, Action<StatSheet> applyValues, WeaponProfile weapon, IReadOnlyList<SkillDefinition> skills, EnemyDefinition enemy)
    {
        this.applyValues = applyValues;
        this.enemy       = enemy;

        Name   = name;
        Weapon = weapon;
        Skills = skills;
    }

    public string Name { get; }

    public WeaponProfile Weapon { get; }

    //Genommen wird der erste Skill, der bereit ist und bezahlt werden kann. Ist keiner bereit, wartet der Kämpfer
    public IReadOnlyList<SkillDefinition> Skills { get; }

    //Gegner haben unbegrenzt Mana, der Held zahlt
    public bool PaysMana => enemy is null;

    public static Fighter Hero(HeroBaseValues values, IEnumerable<ItemDefinition> equipment = null, IReadOnlyList<SkillDefinition> skills = null, string name = "Hero")
    {
        ArgumentNullException.ThrowIfNull(values);

        var items  = (equipment ?? []).Select(item => new ItemInstance(item)).ToList();
        var weapon = items.FirstOrDefault(item => item.Definition.Weapon is not null)?.ToWeaponProfile() ?? WeaponProfile.Unarmed;

        return new Fighter(name,
                           sheet =>
                           {
                               values.Apply(sheet);
                               weapon.ApplyTo(sheet);

                               foreach (var item in items)
                                   sheet.AddModifiers(item.GetEquipModifiers());
                           },
                           weapon,
                           skills ?? [StandardAttack],
                           null);
    }

    //Ein Boss kämpft ohne Angabe mit seinen festen Mods, wie im Spiel. Mods wirken nur über ihre Modifier, Effekte wie Beschwörungen fehlen
    public static Fighter Enemy(EnemyDefinition definition, int level, IReadOnlyList<MonsterModDefinition> mods = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var chosenMods = mods ?? (definition.IsBoss ? definition.FixedMods : []);
        var label      = chosenMods.Count == 0 ? $"{definition.Name} {level}" : $"{definition.Name} {level} ({string.Join(", ", chosenMods.Select(mod => mod.Name))})";

        return new Fighter(label,
                           sheet => EnemyStats.Apply(sheet, definition, level, chosenMods),
                           EnemyStats.GetWeapon(definition, level),
                           definition.Skills.Count > 0 ? definition.Skills : [StandardAttack],
                           definition);
    }

    public StatSheet CreateStats()
    {
        var sheet = new StatSheet();

        sheet.Update(stats =>
        {
            UnitBaseValues.Apply(stats);

            applyValues(stats);
        });

        return sheet;
    }

    //Der Held schlägt im Takt seines Angriffstempos und steht beim Zaubern so lange, wie der Zauber zum Wirken braucht.
    //Ein Gegner holt so lange aus und erholt sich, wie seine Definition sagt, geteilt durch sein Angriffstempo
    public (double WindupSec, double RecoverySec) GetActionTiming(StatSheet stats, SkillDefinition skill)
    {
        if (enemy is not null)
        {
            var rate = Math.Max(0.1f, stats.GetTotalMultiplier(CombatStat.Attackspeed));

            return (enemy.AttackWindupSec / rate, enemy.AttackRecoverySec / rate);
        }

        //Ein Wirbel trifft je Tick, der Simulator rechnet jeden Tick wie einen Schlag mit halbem Ausholen und halber Erholung
        var durationSec = skill.IsChanneled
                ? skill.Channel.GetIntervalSec(ChannelSettings.GetAttacksPerSec(stats))
                : skill.Kind == SkillKind.Attack
                        ? 1.0 / Math.Max(CombatRules.MinAttacksPerSecond, stats.GetFinal(CombatStat.Attackspeed))
                        : skill.GetCastSec(stats);

        return (durationSec * HeroImpactFraction, durationSec * (1 - HeroImpactFraction));
    }

    //Der Held wartet nach einem Zauber mindestens die kürzeste Abklingzeit, wie im Spiel
    public double GetCooldownSec(SkillDefinition skill)
        => PaysMana && skill.Kind == SkillKind.Spell ? Math.Max(skill.CooldownSec, CombatRules.MinSpellCooldownSec) : skill.CooldownSec;

    public override string ToString()
        => Name;
}
