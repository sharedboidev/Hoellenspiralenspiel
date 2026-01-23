using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Models;

public record HitResult(float Value, HitType HitType, LifeModificationMode LifeModificationMode);