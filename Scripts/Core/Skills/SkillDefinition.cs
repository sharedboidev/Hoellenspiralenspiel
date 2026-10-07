using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Genau eines von Attack und Spell ist gesetzt
public sealed record SkillDefinition
{
    private const float MinCastSpeedFactor = 0.1f;

    private SkillDefinition(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ein Skill braucht eine Id.", nameof(id));

        Id   = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
    }

    public string Id   { get; }
    public string Name { get; }

    public AttackDefinition Attack { get; private init; }
    public SpellDefinition  Spell  { get; private init; }

    public SkillKind Kind => Spell is not null ? SkillKind.Spell : SkillKind.Attack;

    public float         ManaCost    { get; init; }
    public double        CooldownSec { get; init; }
    public SkillDelivery Delivery    { get; init; }

    //Nur für Zauber des Helden: So lange steht er beim Wirken. Gegner nehmen die Zeiten ihres EnemyResource
    public double CastSec { get; init; }

    public ProjectileSettings Projectile { get; init; }

    public AreaSettings Area { get; init; }

    public SweepSettings Sweep { get; init; }

    //Nur für Attacks: Kugeln, die nach einem gelandeten Treffer aus dem Ziel springen
    public ScatterSettings Scatter { get; init; }

    //Nur für Attacks mit dem Bogen: Pfeile, die nach dem Schuss in den Himmel auf das Zielgebiet fallen
    public RainSettings Rain { get; init; }

    public bool IsArea => Delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint;

    //Ein Bogen ist keine Klinge: Mit einer Fernkampfwaffe in der Hand lassen sich diese Skills nicht einsetzen
    public bool NeedsMeleeWeapon => Delivery is SkillDelivery.WeaponSweep or SkillDelivery.MeleeStrike;

    //Pfeile kommen nur aus einem Bogen, der einzigen Fernkampfwaffe
    public bool NeedsBow => Delivery is SkillDelivery.ArrowRain;

    //Erhöhtes Zaubertempo teilt die Wirkzeit: 50 % mehr ergibt zwei Drittel der Zeit
    public double GetCastSec(StatSheet caster)
    {
        ArgumentNullException.ThrowIfNull(caster);

        return CastSec / Math.Max(MinCastSpeedFactor, caster.GetTotalMultiplier(CombatStat.CastSpeed));
    }

    public static SkillDefinition ForAttack(string id, AttackDefinition attack)
    {
        ArgumentNullException.ThrowIfNull(attack);

        return new SkillDefinition(id, attack.Name) { Attack = attack };
    }

    public static SkillDefinition ForSpell(string id, SpellDefinition spell)
    {
        ArgumentNullException.ThrowIfNull(spell);

        return new SkillDefinition(id, spell.Name) { Spell = spell };
    }
}
