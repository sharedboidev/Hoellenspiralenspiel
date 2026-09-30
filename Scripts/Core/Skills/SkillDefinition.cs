using System;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Genau eines von Attack und Spell ist gesetzt
public sealed record SkillDefinition
{
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

    public bool IsArea => Delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint;

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
