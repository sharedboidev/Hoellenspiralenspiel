namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Ein SPELL: ein Skill mit eigenem Grundschaden, unabhängig von der Waffe
public sealed record SpellDefinition(string     Name,
                                     float      MinDamage,
                                     float      MaxDamage,
                                     DamageType DamageType,
                                     float      CriticalHitChance);
