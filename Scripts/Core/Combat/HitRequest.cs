namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Ein Treffer, bevor gewürfelt wird. Alle Werte des Angreifers sind bereits eingerechnet. Chancen und Krit-Schaden in Prozent
public sealed record HitRequest(float      MinDamage,
                                float      MaxDamage,
                                DamageType DamageType,
                                SkillKind  SkillKind,
                                float      HitChance           = CombatRules.BaseHitChance,
                                float      CriticalHitChance   = 0f,
                                float      CriticalDamageBonus = CombatRules.BaseCriticalDamage);
