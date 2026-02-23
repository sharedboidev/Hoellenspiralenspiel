using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Models.Weapons;

namespace Hoellenspiralenspiel.Scripts.Models;

public record ItemModifier(AffixType AffixType, CombatStat CombatStat, ModificationType ModificationType, float Value, string ItemnameAddition);