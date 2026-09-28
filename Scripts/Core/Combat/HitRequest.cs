namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record HitRequest(float      MinDamage,
                                float      MaxDamage,
                                DamageType DamageType,
                                SkillKind  SkillKind,
                                float      HitChance           = CombatRules.BaseHitChance,
                                float      CriticalHitChance   = 0f,
                                float      CriticalDamageBonus = CombatRules.BaseCriticalDamage);
