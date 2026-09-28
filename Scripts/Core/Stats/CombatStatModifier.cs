using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

public record CombatStatModifier(CombatStat AffectedStat, ModificationType ModificationType, float Value, string OriginId = "");