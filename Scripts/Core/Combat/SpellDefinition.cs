namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record SpellDefinition(string     Name,
                                     float      MinDamage,
                                     float      MaxDamage,
                                     DamageType DamageType,
                                     float      CriticalHitChance);
