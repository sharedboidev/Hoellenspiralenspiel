using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Models.Weapons;

public record WeaponStatModifier(AffixType AffixType, WeaponStat WeaponStat, ModificationType ModificationType, float Value);