namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record AttackDefinition(string Name, float WeaponDamagePercent, DamageType? DealtAs = null)
{
    public static AttackDefinition Standard { get; } = new("Attack", 100f);
}
