using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Models;

public record CombatStatModifier(CombatStat AffectedStat, ModificationType ModificationType, float Value);