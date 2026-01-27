using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Models;

public record HitResult(float Value, HitType HitType, LifeModificationMode LifeModificationMode);