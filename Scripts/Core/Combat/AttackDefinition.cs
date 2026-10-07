namespace Hoellenspiralenspiel.Scripts.Core.Combat;

public sealed record AttackDefinition(string Name, float WeaponDamagePercent, DamageType? DealtAs = null)
{
    public static AttackDefinition Standard { get; } = new("Attack", 100f);

    //Pierce trifft nur halb so oft. Ein Angriff mit diesem Kennzeichen trifft trotzdem mit der vollen Chance
    public bool IgnoresPierceHitPenalty { get; init; }
}
